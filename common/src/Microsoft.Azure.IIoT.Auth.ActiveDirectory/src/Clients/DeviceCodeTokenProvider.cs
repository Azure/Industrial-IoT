// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Serilog;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Authentication;
    using System.Threading.Tasks;

    /// <summary>
    /// Authenticate using device code
    /// </summary>
    public class DeviceCodeTokenProvider : ITokenProvider {

        /// <summary>
        /// Create console output device code based token provider
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public DeviceCodeTokenProvider(IClientConfig config, ILogger logger) :
            this((c, exp, msg) => Console.WriteLine(msg), config, null, logger) {
        }

        /// <summary>
        /// Create console output device code based token provider
        /// </summary>
        /// <param name="store"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public DeviceCodeTokenProvider(IClientConfig config, ITokenCacheProvider store,
            ILogger logger) :
            this((c, exp, msg) => Console.WriteLine(msg), config, store, logger) {
        }

        /// <summary>
        /// Create device code provider with callback
        /// </summary>
        /// <param name="store"></param>
        /// <param name="callback"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public DeviceCodeTokenProvider(Action<string, DateTimeOffset, string> callback,
            IClientConfig config, ITokenCacheProvider store, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _store = store ?? DefaultTokenCacheProvider.Instance;

            if (string.IsNullOrEmpty(_config.AppId)) {
                _logger.Error("Device code token provider was not configured with " +
                    "a client id.  No tokens will be obtained.");
            }
        }

        /// <summary>
        /// Obtain token from user
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {
            if (string.IsNullOrEmpty(_config.AppId)) {
                // No auth
                return null;
            }
            var ctx = CreateAuthenticationContext(_config.InstanceUrl,
                _config.TenantId, _store);
            try {
                try {
                    var result = await ctx.AcquireTokenSilentAsync(
                        resource, _config.AppId);
                    return result.ToTokenResult();
                }
                catch (AdalSilentTokenAcquisitionException) {

                    // Use device code
                    var codeResult = await ctx.AcquireDeviceCodeAsync(
                        resource, _config.AppId);

                    _callback(codeResult.DeviceCode, codeResult.ExpiresOn,
                        codeResult.Message);

                    // Wait and acquire it when authenticated
                    var result = await ctx.AcquireTokenByDeviceCodeAsync
                        (codeResult);
                    return result.ToTokenResult();
                }
            }
            catch (AdalException exc) {
                throw new AuthenticationException("Failed to authenticate", exc);
            }
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

        /// <inheritdoc/>
        public Task InvalidateAsync(string resource) {
            return Task.CompletedTask;
        }

        /// <summary>Logger for derived class</summary>
        protected readonly ILogger _logger;
        /// <summary>Configuration for derived class</summary>
        protected readonly IClientConfig _config;
        /// <summary>Callback for derived class</summary>
        protected readonly Action<string, DateTimeOffset, string> _callback;

        private const string kDefaultAuthorityUrl = "https://login.microsoftonline.com/";
        private readonly ITokenCacheProvider _store;
    }
}
