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
    public abstract class AppAuthenticationBase : ITokenClient {

        /// <inheritdoc/>
        protected AppAuthenticationBase(ILogger logger) {
            _logger = logger;
        }

        /// <inheritdoc/>
        public bool Supports(string resource) {
            return Get(resource).Any();
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {
            var exceptions = new List<Exception>();
            foreach (var (config, provider) in Get(resource)) {
                try {
                    var token = await provider.KeyVaultTokenCallback(
                        config.GetAuthorityUrl(true), config.GetAudience(scopes),
                        config.GetScopeNames(scopes)?.FirstOrDefault());
                    if (token == null) {
                        return null;
                    }
                    var result = JwtSecurityTokenEx.Parse(token);
                    if (result.ExpiresOn < DateTime.UtcNow) {
                        return null;
                    }
                    _logger.Information(
                        "Successfully acquired token for {resource} with {config}.",
                        resource, config.GetName());
                    return result;
                }
                catch (Exception ex) {
                    _logger.Debug(ex,
                        "Failed to retrieve token for {resource} using {config}",
                        resource, config.GetName());
                    exceptions.Add(ex);
                    continue;
                }
            }
            if (exceptions.Count != 0) {
                throw new AggregateException(exceptions);
            }
            return null;
        }

        /// <inheritdoc/>
        public Task InvalidateAsync(string resource) {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get provider
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        protected abstract IEnumerable<(IOAuthClientConfig, AzureServiceTokenProvider)> Get(string resource);

        /// <summary> Logger </summary>
        protected readonly ILogger _logger;
    }
}
