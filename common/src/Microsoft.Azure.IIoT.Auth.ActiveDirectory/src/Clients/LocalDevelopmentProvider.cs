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
    /// Uses developer tool or Az cache as authentication
    /// </summary>
    public class LocalDevelopmentProvider : AppAuthenticationBase {

        /// <inheritdoc/>
        public LocalDevelopmentProvider(IClientAuthConfig config, ILogger logger) :
            base(logger) {
            _config = config?.ClientSchemes?
                .Where(c => c.Scheme == AuthScheme.Msi || c.Scheme == AuthScheme.Aad)
                .SelectMany(CreateProvider)
                .ToList();
        }

        /// <inheritdoc/>
        protected override IEnumerable<(IOAuthClientConfig, AzureServiceTokenProvider)> Get(string resource) {
            return _config.Where(c => c.Key == resource).Select(c => c.Value);
        }

        /// <summary>
        /// Create providers
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<KeyValuePair<string, (IOAuthClientConfig, AzureServiceTokenProvider)>>
            CreateProvider(IOAuthClientConfig config) {
            yield return KeyValuePair.Create(config.Resource ?? Http.Resource.Platform,
                (config, new AzureServiceTokenProvider(
                    "RunAs=Developer; DeveloperTool=VisualStudio", config.GetAuthorityUrl())));
            yield return KeyValuePair.Create(config.Resource ?? Http.Resource.Platform,
                (config, new AzureServiceTokenProvider(
                    "RunAs=Developer; DeveloperTool=AzureCli", config.GetAuthorityUrl())));
        }

        private readonly List<KeyValuePair<string, (IOAuthClientConfig, AzureServiceTokenProvider)>> _config;
    }
}
