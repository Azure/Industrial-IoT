// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Auth {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Clients;
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
        /// <param name="provider"></param>
        /// <param name="config"></param>
        public TokenProviderCredentials(ITokenProvider provider,
            IClientConfig config) : this(config) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Create credentials
        /// </summary>
        /// <param name="config"></param>
        protected TokenProviderCredentials(IClientConfig config) {
            _config = config;
            _expiry = TimeSpan.FromMinutes(3); // Create new credential after 3 minutes
        }

        /// <inheritdoc/>
        public async Task<AzureCredentials> GetAzureCredentialsAsync(
            AzureEnvironment environment) {
            if (environment == null) {
                environment = AzureEnvironment.AzureGlobalCloud;
            }
            await _lock.WaitAsync();
            try {
                if (!_credentials.TryGetValue(environment.Name, out var creds) ||
                    creds.Item1 + _expiry < DateTime.Now) {
                    var credentials = await CreateCredentialsAsync(environment);
                    creds = Tuple.Create(DateTime.Now, credentials);
                    _credentials.AddOrUpdate(environment.Name, creds);
                }
                return creds.Item2;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public virtual async Task<Rest.TokenCredentials> GetTokenCredentialsAsync(
            string resource) {
            var token = await _provider.GetTokenForAsync(resource);
            return new Rest.TokenCredentials(token.RawToken);
        }

        /// <summary>
        /// Create credentials from provider
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        protected virtual async Task<AzureCredentials> CreateCredentialsAsync(
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

        /// <summary>Configuration to be used by derived classes</summary>
        protected readonly IClientConfig _config;
        private readonly ITokenProvider _provider;
        private readonly Dictionary<string, Tuple<DateTime, AzureCredentials>> _credentials =
            new Dictionary<string, Tuple<DateTime, AzureCredentials>>();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly TimeSpan _expiry;
    }
}
