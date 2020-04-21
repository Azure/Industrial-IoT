// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Auth.Storage;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.Identity.Client;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Authenticate using device code
    /// </summary>
    public sealed class MsalDeviceCodeClient : ITokenClient {

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
            IClientAuthConfig config, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
            _config = config?.Query(AuthProvider.AzureAD)
                .Select(config => (config, CreatePublicClientApplication(config)))
                .ToList();
        }

        /// <inheritdoc/>
        public bool Supports(string resource) {
            return _config.Any(c => c.config.Resource == resource);
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {
            foreach (var client in _config.Where(c => c.config.Resource == resource)) {
                var decorator = client.Item2;
                var accounts = await decorator.Client.GetAccountsAsync();
                if (!accounts.Any()) {
                    continue;
                }
                try {
                    // Attempt to get a token from the cache (or refresh it silently if needed)
                    var result = await decorator.Client.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                        .ExecuteAsync();

                    return result.ToTokenResult();
                }
                catch (MsalUiRequiredException) {
                    try {
                        var result = await decorator.Client.AcquireTokenWithDeviceCode(scopes,
                            deviceCodeCallback => {
                                _prompt.Prompt(deviceCodeCallback.DeviceCode, deviceCodeCallback.ExpiresOn,
                                    deviceCodeCallback.Message);
                                return Task.CompletedTask;
                            }).ExecuteAsync();

                        _logger.Information(
                            "Successfully acquired token for {resource} with {config}.",
                            resource, client.config.GetName());

                        return result.ToTokenResult();
                    }
                    catch (MsalException ex) {
                        _logger.Error(ex, "Failed to get token for {resource} with {config} " +
                            "- error: {error}",
                            resource, client.config.GetName(), ex.ErrorCode);
                    }
                    catch (Exception e) {
                        _logger.Debug(e, "Failed to get token for {resource} with {config}.",
                            resource, client.config.GetName());
                    }
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public Task InvalidateAsync(string resource) {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Create public client
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private MsalClientApplicationDecorator<IPublicClientApplication> CreatePublicClientApplication(
            IOAuthClientConfig config) {
            return new MsalClientApplicationDecorator<IPublicClientApplication>(
                PublicClientApplicationBuilder.Create(config.ClientId).WithTenantId(config.TenantId).Build(),
                    new MemoryCache(), config.ClientId);
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
        private readonly IDeviceCodePrompt _prompt;
        private readonly List<(IOAuthClientConfig config,
            MsalClientApplicationDecorator<IPublicClientApplication>)> _config;
    }
}
