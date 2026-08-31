// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Storage
{
    using System.Collections.Generic;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Key value store interface.
    /// </summary>
    /// <remarks>
    /// Values are <see cref="JsonNode"/> instances (the OPC Publisher 3.0
    /// System.Text.Json replacement for the former Legacy VariantValue),
    /// which are mutable and indexable and keep the store AOT/trim safe.
    /// </remarks>
    public interface IKeyValueStore
    {
        /// <summary>
        /// Name of the storage interface used in the
        /// state store.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Get kv store state which can be manipulated
        /// and which is flushed periodically.
        /// </summary>
        IDictionary<string, JsonNode?> State { get; }

        /// <summary>
        /// Try page in
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<JsonNode?> TryPageInAsync(string key,
            CancellationToken ct = default);
    }
}
