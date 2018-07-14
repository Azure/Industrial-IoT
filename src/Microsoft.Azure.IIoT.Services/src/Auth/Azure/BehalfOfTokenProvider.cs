// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Auth.Azure {
    using Microsoft.Azure.IIoT.Auth.Azure;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Http;
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
        /// <param name="ctx"></param>
        /// <param name="store"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public BehalfOfTokenProvider(IHttpContextAccessor ctx, ITokenCacheProvider store,
            IClientConfig config, ILogger logger) {
            _store = store ?? throw new ArgumentNullException(nameof(store));
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

            var user = _ctx.HttpContext.User;
            // User id should be known, we need it to sign in on behalf of...
            if (user == null) {
                throw new AuthenticationException("Missing claims principal.");
            }

            var name = user.FindFirstValue(ClaimTypes.Upn) ??
                user.FindFirstValue(ClaimTypes.Email);

            const string kAccessTokenKey = "access_token";
            var token = await _ctx.HttpContext.GetTokenAsync(kAccessTokenKey);
            if (string.IsNullOrEmpty(token)) {
                // TODO: The above always fails currently. Find out what we do wrongly.
                // This is the 1.1 workaround...
                token = user?.FindFirstValue(kAccessTokenKey);
                if (string.IsNullOrEmpty(token)) {
                    throw new AuthenticationException(
                        $"No auth on behalf of {name} without token...");
                }
            }

            var cache = _store.GetCache(
                $"OID:{user.GetObjectId()}::AUD:{user.GetAudienceId()}");
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

        private readonly IHttpContextAccessor _ctx;
        private readonly ITokenCacheProvider _store;
        private readonly ILogger _logger;
        private readonly IClientConfig _config;
    }

}
