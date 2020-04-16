// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Storage {
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.AspNetCore.DataProtection;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Distributed cache implementation
    /// </summary>
    public class DistributedProtectedCache : ICache {

        /// <summary>
        /// Create cache using provided distributed cache
        /// </summary>
        /// <param name="cache">Cache</param>
        /// <param name="dp">protector</param>
        public DistributedProtectedCache(
            IDistributedCache cache, IDataProtectionProvider dp) {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _protector = dp.CreateProtector(GetType().FullName);
        }

        /// <inheritdoc/>
        public async Task SetAsync(string key, byte[] value,
            DateTimeOffset expiration, CancellationToken ct) {
            await _cache.SetAsync(key, _protector.Protect(value), ct);
        }

        /// <inheritdoc/>
        public async Task<byte[]> GetAsync(string key, CancellationToken ct) {
            var value = await _cache.GetAsync(key, ct);
            return _protector.Unprotect(value);
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(string key, CancellationToken ct) {
            await _cache.RemoveAsync(key, ct);
        }

        private readonly IDistributedCache _cache;
        private readonly IDataProtector _protector;
    }
}
