// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients {
    using Microsoft.Azure.Services.AppAuthentication;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <inheritdoc/>
    public class AppAuthenticationClient : AppAuthenticationBase {

        /// <inheritdoc/>
        public AppAuthenticationClient(IClientAuthConfig config, ILogger logger) :
            base(logger) {
            _config = config?.ClientSchemes?
                .Where(c => c.Scheme == AuthScheme.AzureAD)
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
            // See if configured in environment variable
            var cs = Environment.GetEnvironmentVariable("AzureServicesAuthConnectionString");
            if (string.IsNullOrEmpty(cs)) {
                // Run as app
                cs = $"RunAs=App;AppId={config.ClientId}";
                if (!string.IsNullOrEmpty(config.TenantId)) {
                    cs += $";TenantId={config.TenantId}";
                }
                if (!string.IsNullOrEmpty(config.ClientSecret)) {
                    cs += $";AppKey={config.ClientSecret}";
                }
            }
            return KeyValuePair.Create(config.Resource ?? Http.Resource.Platform,
                (config, new AzureServiceTokenProvider(cs, config.GetAuthorityUrl())));
        }

        private readonly List<KeyValuePair<string, (IOAuthClientConfig, AzureServiceTokenProvider)>> _config;
    }
}
