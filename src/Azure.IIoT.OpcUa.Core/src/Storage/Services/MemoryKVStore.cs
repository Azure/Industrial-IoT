// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Storage.Services
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Key value store in memory
    /// </summary>
    public sealed class MemoryKVStore : IKeyValueStore
    {
        /// <inheritdoc/>
        public string Name => "Memory";

        /// <inheritdoc/>
        public ValueTask<JsonNode?> TryPageInAsync(string key,
            CancellationToken ct = default)
        {
            State.TryGetValue(key, out var value);
            return ValueTask.FromResult(value);
        }

        /// <inheritdoc/>
        public IDictionary<string, JsonNode?> State { get; }
            = new ConcurrentDictionary<string, JsonNode?>();
    }
}
