// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Auth.Storage;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Identity.Client;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Threading.Tasks;

    /// <summary>
    /// Authenticate on behalf of current logged in claims principal to another
    /// service. This uses the behalf_of flow defined in xxx.
    /// </summary>
    public class MsalUserTokenClient : ITokenClient, IUserTokenClient {

        /// <inheritdoc/>
        public string Provider => AuthProvider.AzureAD;

        /// <summary>
        /// Create auth provider. Need to also inject the http context accessor
        /// to be able to get at the http context here.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="cache"></param>
        /// <param name="schemes"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public MsalUserTokenClient(IHttpContextAccessor ctx, ICache cache,
            IAuthenticationSchemeProvider schemes, IClientAuthConfig config, ILogger logger) {
            _schemes = schemes ?? throw new ArgumentNullException(nameof(schemes));
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc/>
        public bool Supports(string resource) {
            return _config.Query(resource, Provider).Any();
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {
            if (_ctx.HttpContext?.User == null) {
                return null;
            }
            var schemes = await _schemes.GetAllSchemesAsync();
            if (!schemes.Any(s => s.Name == Provider)) {
                return null;
            }
            var exceptions = new List<Exception>();
            foreach (var config in _config.Query(resource, Provider)) {
                var decorator = CreateConfidentialClientApplication(_ctx.HttpContext.User,
                    config, CreateRedirectUrl());
                try {
                    var result = await AcquireTokenSilentAsync(decorator.Client,
                        _ctx.HttpContext.User, GetScopes(config, scopes), config.TenantId);
                    if (result != null) {
                        _logger.Debug(
                            "Successfully acquired token {resource} with {config}.",
                            resource, config.GetName());
                        return result.ToTokenResult();
                    }
                }
                catch (MsalUiRequiredException) {
                    // Expected if not in cache - continue down
                }
                catch (Exception e) {
                    _logger.Debug(e, "Failed to get token silently for {resource} with {config}.",
                        resource, config.GetName());
                    exceptions.Add(e);
                    continue;
                }

                var validatedToken = (JwtSecurityToken)_ctx.HttpContext?.Items["pass_through"];
                if (validatedToken != null) {
                    // to get a token for a web api on behalf of the user inside a web api.
                    // In the case the token is a JWE (encrypted token), we use the decrypted token.
                    var accessToken = validatedToken.InnerToken == null ?
                        validatedToken.RawData : validatedToken.InnerToken.RawData;

                    try {
                        var result = await decorator.Client.AcquireTokenOnBehalfOf(
                            GetScopes(config, scopes), new UserAssertion(accessToken)).ExecuteAsync();
                        _logger.Information(
                            "Successfully acquired on behalf token for {resource} with {config}.",
                                resource, config.GetName());
                        return result.ToTokenResult();
                    }
                    catch (Exception ex) {
                        exceptions.Add(ex);
                        continue;
                    }
                }
                else {
                    _logger.Debug("Could not find token for {resource} with {config} in http context.",
                        resource, config.GetName());
                }
            }
            if (exceptions.Count != 0) {
                throw new AggregateException(exceptions);
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> RedeemCodeForUserAsync(ClaimsPrincipal user,
            string code, IEnumerable<string> scopes) {
            if (user == null) {
                return null;
            }
            var schemes = await _schemes.GetAllSchemesAsync();
            if (!schemes.Any(s => s.Name == Provider)) {
                return null;
            }
            foreach (var config in _config.Query(Provider)) {
                var decorator = CreateConfidentialClientApplication(user, config, CreateRedirectUrl());
                try {
                    var result = await decorator.Client
                         .AcquireTokenByAuthorizationCode(GetScopes(config, scopes), code)
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
        public async Task<TokenResultModel> GetUserTokenAsync(ClaimsPrincipal user,
            IEnumerable<string> scopes) {
            if (user == null) {
                return null;
            }
            foreach (var config in _config.Query(Provider)) {
                var decorator = CreateConfidentialClientApplication(user, config);
                try {
                    var result = await AcquireTokenSilentAsync(decorator.Client,
                        user, GetScopes(config, scopes), config.TenantId);
                    if (result != null) {
                        return result.ToTokenResult();
                    }
                }
                catch {
                    continue;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task SignOutUserAsync(ClaimsPrincipal user) {
            if (user == null) {
                return;
            }
            foreach (var config in _config.Query(Provider)) {
                var decorator = CreateConfidentialClientApplication(user, config);
                var account = await decorator.Client.GetAccountAsync(user.GetMsalAccountId());

                if (account == null) {
#pragma warning disable CS0618 // Type or member is obsolete
                    var accounts = await decorator.Client.GetAccountsAsync();
#pragma warning restore CS0618 // Type or member is obsolete
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
            if (_ctx.HttpContext?.User == null) {
                return;
            }
            foreach (var config in _config.Query(resource, Provider)) {
                var decorator = CreateConfidentialClientApplication(_ctx.HttpContext.User, config);
                await decorator.ClearCacheAsync();
            }
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
            var account = await GetUserAccountAsync(application, user);
            if (account == null) {
                return null;
            }
            if (!string.IsNullOrWhiteSpace(tenant)) {
                // Acquire an access token as another authority
                var authority = application.Authority.Replace(
                    new Uri(application.Authority).PathAndQuery, $"/{tenant}/");
                return await application
                    .AcquireTokenSilent(scopes, account)
                    .WithAuthority(authority)
                    .ExecuteAsync();
            }
            return await application
                .AcquireTokenSilent(scopes, account)
                .ExecuteAsync();
        }

        /// <summary>
        /// Create redirect url
        /// </summary>
        /// <returns></returns>
        private string CreateRedirectUrl() {
            var request = _ctx.HttpContext?.Request;
            if (request == null) {
                return null;
            }
            var redirectUri = new UriBuilder(request.Scheme, request.Host.Host,
                request.Host.Port ?? -1, request.PathBase + "/signin-oidc").ToString();
            return redirectUri;
        }

        /// <summary>
        /// Get scopes
        /// </summary>
        /// <param name="config"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        private IEnumerable<string> GetScopes(IOAuthClientConfig config,
            IEnumerable<string> scopes) {
            var requestedScopes = new HashSet<string>();
            if (scopes != null) {
                foreach (var scope in scopes.Except(ScopesRequestedByMsal)) {
                    requestedScopes.Add(scope);
                }
            }
            if (config.Audience != null) {
                requestedScopes.Add(config.Audience + "/.default");
            }
            return requestedScopes;
        }

        /// <summary>
        /// Get account info for the user
        /// </summary>
        /// <param name="application"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task<IAccount> GetUserAccountAsync(IClientApplicationBase application,
            ClaimsPrincipal user) {
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
            return account;
        }

        /// <summary>
        /// Create public client
        /// </summary>
        /// <param name="user"></param>
        /// <param name="config"></param>
        /// <param name="redirectUri"></param>
        /// <returns></returns>
        private MsalConfidentialClientDecorator CreateConfidentialClientApplication(
            ClaimsPrincipal user, IOAuthClientConfig config, string redirectUri = null) {
            var builder = ConfidentialClientApplicationBuilder.Create(config.ClientId);
            if (redirectUri != null) {
                builder = builder.WithRedirectUri(redirectUri);
            }
            builder = builder
                .WithClientSecret(config.ClientSecret)
                .WithTenantId(config.TenantId)
              //  .WithHttpClientFactory(...)
                .WithAuthority($"{config.GetAuthorityUrl()}/")
                ;
            return new MsalConfidentialClientDecorator(builder.Build(), _cache, config.ClientId,
                user.GetObjectId());
        }

        /// <summary> Scopes requested internally already </summary>
        public static readonly string[] ScopesRequestedByMsal = new [] {
            "openid", "profile", "offline_acccess"
        };

        private readonly ILogger _logger;
        private readonly IClientAuthConfig _config;
        private readonly IAuthenticationSchemeProvider _schemes;
        private readonly IHttpContextAccessor _ctx;
        private readonly ICache _cache;
    }
}
