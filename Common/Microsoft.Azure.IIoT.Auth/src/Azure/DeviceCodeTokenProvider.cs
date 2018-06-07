// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Azure {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
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
        /// Create auth provider
        /// </summary>
        /// <param name="logger"></param>
        public DeviceCodeTokenProvider(ILogger logger,  IClientConfig config) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrEmpty(_config.ClientId)) {
                _logger.Error("Device code token provider was not configured with " +
                    "a client id.  No tokens will be obtained. ", () => { });
            }
        }

        /// <summary>
        /// Obtain token from user
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {
            if (string.IsNullOrEmpty(_config.ClientId)) {
                return null;
            }
            var ctx = CreateAuthenticationContext(_config.Authority,
                _config.TenantId);
            try {
                try {
                    var result = await ctx.AcquireTokenSilentAsync(
                        resource, _config.ClientId);
                    return result.ToTokenResult();
                }
                catch (AdalSilentTokenAcquisitionException) {

                    // Use device code
                    var codeResult = await ctx.AcquireDeviceCodeAsync(
                        resource, _config.ClientId);

                    Console.WriteLine();
                    Console.WriteLine("You need to sign in.");
                    Console.WriteLine(codeResult.Message);

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
        /// <param name="tenantId"></param>
        /// <returns></returns>
        private static AuthenticationContext CreateAuthenticationContext(
            string authority, string tenantId) {
            var tenant = tenantId ?? "common";
            if (string.IsNullOrEmpty(authority)) {
                authority = kAuthority;
            }
            var ctx = new AuthenticationContext(authority + tenant);
            if (tenantId == null && ctx.TokenCache.Count > 0) {
                tenant = ctx.TokenCache.ReadItems().First().TenantId;
                ctx = new AuthenticationContext(authority + tenant);
            }
            return ctx;
        }

        public Task InvalidateAsync(string resource) {
            return Task.CompletedTask;
        }

        private const string kAuthority = "https://login.microsoftonline.com/";
        private readonly ILogger _logger;
        private readonly IClientConfig _config;
    }
}
