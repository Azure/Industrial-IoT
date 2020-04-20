// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Storage;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Identity.Client;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Authentication;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Threading.Tasks;

    /// <summary>
    /// Authenticate on behalf of current logged in claims principal to another
    /// service. This uses the behalf_of flow defined in xxx.
    /// </summary>
    public partial class MsalUserTokenClient : ITokenClient, ICodeRedemption {

        /// <inheritdoc/>
        public string Scheme => AuthScheme.AzureAD;

        /// <summary>
        /// Create auth provider. Need to also inject the http context accessor
        /// to be able to get at the http context here.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="cache"></param>
        /// <param name="schemes"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="handler"></param>
        public MsalUserTokenClient(IHttpContextAccessor ctx, ICache cache,
            IAuthenticationSchemeProvider schemes, IClientAuthConfig config,
            ILogger logger, IAuthChallengeHandler handler = null) {
            _schemes = schemes ?? throw new ArgumentNullException(nameof(schemes));
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _handler = handler ?? new NullHandler();
        }

        /// <inheritdoc/>
        public bool Supports(string resource) {
            return _config.Query(resource, Scheme).Any();
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {
            var schemes = await _schemes.GetAllSchemesAsync();
            if (!schemes.Any(s => s.Name == Scheme)) {
                return null;
            }
            foreach (var config in _config.Query(resource, Scheme)) {
                var decorator = CreateConfidentialClientApplication(_ctx.HttpContext.User, config);
                try {
                    var result = await AcquireTokenSilentAsync(decorator.Client,
                        _ctx.HttpContext.User, scopes, config.TenantId);
                    if (result != null) {
                        _logger.Debug(
                            "Successfully acquired token {resource} with {config}.",
                            resource, config.GetName());
                        return result.ToTokenResult();
                    }
                }
                catch (MsalUiRequiredException e) {
                    var validatedToken = (JwtSecurityToken)_ctx.HttpContext.Items["pass_through"];
                    if (validatedToken != null) {
                        // to get a token for a web api on behalf of the user inside a web api.
                        // In the case the token is a JWE (encrypted token), we use the decrypted token.
                        var accessToken = validatedToken.InnerToken == null ?
                            validatedToken.RawData : validatedToken.InnerToken.RawData;

                        var result = await decorator.Client.AcquireTokenOnBehalfOf(
                            scopes.Except(kScopesRequestedByMsal), new UserAssertion(accessToken))
                            .ExecuteAsync();
                        _logger.Information(
                            "Successfully acquired on behalf token for {resource} with {config}.",
                                resource, config.GetName());
                        return result.ToTokenResult();
                    }
                    else {
                        _logger.Debug(e, "Failed to get token for {resource} with {config}.",
                            resource, config.GetName());
                        // Challenge the scheme inside the web app
                        var result = await _handler.ChallengeAsync(
                            _ctx.HttpContext, resource, AuthScheme.AzureAD);
                        if (result != null) {
                            return result;
                        }
                    }
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> RedeemCodeForUserAsync(ClaimsPrincipal user,
            string code, IEnumerable<string> scopes) {
            var schemes = await _schemes.GetAllSchemesAsync();
            if (!schemes.Any(s => s.Name == Scheme)) {
                return null;
            }
            foreach (var config in _config.Query(Scheme)) {
                var decorator = CreateConfidentialClientApplication(user, config);
                try {
                    var result = await decorator.Client
                         .AcquireTokenByAuthorizationCode(scopes.Except(kScopesRequestedByMsal), code)
                         .ExecuteAsync();
                    if (result != null) {
                        return result.ToTokenResult();
                    }
                }
                catch (Exception e) {
                    _logger.Error(e, "Failed to get token for code with {config}.",
                         config.GetName());
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task SignOutUserAsync(ClaimsPrincipal user) {
            foreach (var config in _config.Query(Scheme)) {
                var decorator = CreateConfidentialClientApplication(user, config);
                var account = await decorator.Client.GetAccountAsync(user.GetMsalAccountId());

                if (account == null) {
                    var accounts = await decorator.Client.GetAccountsAsync();
                    account = accounts.FirstOrDefault(a => a.Username == user.GetLoginHint());
                }

                if (account != null) {
                    await decorator.Client.RemoveAsync(account);
                    await decorator.ClearCacheAsync();
                }
            }
        }

        /// <inheritdoc/>
        public async Task InvalidateAsync(string resource) {
            await _handler.ChallengeAsync(_ctx.HttpContext, resource, AuthScheme.AzureAD);
        }

        /// <summary>
        /// Gets an access token from cache on behalf of the user.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="user">User to get token for</param>
        /// <param name="scopes">Scopes for api to call</param>
        /// <param name="tenant"></param>
        private async Task<AuthenticationResult> AcquireTokenSilentAsync(
            IConfidentialClientApplication application, ClaimsPrincipal user,
            IEnumerable<string> scopes, string tenant) {
            var account = await application.GetUserAccountAsync(user);
            if (account == null) {
                return null;
            }
            if (!string.IsNullOrWhiteSpace(tenant)) {
                // Acquire an access token as another authority
                var authority = application.Authority.Replace(
                    new Uri(application.Authority).PathAndQuery, $"/{tenant}/");
                return await application
                    .AcquireTokenSilent(scopes.Except(kScopesRequestedByMsal), account)
                    .WithAuthority(authority)
                    .ExecuteAsync();
            }
            return await application
                .AcquireTokenSilent(scopes.Except(kScopesRequestedByMsal), account)
                .ExecuteAsync();
        }

        /// <summary>
        /// Get account info for the user
        /// </summary>
        /// <param name="application"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task<IAccount> GetUserAccountAsync(IClientApplicationBase application, ClaimsPrincipal user) {
            var accountId = user.GetMsalAccountId();
            IAccount account = null;
            if (accountId != null) {
                account = await application.GetAccountAsync(accountId);
                // Special case for guest users as the Guest oid / tenant id are not surfaced.
                if (account == null) {
                    var loginHint = user.GetLoginHint();
                    if (loginHint == null) {
                        throw new ArgumentNullException(nameof(loginHint));
                    }
                    var accounts = await application.GetAccountsAsync();
                    account = accounts.FirstOrDefault(a => a.Username == loginHint);
                }
            }
            return null;
        }

        /// <summary>
        /// Create public client
        /// </summary>
        /// <param name="user"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private MsalConfidentialClientDecorator CreateConfidentialClientApplication(
            ClaimsPrincipal user, IOAuthClientConfig config) {
            return new MsalConfidentialClientDecorator(ConfidentialClientApplicationBuilder
                .Create(config.ClientId).WithAuthority($"{config.GetAuthorityUrl()}/").Build(),
                    _cache, config.ClientId, user.GetObjectId());
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

        private static readonly string[] kScopesRequestedByMsal = new [] {
            "openid", "profile", "offline_acccess"
        };

        private readonly ILogger _logger;
        private readonly IClientAuthConfig _config;
        private readonly IAuthenticationSchemeProvider _schemes;
        private readonly IHttpContextAccessor _ctx;
        private readonly ICache _cache;
        private readonly IAuthChallengeHandler _handler;

    }
}
