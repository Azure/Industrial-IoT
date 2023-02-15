// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Storage;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System;
    using System.Text;
    using System.Linq;

    /// <summary>
    /// Caching token provider
    /// </summary>
    public class CachingTokenProvider : DefaultTokenProvider {

        /// <inheritdoc/>
        public CachingTokenProvider(IEnumerable<ITokenSource> tokenSources,
            ICache cache) : base(tokenSources) {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <inheritdoc/>
        public override async Task<TokenResultModel> GetTokenForAsync(
            string resource, IEnumerable<string> scopes = null) {
            if (string.IsNullOrEmpty(resource)) {
                resource = Http.Resource.Platform;
            }
            var token = await Try.Async(() => GetTokenFromCacheAsync(resource, scopes));
            if (token == null) {
                token = await base.GetTokenForAsync(resource, scopes);
                if (token != null && !token.Cached) {
                    await Try.Async(() => _cache.SetAsync(GetKey(resource),
                        Encoding.UTF8.GetBytes(token.RawToken), token.ExpiresOn));
                }
            }
            return token;
        }

        /// <inheritdoc/>
        public override async Task InvalidateAsync(string resource) {
            if (string.IsNullOrEmpty(resource)) {
                resource = Http.Resource.Platform;
            }
            await _cache.RemoveAsync(GetKey(resource));
            await base.InvalidateAsync(resource);
        }

        /// <summary>
        /// Helper to get token from cache
        /// </summary>
        /// <returns></returns>
        private async Task<TokenResultModel> GetTokenFromCacheAsync(string resource,
            IEnumerable<string> scopes) {
            var cached = await _cache.GetAsync(GetKey(resource));
            if (cached != null) {
                var token = JwtSecurityTokenEx.Parse(Encoding.UTF8.GetString(cached));
                if (token.ExpiresOn >= DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30)) {
                    if (scopes != null) {
                        // TODO: Check token has all scope is part of the token
                        if (!scopes.All(scope => string.IsNullOrEmpty(scope))) {
                            return null;
                        }
                    }
                    return token;
                }
            }
            return null;
        }

        /// <summary>
        /// Create key for resource
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        private string GetKey(string resource) {
            return resource + nameof(CachingTokenProvider);
        }

        private readonly ICache _cache;
    }
}
