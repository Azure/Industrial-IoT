// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {
    using Microsoft.Azure.Services.AppAuthentication;
    using Serilog;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Uses developer tool or Az cache as authentication
    /// </summary>
    public class DevAuthenticationClient : AppAuthenticationBase {

        /// <inheritdoc/>
        public DevAuthenticationClient(IClientAuthConfig config, ILogger logger) :
            base(logger) {
            _config = config?.Providers?
                .Where(c => c.Provider == AuthProvider.Msi || c.Provider == AuthProvider.AzureAD)
                .Where(c => c.Audience != null && Regex.IsMatch(c.Audience, @"^[0-9a-zA-Z-.:/]+$"))
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
            var authority = config.GetAuthorityUrl(true);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                yield return KeyValuePair.Create(config.Resource ?? Http.Resource.Platform,
                    (config, new AzureServiceTokenProvider(
                        "RunAs=Developer; DeveloperTool=VisualStudio", authority)));
            }
            yield return KeyValuePair.Create(config.Resource ?? Http.Resource.Platform,
                (config, new AzureServiceTokenProvider(
                    "RunAs=Developer; DeveloperTool=AzureCli", authority)));
        }

        private readonly List<KeyValuePair<string, (IOAuthClientConfig, AzureServiceTokenProvider)>> _config;
    }
}
