// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Cache extensions
    /// </summary>
    public static class CacheEx {

        /// <summary>
        /// Get string
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="key"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<string> GetStringAsync(this ICache cache,
            string key, CancellationToken ct = default) {
            var val = await cache.GetAsync(key, ct);
            if (val == null) {
                return null;
            }
            try {
                return Encoding.UTF8.GetString(val);
            }
            catch {
                return null;
            }
        }

        /// <summary>
        /// Set string
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task SetStringAsync(this ICache cache, string key,
            string value, DateTimeOffset expiration, CancellationToken ct = default) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            await cache.SetAsync(key, Encoding.UTF8.GetBytes(value), expiration, ct);
        }
    }
}