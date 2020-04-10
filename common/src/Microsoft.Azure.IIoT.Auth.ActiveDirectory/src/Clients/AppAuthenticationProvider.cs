// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.Services.AppAuthentication;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Authentication;
    using System.Threading.Tasks;

    /// <summary>
    /// Authenticate using azure service token provider. This can be
    /// used for where you would not have a token from a logged on user,
    /// e.g. in deamon or service to service scenarios.
    ///
    /// This provider works for development, managed service identity
    /// and service principal scenarios.  It can optionally be
    /// configured using a connection string provided as environment
    /// variable or injected configuration.
    ///
    /// For more information check out
    /// https://docs.microsoft.com/en-us/azure/key-vault/service-to-service-authentication
    /// </summary>
    public class AppAuthenticationProvider : ITokenProvider {

        /// <summary>
        /// Create auth provider.
        /// </summary>
        /// <param name="logger"></param>
        public AppAuthenticationProvider(ILogger logger) : this(null, logger) {
        }

        /// <summary>
        /// Create auth provider
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public AppAuthenticationProvider(IClientAuthConfig config, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config?.ClientSchemes?
                .Where(c => c.Scheme == AuthScheme.Aad)
                .Where(c => !string.IsNullOrEmpty(c.AppId))
                .ToDictionary(c => c.Audience ?? string.Empty,
                    c => (c.GetAuthorityUrl(), CreateProvider(c)));
            // Add default entry
            if (_config == null) {
                _config = new Dictionary<string, (string, AzureServiceTokenProvider)> {
                    [string.Empty] =
                        ("https://login.microsoftonline.com/",
                            CreateProvider())
                };
            }
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {
            resource ??= string.Empty;
            if (!_config.TryGetValue(resource, out var entry) &&
                !_config.TryGetValue(string.Empty, out entry)) {
                throw new AuthenticationException("Failed to retrieve handler.");
            }
            var (authorityUrl, provider) = entry;
            try {
                var token = await provider.KeyVaultTokenCallback(
                    authorityUrl, resource, scopes?.FirstOrDefault());
                if (token == null) {
                    throw new AuthenticationException("No token found.");
                }
                return TokenResultModelEx.Parse(token);
            }
            catch (AuthenticationException) {
                throw;
            }
            catch (Exception ex) {
                _logger.Information(ex, "Failed to retrieve token for {resource}",
                    resource);
                throw new AuthenticationException("Unexpected error retrieving token", ex);
            }
        }

        /// <inheritdoc/>
        public Task InvalidateAsync(string resource) {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Have inheriting class override the runas string.
        /// </summary>
        /// <returns></returns>
        protected virtual string NoClientIdRunAs() {
            return null;
        }

        /// <summary>
        /// Helper to create provider
        /// </summary>
        /// <returns></returns>
        private AzureServiceTokenProvider CreateProvider(
            IOAuthClientConfig config = null) {
            // See if configured in environment variable
            var cs = Environment.GetEnvironmentVariable(
                "AzureServicesAuthConnectionString");
            if (string.IsNullOrEmpty(cs)) {
                if (string.IsNullOrEmpty(config?.AppId)) {
                    // Run as dev or current user
                    cs = NoClientIdRunAs();
                }
                else {
                    // Run as app
                    cs = $"RunAs=App;AppId={config.AppId}";
                    if (!string.IsNullOrEmpty(config.TenantId)) {
                        cs += $";TenantId={config.TenantId}";
                    }
                    if (!string.IsNullOrEmpty(config.AppSecret)) {
                        cs += $";AppKey={config.AppSecret}";
                    }
                }
            }
            return new AzureServiceTokenProvider(cs,
                config.GetAuthorityUrl());
        }

        /// <summary>Configuration for derived class</summary>
        protected readonly Dictionary<string,
            (string, AzureServiceTokenProvider)> _config;
        private readonly ILogger _logger;
    }
}
