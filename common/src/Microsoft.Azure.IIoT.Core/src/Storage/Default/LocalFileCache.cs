// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Default {
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Simplistic cache using a single .cache file
    /// </summary>
    public sealed class LocalFileCache : ICache {

        /// <summary>
        /// Create file cache
        /// </summary>
        public LocalFileCache(IJsonSerializer serializer) {
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _filePath = Path.GetFullPath(Environment.CurrentDirectory) + ".cache";
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
        }

        /// <inheritdoc/>
        public Task<byte[]> GetAsync(string key, CancellationToken ct) {
            byte[] result = null;
            Access(secrets => {
                if (secrets.TryGetValue(key, out var v)) {
                    result = v.DecodeAsBase64();
                }
                return false;
            });
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task RemoveAsync(string key, CancellationToken ct) {
            Access(secrets => secrets.Remove(key));
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetAsync(string key, byte[] value,
            DateTimeOffset expiration, CancellationToken ct) {
            Access(secrets => {
                secrets.AddOrUpdate(key, value.ToBase64String());
                return true;
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Access shared cache file
        /// </summary>
        private void Access(Func<IDictionary<string, string>, bool> updater) {
            using (var fileStream = new FileStream(_filePath, FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None)) {
                using (var reader = new StreamReader(fileStream, _serializer.ContentEncoding)) {
                    var secrets = Try.Op(() =>
                        _serializer.Deserialize<IDictionary<string, string>>(reader));
                    if (updater(secrets ?? new Dictionary<string, string>())) {
                        fileStream.Seek(0, SeekOrigin.Begin);
                        fileStream.SetLength(0);
                        using (var writer =
                            new StreamWriter(fileStream, _serializer.ContentEncoding)) {
                            var str = _serializer.SerializeToString(secrets);
                            writer.Write(str);
                        }
                    }
                }
            }
        }

        private readonly string _filePath;
        private readonly IJsonSerializer _serializer;
    }
}
