// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi
{
    using Furly.Extensions.Configuration;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.Identity.Web;

    /// <summary>
    /// Service auth configuration
    /// </summary>
    public static class Security
    {
        /// <summary>
        /// Helper to add jwt bearer authentication
        /// </summary>
        /// <param name="services"></param>
        public static MicrosoftIdentityWebApiAuthenticationBuilder AddMicrosoftIdentityWebApiAuthentication(
            this IServiceCollection services)
        {
            services.AddTransient<IConfigureOptions<MicrosoftIdentityOptions>, MicrosoftIdentity>();
            services.AddTransient<IConfigureNamedOptions<MicrosoftIdentityOptions>, MicrosoftIdentity>();
            return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(_ => { }, _ => { });
        }

        /// <summary>
        /// Jwt bearer configuration
        /// </summary>
        internal sealed class MicrosoftIdentity : ConfigureOptionBase<MicrosoftIdentityOptions>
        {
            /// <summary>
            /// Auth configuration
            /// </summary>
            private const string kAuth_TenantIdKey = "AzureAd:TenantId";
            private const string kAuth_InstanceUrlKey = "AzureAd:InstanceUrl";
            private const string kAuth_ClientIdKey = "AzureAd:ClientId";
            private const string kAuth_ClientSecretKey = "AzureAd:ClientSecret";
            private const string kAuth_DomainKey = "AzureAd:Domain";

            /// <inheritdoc/>
            public override void Configure(string name, MicrosoftIdentityOptions options)
            {
                if (options.ClientId == null)
                {
                    options.ClientId = GetStringOrDefault(kAuth_ClientIdKey,
                        GetStringOrDefault(EnvVars.PCS_AAD_SERVICE_APPID))?.Trim();
                }
                if (options.ClientSecret == null)
                {
                    options.ClientSecret = GetStringOrDefault(kAuth_ClientSecretKey,
                        GetStringOrDefault(EnvVars.PCS_AAD_SERVICE_SECRET))?.Trim();
                }
                if (options.Domain == null)
                {
                    options.Instance = GetStringOrDefault(kAuth_DomainKey,
                        GetStringOrDefault(EnvVars.PCS_AAD_AUDIENCE))?.Trim();
                }
                if (options.Instance == null)
                {
                    options.Instance = GetStringOrDefault(kAuth_InstanceUrlKey,
                        GetStringOrDefault(EnvVars.PCS_AAD_INSTANCE,
                            "https://login.microsoftonline.com")).Trim();
                }
                if (options.TenantId == null)
                {
                    options.TenantId = GetStringOrDefault(kAuth_TenantIdKey,
                        GetStringOrDefault(EnvVars.PCS_AUTH_TENANT))?.Trim();
                }
            }

            /// <summary>
            /// Configuration constructor
            /// </summary>
            /// <param name="configuration"></param>
            public MicrosoftIdentity(IConfiguration configuration) :
                base(configuration)
            {
            }
        }
    }
}
