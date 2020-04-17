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
    public partial class AdalUserTokenClient : ITokenClient {

        /// <summary>
        /// Create auth provider. Need to also inject the http context accessor
        /// to be able to get at the http context here.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="store"></param>
        /// <param name="schemes"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="handler"></param>
        public AdalUserTokenClient(IHttpContextAccessor ctx, IAdalTokenCacheProvider store,
            IAuthenticationSchemeProvider schemes, IClientAuthConfig config, ILogger logger,
            IAuthChallengeHandler handler = null) {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _schemes = schemes ?? throw new ArgumentNullException(nameof(schemes));
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _handler = handler ?? new NullHandler();
        }

        /// <inheritdoc/>
        public bool Supports(string resource) {
            return _config.Query(resource, AuthScheme.AzureAD).Any();
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {

            var schemes = await _schemes.GetAllSchemesAsync();
            if (!schemes.Any(s => s.Name == AuthScheme.AzureAD)) {
                return null;
            }

            var user = _ctx.HttpContext?.User;
            // User id should be known, we need it to sign in on behalf of them...
            if (user == null) {
                var e = new AuthenticationException("Missing claims principal.");
                _logger.Information(e, "Failed to get token for {resource} ", resource);
                return await _handler.ChallengeAsync(_ctx.HttpContext, resource, AuthScheme.AzureAD, e);
            }

            var cache = _store.GetCache($"OID:{user.GetObjectId()}");
            foreach (var config in _config.Query(resource, AuthScheme.AzureAD)) {
                try {
                    var ctx = CreateAuthenticationContext(config.InstanceUrl,
                        config.TenantId, cache);
                    try {
                        var result = await ctx.AcquireTokenSilentAsync(config.GetAudience(scopes),
                            config.ClientId);
                        _logger.Debug(
                            "Successfully acquired token for {resource} with {config}.",
                            resource, config.GetName());
                        return result.ToTokenResult();
                    }
                    catch (AdalException ex) {
                        if (ex.ErrorCode == AdalError.FailedToAcquireTokenSilently) {
                            if (!string.IsNullOrEmpty(config.ClientSecret)) {
                                try {
                                    const string kAccessTokenKey = "access_token";
                                    var token = await _ctx.HttpContext.GetTokenAsync(kAccessTokenKey);
                                    if (string.IsNullOrEmpty(token)) {
                                        token = _ctx.HttpContext.User.FindFirstValue(kAccessTokenKey);
                                    }
                                    var result = await ctx.AcquireTokenAsync(config.GetAudience(scopes),
                                        new ClientCredential(config.ClientId, config.ClientSecret),
                                        new UserAssertion(token, kGrantType));
                                    _logger.Information(
                                        "Successfully acquired token for {resource} with {config}.",
                                        resource, config.GetName());
                                    return result.ToTokenResult();
                                }
                                catch (AdalException ex2) {
                                    ex = ex2;
                                }
                            }
                        }
                        throw new AuthenticationException(
                            $"Failed to authenticate on behalf of user", ex);
                    }
                    catch (Exception ex2) {
                        throw new AuthenticationException(
                            $"Unexpected error authenticating on behalf of user", ex2);
                    }
                }
                catch (AuthenticationException e) {
                    _logger.Debug(e, "Failed to get token for {resource} with {config}.",
                        resource, config.GetName());
                    var result = await _handler.ChallengeAsync(_ctx.HttpContext, resource, AuthScheme.AzureAD, e);
                    if (result != null) {
                        return result;
                    }
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task InvalidateAsync(string resource) {
            var user = _ctx.HttpContext?.User;
            if (user != null) {
                var cache = _store.GetCache($"OID:{user.GetObjectId()}");
                cache?.Clear();
            }
            await _handler.ChallengeAsync(_ctx.HttpContext, resource, AuthScheme.AzureAD);
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
        /// Null handling
        /// </summary>
        private sealed class NullHandler : IAuthChallengeHandler {
            /// <inheritdoc/>
            public Task<TokenResultModel> ChallengeAsync(HttpContext context, string resource,
                string scheme, AuthenticationException ex = null) {
                return kNull;
            }
            private static readonly Task<TokenResultModel> kNull =
                Task.FromResult<TokenResultModel>(null);
        }

        private const string kGrantType = "urn:ietf:params:oauth:grant-type:jwt-bearer";
        private const string kDefaultAuthorityUrl = "https://login.microsoftonline.com/";

        private readonly IHttpContextAccessor _ctx;
        private readonly IAdalTokenCacheProvider _store;
        private readonly IAuthenticationSchemeProvider _schemes;
        private readonly ILogger _logger;
        private readonly IClientAuthConfig _config;
        private readonly IAuthChallengeHandler _handler;
    }
}
