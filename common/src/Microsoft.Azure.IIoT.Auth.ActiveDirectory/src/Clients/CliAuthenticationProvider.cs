// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Authenticate with device token after trying app authentication.
    /// </summary>
    public class CliAuthenticationProvider : ITokenProvider {

        /// <inheritdoc/>
        public CliAuthenticationProvider(IClientConfig config, ILogger logger) {
            _vs = new VsAuthenticationProvider(config);
            _dc = new DeviceCodeTokenProvider(config, logger);
        }

        /// <inheritdoc/>
        public CliAuthenticationProvider(IClientConfig config,
            ITokenCacheProvider store, ILogger logger) {
            _vs = new VsAuthenticationProvider(config);
            _dc = new DeviceCodeTokenProvider(config, store, logger);
        }

        /// <inheritdoc/>
        public CliAuthenticationProvider(Action<string, DateTimeOffset, string> callback,
            IClientConfig config, ITokenCacheProvider store, ILogger logger) {
            _vs = new VsAuthenticationProvider(config);
            _dc = new DeviceCodeTokenProvider(callback, config, store, logger);
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes = null) {
            try {
                return await _vs.GetTokenForAsync(resource, scopes);
            }
            catch {
                return await _dc.GetTokenForAsync(resource, scopes);
            }
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
