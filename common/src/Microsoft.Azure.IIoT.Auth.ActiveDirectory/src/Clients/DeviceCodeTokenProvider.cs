// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Authenticate using device code
    /// </summary>
    public sealed class DeviceCodeTokenProvider : ITokenProvider {

        /// <summary>
        /// Create console output device code based token provider
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public DeviceCodeTokenProvider(IClientAuthConfig config, ILogger logger) :
            this(new ConsolePrompt(), config, null, logger) {
        }

        /// <summary>
        /// Create console output device code based token provider
        /// </summary>
        /// <param name="store"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public DeviceCodeTokenProvider(
            IClientAuthConfig config, ITokenCacheProvider store, ILogger logger) :
            this(new ConsolePrompt(), config, store, logger) {
        }

        /// <summary>
        /// Create device code provider with callback
        /// </summary>
        /// <param name="store"></param>
        /// <param name="prompt"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public DeviceCodeTokenProvider(IDeviceCodePrompt prompt,
            IClientAuthConfig config, ITokenCacheProvider store, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
            _store = store ?? DefaultTokenCacheProvider.Instance;
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Obtain token from user
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {
            foreach (var config in _config.Query(resource, AuthScheme.Aad)) {
                var ctx = CreateAuthenticationContext(config.InstanceUrl,
                    config.TenantId, _store);
                try {
                    try {
                        var result = await ctx.AcquireTokenSilentAsync(
                            config.Audience, config.AppId);
                        return result.ToTokenResult();
                    }
                    catch (AdalSilentTokenAcquisitionException) {
                        // Use device code
                        var codeResult = await ctx.AcquireDeviceCodeAsync(
                            config.Audience, config.AppId);

                        _prompt.Prompt(codeResult.DeviceCode, codeResult.ExpiresOn,
                            codeResult.Message);

                        // Wait and acquire it when authenticated
                        var result = await ctx.AcquireTokenByDeviceCodeAsync
                            (codeResult);
                        return result.ToTokenResult();
                    }
                }
                catch (Exception exc) {
                    _logger.Information(exc, "Failed to get token for {resource}", resource);
                    continue;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public Task InvalidateAsync(string resource) {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Helper to create authentication context
        /// </summary>
        /// <param name="authorityUrl"></param>
        /// <param name="tenantId"></param>
        /// <param name="store"></param>
        /// <returns></returns>
        private static AuthenticationContext CreateAuthenticationContext(
            string authorityUrl, string tenantId, ITokenCacheProvider store) {
            if (string.IsNullOrEmpty(authorityUrl)) {
                authorityUrl = kDefaultAuthorityUrl;
            }
            var uri = new UriBuilder(authorityUrl) {
                Path = tenantId ?? "common"
            };
            var ctx = new AuthenticationContext(uri.ToString(),
                store.GetCache(authorityUrl));
            if (tenantId == null && ctx.TokenCache.Count > 0) {
                uri.Path = ctx.TokenCache.ReadItems().First().TenantId;
                ctx = new AuthenticationContext(uri.ToString());
            }
            return ctx;
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
        private readonly ITokenCacheProvider _store;
        private const string kDefaultAuthorityUrl = "https://login.microsoftonline.com/";
    }
}
