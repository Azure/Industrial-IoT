// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Authenticate using device code
    /// </summary>
    public sealed class DeviceCodeProvider : ITokenProvider {

        /// <summary>
        /// Create console output device code based token provider
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public DeviceCodeProvider(IClientAuthConfig config, ILogger logger) :
            this(new ConsolePrompt(), config, logger) {
        }

        /// <summary>
        /// Create device code provider with callback
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public DeviceCodeProvider(IDeviceCodePrompt prompt,
            IClientAuthConfig config, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc/>
        public bool Supports(string resource) {
            return _config.Query(resource, AuthScheme.AuthService).Any();
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {
            foreach (var config in _config.Query(resource, AuthScheme.AuthService)) {
                await Task.Delay(1);
                // var ctx = CreateAuthenticationContext(config.InstanceUrl,
                //     config.TenantId, _store);
                // try {
                //     try {
                //         var result = await ctx.AcquireTokenSilentAsync(
                //             config.Audience, config.AppId);
                //         return result.ToTokenResult();
                //     }
                //     catch (AdalSilentTokenAcquisitionException) {
                //         // Use device code
                //         var codeResult = await ctx.AcquireDeviceCodeAsync(
                //             config.Audience, config.AppId);
                //
                //         _prompt.Prompt(codeResult.DeviceCode, codeResult.ExpiresOn,
                //             codeResult.Message);
                //
                //         // Wait and acquire it when authenticated
                //         var result = await ctx.AcquireTokenByDeviceCodeAsync
                //             (codeResult);
                //         return result.ToTokenResult();
                //     }
                //    _logger.Information(
                //  "Successfully acquired token for {resource} with {config}.",
                //  resource, config.GetName());
                //    // }
                // catch (Exception exc) {
                //     _logger.Information(exc, "Failed to get token for {resource}using {config}",
              //  resource, config.GetName());
                //     continue;
                // }
            }
            return null;
        }

        /// <inheritdoc/>
        public Task InvalidateAsync(string resource) {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Console prompt
        /// </summary>
        private sealed class ConsolePrompt : IDeviceCodePrompt {
            /// <inheritdoc/>
            public void Prompt(string deviceCode, DateTimeOffset expiresOn,
                string message) {
                Console.WriteLine(message);
            }
        }

        private readonly ILogger _logger;
        private readonly IClientAuthConfig _config;
        private readonly IDeviceCodePrompt _prompt;
    }
}
