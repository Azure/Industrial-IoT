// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Azure {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using System;
    using System.Linq;
    using System.Security.Authentication;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// Authenticate on behalf of current logged in claims principal to another
    /// service. This uses the behalf_of flow defined in xxx.
    /// </summary>
    public class BehalfOfTokenProvider : ITokenProvider {

        /// <summary>
        /// Create auth provider. Need to also inject the http context accessor
        /// to be able to get at the http context here.
        /// </summary>
        /// <param name="logger"></param>
        public BehalfOfTokenProvider(IHttpContextAccessor ctx, IDistributedCache cache,
            IDataProtectionProvider dp, IClientConfig config, ILogger logger) {
            _dp = dp ?? throw new ArgumentNullException(nameof(dp));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrEmpty(_config.ClientId) ||
                string.IsNullOrEmpty(_config.ClientSecret)) {
                _logger.Error("On behalf token provider was not configured with " +
                    "a client id or secret.  No tokens will be obtained. ", () => { });
            }
        }

        /// <summary>
        /// Obtain token from user
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {
            if (string.IsNullOrEmpty(_config.ClientId) ||
                string.IsNullOrEmpty(_config.ClientSecret)) {
                return null;
            }

            // User id should be known, we use it to sign in on behalf of...
            var token = await _ctx.HttpContext.GetTokenAsync("access_token");
            var user = _ctx.HttpContext.User;
            if (user == null || string.IsNullOrEmpty(token)) {
                throw new AuthenticationException("Missing token or claims principal.");
            }
            var name = user.FindFirstValue(ClaimTypes.Upn) ??
                user.FindFirstValue(ClaimTypes.Email);

            var cache = new DistributedTokenCache(_cache,
                $"OID:{user.GetObjectId()}::AUD:{user.GetAudienceId()}", _dp);
            var ctx = CreateAuthenticationContext(_config.Authority,
                _config.TenantId, cache);

            try {
                var result = await ctx.AcquireTokenAsync(resource,
                    new ClientCredential(_config.ClientId, _config.ClientSecret),
                    new UserAssertion(token, kGrantType, name));
                return result.ToTokenResult();
            }
            catch (AdalException ex) {
                throw new AuthenticationException(
                    $"Failed to authenticate on behalf of {name}", ex);
            }
        }

        /// <summary>
        /// Helper to create authentication context
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        private static AuthenticationContext CreateAuthenticationContext(
            string authority, string tenantId, TokenCache cache) {
            var tenant = tenantId ?? "common";
            if (string.IsNullOrEmpty(authority)) {
                authority = kAuthority;
            }
            var ctx = new AuthenticationContext(authority + tenant, cache);
            if (tenantId == null && ctx.TokenCache.Count > 0) {
                tenant = ctx.TokenCache.ReadItems().First().TenantId;
                ctx = new AuthenticationContext(authority + tenant, cache);
            }
            return ctx;
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


        private const string kGrantType = "urn:ietf:params:oauth:grant-type:jwt-bearer";
        private const string kAuthority = "https://login.microsoftonline.com/";

        private readonly IDataProtectionProvider _dp;
        private readonly IDistributedCache _cache;
        private readonly IHttpContextAccessor _ctx;
        private readonly ILogger _logger;
        private readonly IClientConfig _config;
    }

}
