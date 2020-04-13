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
    public class PassThroughTokenProvider : ITokenProvider {

        /// <summary>
        /// Create auth provider. Need to also inject the http context accessor
        /// to be able to get at the http context here.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="config"></param>
        public PassThroughTokenProvider(IHttpContextAccessor ctx,
            IClientAuthConfig config = null) {
            _schemes = config?.ClientSchemes?.Select(s => s.Scheme).Distinct().ToList();
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {
            const string kAccessTokenKey = "access_token";

            string token = null;
            if (_schemes == null) {
                token = await _ctx.HttpContext.GetTokenAsync(kAccessTokenKey);
            }
            else {
                foreach (var scheme in _schemes) {
                    token = await _ctx.HttpContext.GetTokenAsync(scheme, kAccessTokenKey);
                    if (token != null) {
                        break; // Use first found token
                    }
                }
            }
            if (string.IsNullOrEmpty(token)) {
                return null;
            }
            return JwtSecurityTokenEx.Parse(token);
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

        private readonly List<string> _schemes;
        private readonly IHttpContextAccessor _ctx;
    }

}
