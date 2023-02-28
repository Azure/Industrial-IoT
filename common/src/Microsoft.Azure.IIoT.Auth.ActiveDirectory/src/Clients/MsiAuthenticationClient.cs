// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients
{
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Use msi to get token
    /// </summary>
    public class MsiAuthenticationClient : AppAuthenticationBase
    {
        /// <inheritdoc/>
        public MsiAuthenticationClient(IClientAuthConfig config, ILogger logger) : base(logger)
        {
            _config = config?.Providers?
                .Where(c => c.Provider == AuthProvider.Msi)
                .Where(c => !string.IsNullOrEmpty(c.ClientId))
                .Select(c => CreateProvider(c, logger))
                .ToList();
            if (_config.Count == 0)
            {
                logger.LogInformation("No managed service identity configured for this service.");
            }
        }

        /// <inheritdoc/>
        protected override IEnumerable<(IOAuthClientConfig, AzureServiceTokenProvider)> GetProvider(string resource)
        {
            return _config.Where(c => c.Key == resource).Select(c => c.Value);
        }

        /// <summary>
        /// Helper to create provider
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private static KeyValuePair<string, (IOAuthClientConfig, AzureServiceTokenProvider)> CreateProvider(
            IOAuthClientConfig config, ILogger logger)
        {
            var cs = $"RunAs=App;AppId={config.ClientId}";
            if (!string.IsNullOrEmpty(config.TenantId))
            {
                cs += $";TenantId={config.TenantId}";
            }
            var provider = new AzureServiceTokenProvider(cs, config.GetAuthorityUrl(true));
            logger.LogInformation("Managed service identity {ClientId} in {Tenant} registered.",
                config.ClientId, config.TenantId);
            return KeyValuePair.Create(config.Resource ?? Http.Resource.Platform, (config, provider));
        }

        private readonly List<KeyValuePair<string, (IOAuthClientConfig, AzureServiceTokenProvider)>> _config;
    }
}
