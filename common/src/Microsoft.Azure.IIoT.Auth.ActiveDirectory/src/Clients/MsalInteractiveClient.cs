// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Identity.Client;
    using Serilog;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Authenticate using browser
    /// </summary>
    public sealed class MsalInteractiveClient : MsalPublicClientBase {

        /// <summary>
        /// Create interactive token provider with callback
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public MsalInteractiveClient(IClientAuthConfig config, ILogger logger) :
            base(config, logger) {
        }

        /// <inheritdoc/>
        protected override async Task<TokenResultModel> GetTokenAsync(
            IPublicClientApplication client, string resource, IEnumerable<string> scopes) {
            if (!client.IsSystemWebViewAvailable) {
                return null;
            }
            var result = await client.AcquireTokenInteractive(scopes).ExecuteAsync();
            return result.ToTokenResult();
        }

        /// <inheritdoc/>
        protected override PublicClientApplicationBuilder ConfigurePublicClientApplication(
            string clientId, PublicClientApplicationBuilder builder) {
            return builder.WithDefaultRedirectUri();
        }
    }
}
