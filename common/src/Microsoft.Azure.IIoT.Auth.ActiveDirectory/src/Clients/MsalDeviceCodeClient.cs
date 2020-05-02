// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Identity.Client;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Authenticate using device code
    /// </summary>
    public sealed class MsalDeviceCodeClient : MsalPublicClientBase {

        /// <summary>
        /// Create console output device code based token provider
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public MsalDeviceCodeClient(IClientAuthConfig config, ILogger logger) :
            this(new ConsolePrompt(), config, logger) {
        }

        /// <summary>
        /// Create device code provider with callback
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public MsalDeviceCodeClient(IDeviceCodePrompt prompt,
            IClientAuthConfig config, ILogger logger) : base(config, logger) {
            _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        }

        /// <inheritdoc/>
        protected override async Task<TokenResultModel> GetTokenAsync(
            IPublicClientApplication client, string resource, IEnumerable<string> scopes) {
            // Go and get it through device code.
            var result = await client.AcquireTokenWithDeviceCode(
                scopes, deviceCodeCallback => {
                    _prompt.Prompt(deviceCodeCallback.DeviceCode, deviceCodeCallback.ExpiresOn,
                        deviceCodeCallback.Message);
                    return Task.CompletedTask;
                }).ExecuteAsync();
            return result.ToTokenResult();
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

        private readonly IDeviceCodePrompt _prompt;
    }
}
