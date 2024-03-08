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
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Hashing;
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
            _hash = new Crc32();
            _stateStoreFolder = options.Value.StateStoreFolderPath;
        }

        /// <inheritdoc/>
        public async ValueTask<bool> StoreAsync(string key, T value, CancellationToken ct)
        {
            try
            {
                var fs = Open(key, FileMode.Create);
                await using (fs.ConfigureAwait(false))
                {
                    var crc = new HashCalculatingStream(fs, _hash);
                    await using (crc.ConfigureAwait(false))
                    {
                        await _serializer.SerializeAsync(crc, value,
                            SerializeOption.Indented, ct: ct).ConfigureAwait(false);
                    }

                    _items.TryGetValue(key, out var previous);
                    var item = new State(crc.HashAndReset, (fs as MemoryStream)?.ToArray());

                    _items.AddOrUpdate(key, item);
                    return !previous.CheckSum.Span.SequenceEqual(item.CheckSum.Span);
                }
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
                var fs = Open(key, FileMode.Open);
                await using (fs.ConfigureAwait(false))
                {
                    var crc = new HashCalculatingStream(fs, _hash);
                    await using (crc.ConfigureAwait(false))
                    {
                        var result = await _serializer.DeserializeAsync<T>(crc,
                            ct: ct).ConfigureAwait(false);

                        _items.AddOrUpdate(key, new State(crc.HashAndReset, default));
                        return result;
                    }
                }
            }
            catch
            {
                _items.AddOrUpdate(key, new State());
                return default;
            }
        }

        /// <inheritdoc/>
        public ValueTask RemoveAsync(string key, CancellationToken ct)
        {
            if (_items.Remove(key))
            {
                if (_stateStoreFolder == null)
                {
                    return ValueTask.CompletedTask;
                }
                try
                {
                    var path = Path.Combine(_stateStoreFolder, typeof(T).Name);

                    File.Delete(Path.Combine(path, key) + kfileTypeSuffix);

                    if (!Directory.EnumerateFiles(path).Any())
                    {
                        Directory.Delete(path);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to remove state.");
                }
            }
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Open file for key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        private Stream Open(string key, FileMode mode)
        {
            if (_stateStoreFolder == null)
            {
                if (mode != FileMode.Create &&
                    _items.TryGetValue(key, out var previous))
                {
                    return new MemoryStream(previous.Buffer.ToArray());
                }
                return new MemoryStream();
            }
            var path = Path.Combine(_stateStoreFolder, typeof(T).Name);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return new FileStream(Path.Combine(path, key) + kfileTypeSuffix, mode);
        }

        private record struct State(ReadOnlyMemory<byte> CheckSum, ReadOnlyMemory<byte> Buffer);

        private const string kfileTypeSuffix = ".json";
        private readonly Dictionary<string, State> _items = new();
        private readonly ILogger _logger;
        private readonly Crc32 _hash;
        private readonly IJsonSerializer _serializer;
        private readonly string? _stateStoreFolder;
    }
}
