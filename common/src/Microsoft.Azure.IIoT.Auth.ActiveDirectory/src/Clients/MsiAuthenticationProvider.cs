// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.Services.AppAuthentication;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Use msi to get token
    /// </summary>
    public class MsiAuthenticationProvider : AppAuthenticationBase {

        /// <inheritdoc/>
        public MsiAuthenticationProvider(IClientAuthConfig config, ILogger logger) :
            base(logger) {
            _config = config?.ClientSchemes?
                .Where(c => c.Scheme == AuthScheme.Msi)
                .Where(c => !string.IsNullOrEmpty(c.AppId))
                .Select(CreateProvider)
                .ToList();
        }

        /// <inheritdoc/>
        protected override IEnumerable<(string, AzureServiceTokenProvider)> Get(string resource) {
            return _config.Where(c => c.Key == resource).Select(c => c.Value);
        }

        /// <summary>
        /// Helper to create provider
        /// </summary>
        /// <returns></returns>
        private static KeyValuePair<string, (string, AzureServiceTokenProvider)> CreateProvider(
            IOAuthClientConfig config) {
            // See if configured in environment variable
            var cs = Environment.GetEnvironmentVariable("AzureServicesAuthConnectionString");
            if (string.IsNullOrEmpty(cs)) {
                // Run as app
                cs = $"RunAs=App;AppId={config.AppId}";
                if (!string.IsNullOrEmpty(config.TenantId)) {
                    cs += $";TenantId={config.TenantId}";
                }
            }
            var url = config.GetAuthorityUrl();
            return KeyValuePair.Create(config.Audience ?? Http.Resource.Platform,
                (url, new AzureServiceTokenProvider(cs, url)));
        }

        private readonly List<KeyValuePair<string, (string, AzureServiceTokenProvider)>> _config;
    }
}
