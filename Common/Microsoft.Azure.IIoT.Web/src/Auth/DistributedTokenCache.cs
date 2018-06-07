// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Azure {
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// Token cache persisted in the distributed cache.
    /// </summary>
    public class DistributedTokenCache : TokenCache {

        /// <summary>
        /// Create token cache entry in provided distributed cache
        /// </summary>
        /// <param name="cacheKey">Key in cache</param>
        /// <param name="cache">cache to create entry in</param>
        /// <param name="dp">protector</param>
        public DistributedTokenCache(IDistributedCache cache, string cacheKey,
            IDataProtectionProvider dp) {

            var protector = dp.CreateProtector(GetType().FullName);

            AfterAccess = args => {
                if (HasStateChanged) {
                    if (Count > 0) {
                        // Write our new token cache state to the cache
                        cache.Set(cacheKey, protector.Protect(Serialize()));
                    }
                    else {
                        // The Token cache is empty so remove ourselves.
                        cache.Remove(cacheKey);
                    }
                    HasStateChanged = false;
                }
            };

            var cacheData = cache.Get(cacheKey);
            if (cacheData != null) {
                Deserialize(protector.Unprotect(cacheData));
            }
        }
    }
}
