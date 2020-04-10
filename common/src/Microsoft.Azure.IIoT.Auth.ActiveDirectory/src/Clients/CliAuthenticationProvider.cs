// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Utils;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Autofac;

    /// <summary>
    /// Authenticate with device token after trying app authentication.
    /// </summary>
    public class CliAuthenticationProvider : ITokenProvider {

        /// <inheritdoc/>
        public CliAuthenticationProvider(IComponentContext components) {
            _vs = components.Resolve<VsAuthenticationProvider>();
            _dc = components.Resolve<DeviceCodeTokenProvider>();
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes = null) {

            var token = await Try.Async(() => _vs.GetTokenForAsync(resource, scopes));
            if (token != null) {
                return token;
            }
            return await Try.Async(() => _dc.GetTokenForAsync(resource, scopes));
        }

        /// <inheritdoc/>
        public async Task InvalidateAsync(string resource) {
            await _vs.InvalidateAsync(resource);
            await _dc.InvalidateAsync(resource);
        }

        private readonly VsAuthenticationProvider _vs;
        private readonly DeviceCodeTokenProvider _dc;
    }
}
