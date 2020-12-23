// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Default {
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using MemCache = System.Runtime.Caching.MemoryCache;

    /// <summary>
    /// In memory cache
    /// </summary>
    public sealed class MemoryCache : ICache {

        /// <inheritdoc/>
        public Task<byte[]> GetAsync(string key, CancellationToken ct) {
            return Task.FromResult((byte[])_cache.Get(key));
        }

        /// <inheritdoc/>
        public Task RemoveAsync(string key, CancellationToken ct) {
            _cache.Remove(key);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetAsync(string key, byte[] value,
            DateTimeOffset expiration, CancellationToken ct) {
            _cache.Set(key, value, expiration);
            return Task.CompletedTask;
        }

        private static readonly MemCache _cache =
            new MemCache(typeof(MemoryCache).Name);
    }
}
