// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Cli
{
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk.Msal;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk.Runtime;
    using Furly.Extensions.Configuration;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Service client configuration
    /// </summary>
    public sealed class Configuration : ConfigureOptionBase<ServiceSdkOptions>
    {
        /// <inheritdoc/>
        public override void Configure(string name, ServiceSdkOptions options)
        {
            // Get service url
            if (options.ServiceUrl == null)
            {
                var serviceUrl = GetStringOrDefault("url"); // --url
                if (serviceUrl != null)
                {
                    options.ServiceUrl = serviceUrl;
                }
            }

            var useMsgPack = GetBoolOrNull("useMsgPack");   // --useMsgPack
            if (useMsgPack != null)
            {
                options.UseMessagePackProtocol = useMsgPack.Value;
            }

            options.TokenProvider ??= BuildTokenProvider();
        }

        /// <inheritdoc/>
        public Configuration(IConfiguration configuration) : base(configuration)
        {
        }

        /// <summary>
        /// Build a msal token provider if configured as the default token
        /// provider for the sdk.
        /// </summary>
        /// <returns></returns>
        private Func<Task<string>> BuildTokenProvider()
        {
            // TODO: _configuration.Bind<MicrosoftIdentityOptions>() as defaults;
            var clientId = GetStringOrDefault(EnvVars.PCS_AAD_PUBLIC_CLIENT_APPID);
            if (clientId == null)
            {
                return null;
            }

            var instance = GetStringOrDefault(EnvVars.PCS_AAD_INSTANCE,
                "https://login.microsoftonline.com");
            var tenantId = GetStringOrDefault(EnvVars.PCS_AUTH_TENANT,
                "common");
            //
            // Using the client itself as resource in scope produces a
            // consent page for all registered permissions. Then the token
            // provider returns an id_token, rather than an access token.
            //
            var serviceAppId = GetStringOrDefault(EnvVars.PCS_AAD_SERVICE_APPID,
                clientId);
            var scope = $"{serviceAppId}/.default";

            var tokenProvider = new TokenProvider(clientId, instance, tenantId);
            return () => tokenProvider.GetTokenAsync([scope], false);
        }
    }
}
