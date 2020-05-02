// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {
    using Microsoft.Azure.Services.AppAuthentication;
    using Serilog;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Use msi to get token
    /// </summary>
    public class MsiAuthenticationClient : AppAuthenticationBase {

        /// <inheritdoc/>
        public MsiAuthenticationClient(IClientAuthConfig config, ILogger logger) :
            base(logger) {
            _config = config?.Providers?
                .Where(c => c.Provider == AuthProvider.Msi)
                .Where(c => !string.IsNullOrEmpty(c.ClientId))
                .Select(CreateProvider)
                .ToList();
        }

        /// <inheritdoc/>
        protected override IEnumerable<(IOAuthClientConfig, AzureServiceTokenProvider)> Get(string resource) {
            return _config.Where(c => c.Key == resource).Select(c => c.Value);
        }

        /// <summary>
        /// Helper to create provider
        /// </summary>
        /// <returns></returns>
        private static KeyValuePair<string, (IOAuthClientConfig, AzureServiceTokenProvider)> CreateProvider(
            IOAuthClientConfig config) {
            var cs = $"RunAs=App;AppId={config.ClientId}";
            if (!string.IsNullOrEmpty(config.TenantId)) {
                cs += $";TenantId={config.TenantId}";
            }
            return KeyValuePair.Create(config.Resource ?? Http.Resource.Platform,
                (config, new AzureServiceTokenProvider(cs, config.GetAuthorityUrl(true))));
        }

        private readonly List<KeyValuePair<string, (IOAuthClientConfig, AzureServiceTokenProvider)>> _config;
    }
}
