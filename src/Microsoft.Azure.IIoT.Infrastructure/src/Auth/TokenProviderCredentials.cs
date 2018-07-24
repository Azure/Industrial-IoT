// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Auth {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Azure;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Injectable service credentials
    /// </summary>
    public class TokenProviderCredentials : ICredentialProvider {

        /// <summary>
        /// Create credentials
        /// </summary>
        /// <param name="config"></param>
        public TokenProviderCredentials(ITokenProvider provider,
            IClientConfig config) {

            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _config = config;
        }

        /// <inheritdoc/>
        public async Task<AzureCredentials> GetAzureCredentialsAsync(
            AzureEnvironment environment) {
            if (environment == null) {
                environment = AzureEnvironment.AzureGlobalCloud;
            }
            await _lock.WaitAsync();
            try {
                if (!_credentials.TryGetValue(environment.Name, out var creds)) {
                    creds = await CreateCredentialsAsync(environment);
                    _credentials.Add(environment.Name, creds);
                }
                return creds;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<Rest.TokenCredentials> GetTokenCredentialsAsync(
            string resource) {
            var token = await _provider.GetTokenForAsync(resource);
            return new Rest.TokenCredentials(token.RawToken);
        }

        /// <summary>
        /// Create credentials from provider
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        private async Task<AzureCredentials> CreateCredentialsAsync(
            AzureEnvironment environment) {
            var tenant = _config?.TenantId ?? "common";
            var mgmt = await GetTokenCredentialsAsync(environment.ManagementEndpoint);
            try {
                var graph = mgmt;
                if (environment.ManagementEndpoint == environment.GraphEndpoint) {
                    graph = await GetTokenCredentialsAsync(environment.GraphEndpoint);
                }
                return new AzureCredentials(mgmt, graph, tenant, environment);
            }
            catch {
                return new AzureCredentials(mgmt, null, tenant, environment);
            }
        }

        private readonly IClientConfig _config;
        private readonly ITokenProvider _provider;
        private readonly Dictionary<string, AzureCredentials> _credentials =
            new Dictionary<string, AzureCredentials>();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
    }
}
