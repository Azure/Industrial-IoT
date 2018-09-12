// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Default {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.IO;
    using System.Threading;
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Persists into a file
    /// </summary>
    public class FilePersistance : IPersistenceProvider, IDisposable {

        /// <summary>
        /// Create file persistence provider
        /// </summary>
        /// <param name="logger"></param>
        public FilePersistance(ILogger logger) :
            this("iiot_fpp.json", logger) {
        }

        /// <summary>
        /// Create file persistence provider
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="logger"></param>
        public FilePersistance(string fileName, ILogger logger) {
            _fileName = fileName ??
                throw new ArgumentNullException(nameof(fileName));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            _lock = new SemaphoreSlim(1);
            _master = Sync();

            _notifier = new FileSystemWatcher(Path.GetTempPath(), _fileName);
            _notifier.Changed += (_, arg) => OnSynchronize(arg);
            _notifier.Deleted += (_, arg) => OnSynchronize(arg);
            _notifier.Renamed += (_, arg) => OnSynchronize(arg);
        }

        /// <inheritdoc/>
        public async Task WriteAsync(IDictionary<string, dynamic> values) {
            await _lock.WaitAsync();
            try {
                // Force synchronization and merge in new values
                Sync();
                foreach (var value in values) {
                    if (value.Value == null) {
                        _master.Remove(value.Key);
                    }
                    else if (_master.ContainsKey(value.Key)) {
                        _master[value.Key] = value.Value;
                    }
                    else {
                        _master.Add(value.Key, value.Value);
                    }
                }
                // Write all to file
                var path = Path.Combine(Path.GetTempPath(), _fileName);
                File.WriteAllText(path, JsonConvertEx.SerializeObjectPretty(_master));
                // Make sure we do not unnecesarily sync
                _updated = true;
            }
            catch (Exception ex) {
                throw new StorageException("Exception writing", ex);
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<dynamic> ReadAsync(string key) {
            await _lock.WaitAsync();
            try {
                if (_master.TryGetValue(key, out var result)) {
                    return result;
                }
                return null;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() => _notifier.Dispose();

        /// <summary>
        /// Read master from file
        /// </summary>
        private Dictionary<string, dynamic> Sync() {
            var path = Path.Combine(Path.GetTempPath(), _fileName);
            try {
                if (File.Exists(path)) {
                    return JsonConvertEx.DeserializeObject<Dictionary<string, dynamic>>(
                        File.ReadAllText(path));
                }
            }
            catch (Exception ex) {
                _logger.Error($"Failed to synchronize from {path}.",
                    () => ex);
            }
            return new Dictionary<string, dynamic>();
        }

        /// <summary>
        /// Handle file system notification
        /// </summary>
        /// <param name="args"></param>
        private void OnSynchronize(FileSystemEventArgs args) {
            _lock.Wait();
            try {
                if (!args.Name.EqualsIgnoreCase(_fileName)) {
                    _logger.Error("Bad notify", () => args);
                }
                else if (_updated) {
                    _updated = false;
                }
                else {
                    _master = Sync();
                }
            }
            finally {
                _lock.Release();
            }
        }

        private bool _updated;
        private Dictionary<string, dynamic> _master;
        private readonly ILogger _logger;
        private readonly FileSystemWatcher _notifier;
        private readonly string _fileName;
        private readonly SemaphoreSlim _lock;
    }
}
