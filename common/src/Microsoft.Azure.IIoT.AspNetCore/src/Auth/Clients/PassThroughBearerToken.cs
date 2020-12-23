// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Authenticate using the current token.
    /// </summary>
    public class PassThroughBearerToken : ITokenClient {

        /// <summary>
        /// Create auth provider. Need to also inject the http context accessor
        /// to be able to get at the http context here.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="config"></param>
        public PassThroughBearerToken(IHttpContextAccessor ctx,
            IClientAuthConfig config = null) {
            _providers = config?.Providers?.Select(s => s.Provider).Distinct().ToList();
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        /// <inheritdoc/>
        public bool Supports(string resource) {
            return _providers.Any();
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {
            const string kAccessTokenKey = "access_token";
            if (_ctx.HttpContext == null) {
                return null;
            }
            string token = null;
            if (_providers == null) {
                token = await _ctx.HttpContext.GetTokenAsync(kAccessTokenKey);
            }
            else {
                foreach (var provider in _providers) {
                    token = await _ctx.HttpContext.GetTokenAsync(provider, kAccessTokenKey);
                    if (token != null) {
                        break; // Use first found token
                    }
                }
            }
            if (string.IsNullOrEmpty(token)) {
                return null;
            }
            var result = JwtSecurityTokenEx.Parse(token);
            result.Cached = true; // Already cached as part of context
            return result;
        }

        /// <summary>
        /// Invalidate cache entry
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public Task InvalidateAsync(string resource) {
            // TODO
            return Task.CompletedTask;
        }

        private readonly List<string> _providers;
        private readonly IHttpContextAccessor _ctx;
    }

}
