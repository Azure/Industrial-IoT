// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Auth.Storage {
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Identity.Client;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Decorates a client with a cache to keep tokens
    /// </summary>
    public class MsalClientApplicationDecorator<T> where T : IClientApplicationBase {

        /// <inheritdoc/>
        public T Client { get; }

        /// <summary>
        /// Create token cache
        /// </summary>
        /// <param name="client"></param>
        /// <param name="cache"></param>
        /// <param name="userKey"></param>
        public MsalClientApplicationDecorator(T client, ICache cache, string userKey) {
            Client = client;
            _userTokenCache = new MsalTokenCacheDecorator(cache,
                client.UserTokenCache, userKey);
        }

        /// <summary>
        /// Clear the cache
        /// </summary>
        /// <returns></returns>
        public virtual async Task ClearCacheAsync() {
            await _userTokenCache.ClearAsync();
        }

        /// <summary>
        /// Token cache provider
        /// </summary>
        protected sealed class MsalTokenCacheDecorator {

            /// <summary>
            /// Create token cache
            /// </summary>
            /// <param name="cache"></param>
            /// <param name="tokenCache"></param>
            /// <param name="cacheKey"></param>
            public MsalTokenCacheDecorator(ICache cache, ITokenCache tokenCache, string cacheKey) {
                _cache = cache;
                _cacheKey = cacheKey;
                tokenCache.SetBeforeAccessAsync(OnBeforeAccessAsync);
                tokenCache.SetAfterAccessAsync(OnAfterAccessAsync);
            }

            private async Task OnAfterAccessAsync(TokenCacheNotificationArgs args) {
                // if the access operation resulted in a cache update
                if (args.HasStateChanged) {
                    if (!string.IsNullOrWhiteSpace(_cacheKey)) {
                        await _cache.SetAsync(_cacheKey, args.TokenCache.SerializeMsalV3(),
                            DateTimeOffset.UtcNow + TimeSpan.FromDays(1));
                    }
                }
            }

            private async Task OnBeforeAccessAsync(TokenCacheNotificationArgs args) {
                if (!string.IsNullOrEmpty(_cacheKey)) {
                    var tokenCacheBytes = await _cache.GetAsync(_cacheKey);
                    args.TokenCache.DeserializeMsalV3(tokenCacheBytes, shouldClearExistingCache: true);
                }
            }

            /// <inheritdoc/>
            public async Task ClearAsync() {
                await _cache.RemoveAsync(_cacheKey);
            }

            private readonly ICache _cache;
            private readonly string _cacheKey;
        }

        private readonly MsalTokenCacheDecorator _userTokenCache;
    }
}
