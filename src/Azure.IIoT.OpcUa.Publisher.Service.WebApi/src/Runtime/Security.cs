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
    using System;
    using System.Linq;

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
            services.AddTransient<IConfigureOptions<JwtBearerOptions>>(context =>
            {
                // Support 2.8 where the audience does not contain api://
                var clientId = context.GetService<IOptions<MicrosoftIdentityOptions>>()?.Value.ClientId;
                return new ConfigureNamedOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
                    options => options.TokenValidationParameters.ValidAudiences =
                        clientId == null ? Enumerable.Empty<string>() : clientId.YieldReturn());
            });
            return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(_ => { }, _ => { },
                    subscribeToJwtBearerMiddlewareDiagnosticsEvents: false)
                ;
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
            public override void Configure(string? name, MicrosoftIdentityOptions options)
            {
                if (options.ClientId == null)
                {
                    options.ClientId = GetStringOrDefault(kAuth_ClientIdKey,
                        GetStringOrDefault(EnvVars.PCS_AAD_SERVICE_APPID, string.Empty));
                }
                if (options.ClientSecret == null)
                {
                    options.ClientSecret = GetStringOrDefault(kAuth_ClientSecretKey,
                        GetStringOrDefault(EnvVars.PCS_AAD_SERVICE_SECRET, string.Empty));
                }
                if (options.Domain == null)
                {
                    options.Domain = GetStringOrDefault(kAuth_DomainKey);
                }
                if (options.Domain == null &&
                    Uri.TryCreate(GetStringOrDefault(EnvVars.PCS_AAD_AUDIENCE),
                        UriKind.Absolute, out var uri))
                {
                    options.Domain = $"{uri.Host.Split('.')[0]}.onmicrosoft.com";
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
                        GetStringOrDefault(EnvVars.PCS_AUTH_TENANT, string.Empty));
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
