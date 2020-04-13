// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Http;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Authentication;
    using System.Security.Claims;
    using System.Threading.Tasks;

    /// <summary>
    /// Authenticate on behalf of current logged in claims principal to another
    /// service. This uses the behalf_of flow defined in xxx.
    /// </summary>
    public partial class BehalfOfTokenProvider : ITokenProvider {

        /// <summary>
        /// Create auth provider. Need to also inject the http context accessor
        /// to be able to get at the http context here.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="store"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="handler"></param>
        public BehalfOfTokenProvider(IHttpContextAccessor ctx, ITokenCacheProvider store,
            IClientAuthConfig config, ILogger logger, IAuthenticationErrorHandler handler = null) {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _handler = handler ?? new ThrowHandler();
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {

            var user = _ctx.HttpContext?.User;
            // User id should be known, we need it to sign in on behalf of them...
            if (user == null) {
                var e = new AuthenticationException("Missing claims principal.");
                _logger.Information(e, "Failed to get token for {resource} ", resource);
                _handler.Handle(_ctx.HttpContext, e);
                return null;
            }

            var name = user.FindFirstValue(ClaimTypes.Upn);
            if (string.IsNullOrEmpty(name)) {
                name = user.FindFirstValue(ClaimTypes.Email);
            }
            if (string.IsNullOrEmpty(name)) {
                name = user.Identity?.Name;
            }

            const string kAccessTokenKey = "access_token";
            var token = await _ctx.HttpContext.GetTokenAsync(kAccessTokenKey);
            if (string.IsNullOrEmpty(token)) {
                var e = new AuthenticationException(
                    $"No auth on behalf of {name} without token...");
                _logger.Information(e, "Failed to get token for {resource} ", resource);
                _handler.Handle(_ctx.HttpContext, e);
                return null;
            }

            var cache = _store.GetCache($"OID:{user.GetObjectId()}");
            foreach (var config in _config.Query(resource, AuthScheme.Aad)) {
                try {
                    var ctx = CreateAuthenticationContext(config.InstanceUrl,
                        config.TenantId, cache);
                    try {
                        var result = await ctx.AcquireTokenSilentAsync(config.Audience,
                            config.AppId);
                        return result.ToTokenResult();
                    }
                    catch (AdalException ex) {
                        if (ex.ErrorCode == AdalError.FailedToAcquireTokenSilently) {
                            if (_handler.AcquireTokenIfSilentFails &&
                                !string.IsNullOrEmpty(config.AppSecret)) {
                                try {
                                    var result = await ctx.AcquireTokenAsync(config.Audience,
                                        new ClientCredential(config.AppId, config.AppSecret),
                                        new UserAssertion(token, kGrantType, name));
                                    return result.ToTokenResult();
                                }
                                catch (AdalException ex2) {
                                    ex = ex2;
                                }
                            }
                        }
                        throw new AuthenticationException(
                            $"Failed to authenticate on behalf of {name}", ex);
                    }
                    catch (Exception ex2) {
                        throw new AuthenticationException(
                            $"Unexpected error authenticating on behalf of {name}", ex2);
                    }
                }
                catch (AuthenticationException e) {
                    _logger.Information(e, "Failed to get token for {resource} ", resource);
                    _handler.Handle(_ctx.HttpContext, e);
                    continue;
                }
            }
            return null;
        }

        /// <summary>
        /// Helper to create authentication context
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="authorityUrl"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        private static AuthenticationContext CreateAuthenticationContext(
            string authorityUrl, string tenantId, TokenCache cache) {
            if (string.IsNullOrEmpty(authorityUrl)) {
                authorityUrl = kDefaultAuthorityUrl;
            }
            else if (!authorityUrl.EndsWith("/", StringComparison.Ordinal)) {
                authorityUrl += "/";
            }
            var uri = new UriBuilder(authorityUrl) {
                Path = tenantId ?? "common"
            };
            var ctx = new AuthenticationContext(uri.ToString(), cache);
            if (tenantId == null && ctx.TokenCache.Count > 0) {
                uri.Path = ctx.TokenCache.ReadItems().First().TenantId;
                ctx = new AuthenticationContext(uri.ToString(), cache);
            }
            return ctx;
        }

        /// <summary>
        /// Invalidate cache entry
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public Task InvalidateAsync(string resource) {
            var user = _ctx.HttpContext?.User;
            if (user != null) {
                var cache = _store.GetCache($"OID:{user.GetObjectId()}");
                cache?.Clear();
            }
            _handler.Invalidate(_ctx.HttpContext);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Throw exception
        /// </summary>
        private sealed class ThrowHandler : IAuthenticationErrorHandler {
            /// <inheritdoc/>
            public bool AcquireTokenIfSilentFails => false;

            /// <inheritdoc/>
            public void Handle(HttpContext context, AuthenticationException ex) {
                throw ex;
            }
            /// <inheritdoc/>
            public void Invalidate(HttpContext context) {
            }
        }

        private const string kGrantType = "urn:ietf:params:oauth:grant-type:jwt-bearer";
        private const string kDefaultAuthorityUrl = "https://login.microsoftonline.com/";

        private readonly IHttpContextAccessor _ctx;
        private readonly ITokenCacheProvider _store;
        private readonly ILogger _logger;
        private readonly IClientAuthConfig _config;
        private readonly IAuthenticationErrorHandler _handler;
    }
}
