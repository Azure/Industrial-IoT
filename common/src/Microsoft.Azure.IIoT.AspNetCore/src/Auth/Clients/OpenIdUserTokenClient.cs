// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Microsoft.Azure.IIoT.AspNetCore.Auth.Clients
{
    using global::IdentityModel;
    using global::IdentityModel.Client;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Authentication;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements basic token management logic
    /// </summary>
    public class OpenIdUserTokenClient : ITokenClient
    {
        /// <summary>
        /// Http client factory
        /// </summary>
        public IHttpClientFactory Http { get; set; }

        /// <summary>
        /// Create token provider
        /// </summary>
        /// <param name="config"></param>
        /// <param name="ctx"></param>
        /// <param name="oidc"></param>
        /// <param name="schemes"></param>
        /// <param name="clock"></param>
        /// <param name="logger"></param>
        public OpenIdUserTokenClient(IClientAuthConfig config, IHttpContextAccessor ctx,
            IOptionsMonitor<OpenIdConnectOptions> oidc, IAuthenticationSchemeProvider schemes,
            ISystemClock clock, ILogger logger)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _oidc = oidc ?? throw new ArgumentNullException(nameof(oidc));
            _schemes = schemes ?? throw new ArgumentNullException(nameof(schemes));
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Http = new HttpClientFactory(logger); // TODO: Use logger factory here
        }

        /// <inheritdoc/>
        public bool Supports(string resource)
        {
            return _config.Query(resource, AuthProvider.AuthService).Any();
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes)
        {
            var user = _ctx.HttpContext?.User;
            if (user == null)
            {
                return null;
            }

            var schemes = await _schemes.GetAllSchemesAsync().ConfigureAwait(false);
            if (!schemes.Any(s => s.Name == AuthProvider.AuthService))
            {
                return null;
            }

            if (!user.Identity.IsAuthenticated)
            {
                _logger.LogDebug("User is not authenticated.");
                return null;
            }
            var userName = user.FindFirst(JwtClaimTypes.Name)?.Value ??
                user.FindFirst(JwtClaimTypes.Subject)?.Value ?? "unknown";

            var (accessToken, expiration, refreshToken) = await GetTokenFromCacheAsync().ConfigureAwait(false);
            if (refreshToken == null)
            {
                _logger.LogDebug("No token data found in user token store.");
                return null;
            }

            var dtRefresh = expiration.Value.Subtract(TimeSpan.FromMinutes(1));
            if (dtRefresh >= _clock.UtcNow)
            {
                // Token still valid - use it.
                var token = JwtSecurityTokenEx.Parse(accessToken);
                token.Cached = true;
                return token;
            }

            var exceptions = new List<Exception>();
            foreach (var config in _config.Query(resource, AuthProvider.AuthService))
            {
                try
                {
                    _logger.LogDebug("Token for user {User} needs refreshing.", userName);
                    try
                    {
                        accessToken = await kRequests.GetOrAdd(refreshToken, t =>
                        {
                            return new Lazy<Task<string>>(async () =>
                            {
                                var refreshed = await RefreshUserAccessTokenAsync(t, config).ConfigureAwait(false);
                                return refreshed.AccessToken;
                            });
                        }).Value.ConfigureAwait(false);
                        var token = JwtSecurityTokenEx.Parse(accessToken);
                        token.Cached = true;
                        _logger.LogInformation(
                            "Successfully refreshed token for {Resource} with {Config}.",
                            resource, config.GetName());
                        return token;
                    }
                    finally
                    {
                        kRequests.TryRemove(refreshToken, out _);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogDebug(e, "Failed to get token for {Resource} with {Config}.",
                        resource, config.GetName());
                    exceptions.Add(e);
                    continue;
                }
            }
            if (exceptions.Count != 0)
            {
                throw new AggregateException(exceptions);
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task InvalidateAsync(string resource)
        {
            var (_, _, refreshToken) = await GetTokenFromCacheAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(refreshToken))
            {
                return;
            }
            foreach (var config in _config.Query(resource, AuthProvider.AuthService))
            {
                await RevokeRefreshTokenAsync(refreshToken, config).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Refresh access token
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private async Task<TokenResponse> RefreshUserAccessTokenAsync(string refreshToken,
            IOAuthClientConfig config)
        {
            var client = Http.CreateClient("token_client");
            var response = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = config.GetAuthorityUrl(),
                ClientId = config.ClientId,
                ClientSecret = config.ClientSecret,
                RefreshToken = refreshToken
            }).ConfigureAwait(false);
            if (!response.IsError)
            {
                await StoreTokenAsync(response.AccessToken, response.ExpiresIn,
                    response.RefreshToken).ConfigureAwait(false);
            }
            else
            {
                _logger.LogError("Error refreshing access token. Error = {Error}",
                    response.Error);
            }
            return response;
        }

        /// <summary>
        /// Revoke token
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private async Task RevokeRefreshTokenAsync(string refreshToken,
            IOAuthClientConfig config)
        {
            var client = Http.CreateClient("token_client");
            var configuration = await GetOpenIdConfigurationAsync(config.Provider).ConfigureAwait(false);
            if (configuration == null)
            {
                _logger.LogInformation(
                    "Failed to revoke token for scheme {SchemeName}", config.Provider);
                return;
            }
            var response = await client.RevokeTokenAsync(new TokenRevocationRequest
            {
                Address = configuration
                    .AdditionalData[OidcConstants.Discovery.RevocationEndpoint].ToString(),
                ClientId = config.ClientId,
                ClientSecret = config.ClientSecret,
                Token = refreshToken,
                TokenTypeHint = OidcConstants.TokenTypes.RefreshToken
            }).ConfigureAwait(false);
            if (response.IsError)
            {
                _logger.LogError("Error revoking refresh token. Error = {Error}",
                    response.Error);
            }
        }

        /// <summary>
        /// Retrieves configuration from a named OpenID Connect handler
        /// </summary>
        /// <param name="schemeName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<OpenIdConnectConfiguration> GetOpenIdConfigurationAsync(
            string schemeName)
        {
            var options = _oidc.Get(schemeName);
            if (options == null)
            {
                return null;
            }
            try
            {
                return await options.ConfigurationManager.GetConfigurationAsync(default).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e,
                    "Unable to load OpenID configuration for scheme {SchemeName}", schemeName);
                return null;
            }
        }

        /// <summary>
        /// Get tokens for current user
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<(string, DateTimeOffset?, string)> GetTokenFromCacheAsync()
        {
            if (_ctx.HttpContext == null)
            {
                return (null, null, null);
            }
            var result = await _ctx.HttpContext.AuthenticateAsync(AuthProvider.AuthService).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return (null, null, null);
            }
            var tokens = result.Properties.GetTokens();
            if (tokens?.Any() != true)
            {
                throw new InvalidOperationException("No tokens found.");
            }
            var accessToken = tokens
                .SingleOrDefault(t => t.Name == OpenIdConnectParameterNames.AccessToken);
            if (accessToken == null)
            {
                throw new InvalidOperationException("No access token found.");
            }
            var refreshToken = tokens
                .SingleOrDefault(t => t.Name == OpenIdConnectParameterNames.RefreshToken);
            if (refreshToken == null)
            {
                throw new InvalidOperationException("No refresh token found.");
            }
            var expiresAt = tokens
                .SingleOrDefault(t => t.Name == "expires_at");
            if (expiresAt == null)
            {
                throw new InvalidOperationException("No expires_at value found.");
            }
            var dtExpires = DateTimeOffset.Parse(
                expiresAt.Value, CultureInfo.InvariantCulture);
            return (accessToken.Value, dtExpires, refreshToken.Value);
        }

        /// <summary>
        /// Store user tokens
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="expiresIn"></param>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        /// <exception cref="AuthenticationException"></exception>
        private async Task StoreTokenAsync(string accessToken, int expiresIn,
            string refreshToken)
        {
            if (_ctx.HttpContext == null)
            {
                throw new AuthenticationException("can't store tokens. No context");
            }
            var result = await _ctx.HttpContext.AuthenticateAsync().ConfigureAwait(false);
            if (!result.Succeeded)
            {
                throw new AuthenticationException("can't store tokens. User is anonymous");
            }
            result.Properties.UpdateTokenValue(
                OpenIdConnectParameterNames.AccessToken, accessToken);
            if (refreshToken != null)
            {
                result.Properties.UpdateTokenValue(
                    OpenIdConnectParameterNames.RefreshToken, refreshToken);
            }
            var newExpiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(expiresIn);
            result.Properties.UpdateTokenValue("expires_at",
                newExpiresAt.ToString("o", CultureInfo.InvariantCulture));

            await _ctx.HttpContext.SignInAsync(result.Principal, result.Properties).ConfigureAwait(false);
        }

        static readonly ConcurrentDictionary<string, Lazy<Task<string>>> kRequests = new();
        private readonly IAuthenticationSchemeProvider _schemes;
        private readonly IOptionsMonitor<OpenIdConnectOptions> _oidc;
        private readonly ISystemClock _clock;
        private readonly IHttpContextAccessor _ctx;
        private readonly ILogger _logger;
        private readonly IClientAuthConfig _config;
    }
}
