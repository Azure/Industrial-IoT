// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.Msal
{
    using Microsoft.Identity.Client;
    using Microsoft.Identity.Client.Extensions.Msal;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Msal token provider
    /// </summary>
    internal sealed class TokenProvider
    {
        /// <summary>
        /// Create token provider
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="instance"></param>
        /// <param name="tenantId"></param>
        public TokenProvider(string clientId, string instance, string tenantId)
        {
            // Building a public client application
            _app = PublicClientApplicationBuilder.Create(clientId)
                .WithAuthority(new Uri($"{instance.TrimEnd('/')}/{tenantId}"))
                .WithRedirectUri("http://localhost")
                .Build();
            _lazyCacheInitialization = new Lazy<Task>(InitializeCacheAsync);
        }

        /// <summary>
        /// Get token
        /// </summary>
        /// <param name="scopes"></param>
        /// <param name="useEmbaddedView"></param>
        public async Task<string> GetTokenAsync(string[] scopes, bool useEmbaddedView)
        {
            if (!_lazyCacheInitialization.Value.IsCompletedSuccessfully)
            {
                await _lazyCacheInitialization.Value.ConfigureAwait(false);
            }

            AuthenticationResult result;
            try
            {
                var accounts = await _app.GetAccountsAsync().ConfigureAwait(false);

                // Try to acquire an access token from the cache.
                // If an interaction is required, MsalUiRequiredException
                // will be thrown.
                result = await _app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync().ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                // Acquiring an access token interactively.
                // MSAL will cache it so we can use AcquireTokenSilent
                // on future calls.
                result = await _app.AcquireTokenInteractive(scopes)
                            .WithUseEmbeddedWebView(useEmbaddedView)
                            .ExecuteAsync().ConfigureAwait(false);
            }
            return $"Bearer {result.AccessToken}";
        }

        /// <summary>
        /// Ensure cache is initialized
        /// </summary>
        /// <returns></returns>
        private async Task InitializeCacheAsync()
        {
            var rootPath = Path.Combine(MsalCacheHelper.UserRootDirectory,
                "msal.azure.iiot.tools.cache");
            var storageProperties =
                new StorageCreationPropertiesBuilder(
                    Path.GetFileName(rootPath), Path.GetDirectoryName(rootPath))
                .WithLinuxKeyring(
                    "com.azure.iiot.tools.tokencache",
                    MsalCacheHelper.LinuxKeyRingDefaultCollection,
                    "MSAL token cache for Azure Industrial IoT tools.",
                    new KeyValuePair<string, string>("Version", "1"),
                    new KeyValuePair<string, string>("ProductGroup", "Azure"))
                .WithMacKeyChain(
                    "Azure.IIoT.Service.Client.ServiceSdk",
                    "MSALCache")
                .Build();

            var cacheHelper = await MsalCacheHelper.CreateAsync(
                storageProperties).ConfigureAwait(false);
            cacheHelper.RegisterCache(_app.UserTokenCache);
        }

        private readonly IPublicClientApplication _app;
        private readonly Lazy<Task> _lazyCacheInitialization;
    }
}
