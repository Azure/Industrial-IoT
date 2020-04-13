// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {
    using global::IdentityModel.Client;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Authenticate using client credentials
    /// </summary>
    public sealed class ClientCredentialTokenProvider : ITokenProvider {

        /// <summary>
        /// Create console output device code based token provider
        /// </summary>
        /// <param name="http"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public ClientCredentialTokenProvider(IHttpClientFactory http,
            IClientAuthConfig config, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Obtain token from user
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {
            foreach (var config in _config.Query(resource, AuthScheme.AuthService)) {
                if (string.IsNullOrEmpty(config.AppSecret)) {
                    continue;
                }
                try {
                    var client = _http.CreateClient("token_client");
                    var response = await client.RequestClientCredentialsTokenAsync(
                        new ClientCredentialsTokenRequest {
                            Address = $"{config.GetAuthorityUrl()}/connect/token",
                            ClientId = config.AppId,
                            ClientSecret = config.AppSecret
                        });
                    if (response.IsError) {
                        _logger.Error("Error requesting access token for client {clientName}. Error = {error}",
                            resource, response.Error);
                        return null;
                    }
                    return JwtSecurityTokenEx.Parse(response?.AccessToken);
                }
                catch (Exception exc) {
                    _logger.Information(exc, "Failed to get token for {resource}", resource);
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public Task InvalidateAsync(string resource) {
            return Task.CompletedTask;
        }

        private readonly ILogger _logger;
        private readonly IHttpClientFactory _http;
        private readonly IClientAuthConfig _config;
    }
}
