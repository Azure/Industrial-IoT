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
    /// Uses developer tool authentication
    /// </summary>
    public class VsAuthenticationProvider : AppAuthenticationBase {

        /// <inheritdoc/>
        public VsAuthenticationProvider(ILogger logger) : base(logger) {
            _provider = new AzureServiceTokenProvider(
                "RunAs=Developer; DeveloperTool=VisualStudio", kAzureAdInstance);
        }

        /// <inheritdoc/>
        protected override IEnumerable<(string, AzureServiceTokenProvider)> Get(
            string resource) {
            return (kAzureAdInstance, _provider).YieldReturn();
        }

        private const string kAzureAdInstance = "https://login.microsoftonline.com/common";
        private readonly AzureServiceTokenProvider _provider;
    }
}
