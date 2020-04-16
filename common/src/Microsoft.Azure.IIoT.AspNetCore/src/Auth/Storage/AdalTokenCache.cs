// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using System;

    /// <summary>
    /// Token cache persisted in the distributed cache.
    /// </summary>
    public class AdalTokenCache : IAdalTokenCacheProvider {

        /// <summary>
        /// Create token store in provided distributed cache
        /// </summary>
        /// <param name="cache">Cache</param>
        public AdalTokenCache(ICache cache) {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <inheritdoc/>
        public TokenCache GetCache(string name) {
            return new DistributedTokenCacheEntry(this, name);
        }

        /// <summary>
        /// Cache implementation
        /// </summary>
        private class DistributedTokenCacheEntry : TokenCache {

            /// <summary>
            /// Create token cache entry in provided distributed cache
            /// </summary>
            /// <param name="store">cache to create entry in</param>
            /// <param name="cacheKey">Key in cache</param>
            public DistributedTokenCacheEntry(AdalTokenCache store, string cacheKey) {

                AfterAccess = args => {
                    if (HasStateChanged) {
                        if (Count > 0) {
                            // Write our new token cache state to the cache
                            store._cache.SetAsync(cacheKey, SerializeMsalV3(),
                                DateTimeOffset.UtcNow + TimeSpan.FromDays(1)).Wait();
                        }
                        else {
                            // The Token cache is empty so remove ourselves.
                            store._cache.RemoveAsync(cacheKey);
                        }
                        HasStateChanged = false;
                    }
                };

                BeforeAccess = args => {
                    var cacheData = store._cache.GetAsync(cacheKey).Result;
                    if (cacheData != null) {
                        try {
                            DeserializeMsalV3(cacheData);
                        }
                        catch {
                            // Fall back to previous format
                            DeserializeMsalV2(cacheData);
                        }
                    }
                };
            }
        }

        private readonly ICache _cache;
    }
}
