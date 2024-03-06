// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Storage
{
    using Azure.IIoT.OpcUa.Publisher;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// File based state provider
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class StateStoreFolderProvider<T> : IStateProvider<T>
    {
        /// <summary>
        /// Create file based state provider
        /// </summary>
        /// <param name="options"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public StateStoreFolderProvider(IOptions<PublisherOptions> options,
            IJsonSerializer serializer, ILogger<StateStoreFolderProvider<T>> logger)
        {
            _serializer = serializer;
            _logger = logger;
            _stateStoreFolder = options.Value.StateStoreFolderPath;
        }

        private Stream Open(string key, FileMode mode)
        {
            if (_stateStoreFolder == null)
            {
                return new MemoryStream();
            }
            else
            {
                var path = Path.Combine(_stateStoreFolder, typeof(T).Name);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return new FileStream(Path.Combine(path, key), mode);
            }
        }

        /// <inheritdoc/>
        public async ValueTask<bool> StoreAsync(string key, T value, CancellationToken ct)
        {
            try
            {
                var fs = Open(key, FileMode.Create);
                await using (fs.ConfigureAwait(false))
                {
                    await _serializer.SerializeAsync(fs, value,
                        SerializeOption.Indented, ct: ct).ConfigureAwait(false);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store state.");
                return false;
            }
        }

        /// <inheritdoc/>
        public async ValueTask<T?> LoadAsync(string key, CancellationToken ct)
        {
            try
            {
                var fs = Open(key, FileMode.OpenOrCreate);
                await using (fs.ConfigureAwait(false))
                {
                    return await _serializer.DeserializeAsync<T>(fs,
                        ct: ct).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to load state.");
                return default;
            }
        }

        /// <inheritdoc/>
        public ValueTask RemoveAsync(string key, CancellationToken ct)
        {
            if (_stateStoreFolder == null)
            {
                return ValueTask.CompletedTask;
            }
            try
            {
                var path = Path.Combine(_stateStoreFolder, typeof(T).Name);

                File.Delete(Path.Combine(path, key));

                if (!Directory.EnumerateFiles(path).Any())
                {
                    Directory.Delete(path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to remove state.");
            }
            return ValueTask.CompletedTask;
        }

        private readonly ILogger _logger;
        private readonly IJsonSerializer _serializer;
        private readonly string? _stateStoreFolder;
    }
}
