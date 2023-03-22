// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Runtime
{
    using Furly.Extensions.Configuration;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.Identity.Web;
    using System;

    /// <summary>
    /// Service auth configuration
    /// </summary>
    public static class Security
    {
        /// <summary>
        /// Helper to add jwt bearer authentication
        /// </summary>
        /// <param name="services"></param>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddMsalAuthentication(
            this IServiceCollection services)
        {
            services.AddTransient<IConfigureOptions<MicrosoftIdentityOptions>, MicrosoftIdentity>();
            services.AddTransient<IConfigureNamedOptions<MicrosoftIdentityOptions>, MicrosoftIdentity>();
            services.AddTransient<IConfigureOptions<OpenIdConnectOptions>, OpenIdConnect>();
            services.AddTransient<IConfigureNamedOptions<OpenIdConnectOptions>, OpenIdConnect>();
            return services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(_ => { }, _ => { }, OpenIdConnectDefaults.AuthenticationScheme,
                    CookieAuthenticationDefaults.AuthenticationScheme, false, null)
                .EnableTokenAcquisitionToCallDownstreamApi(Array.Empty<string>());
        }

        /// <summary>
        /// Open id configuration
        /// </summary>
        internal sealed class OpenIdConnect : ConfigureOptionBase<OpenIdConnectOptions>
        {
            /// <inheritdoc/>
            public override void Configure(string name, OpenIdConnectOptions options)
            {
                var serviceAppId = GetStringOrDefault(EnvVars.PCS_AAD_SERVICE_APPID);
                if (serviceAppId != null)
                {
                    // Add initial scopes
                    var scope = $"{serviceAppId}/.default";
                    if (!options.Scope.Contains(scope))
                    {
                        options.Scope.Add(scope);
                    }
                }
            }

            /// <inheritdoc/>
            public OpenIdConnect(IConfiguration configuration) : base(configuration)
            {
            }
        }

        /// <summary>
        /// Msal configuration
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
                        GetStringOrDefault(EnvVars.PCS_AAD_CONFIDENTIAL_CLIENT_APPID,
                        GetStringOrDefault("PCS_WEBUI_AUTH_AAD_APPID")));
                }
                if (options.ClientSecret == null)
                {
                    options.ClientSecret = GetStringOrDefault(kAuth_ClientSecretKey,
                        GetStringOrDefault(EnvVars.PCS_AAD_CONFIDENTIAL_CLIENT_SECRET,
                        GetStringOrDefault("PCS_APPLICATION_SECRET")));
                }
                if (options.Domain == null)
                {
                    options.Instance = GetStringOrDefault(kAuth_DomainKey,
                        GetStringOrDefault(EnvVars.PCS_AAD_AUDIENCE));
                }
                if (options.Instance == null)
                {
                    options.Instance = GetStringOrDefault(kAuth_InstanceUrlKey,
                        GetStringOrDefault(EnvVars.PCS_AAD_INSTANCE,
                        GetStringOrDefault("PCS_WEBUI_AUTH_AAD_AUTHORITY",
                            "https://login.microsoftonline.com")));
                }
                if (options.TenantId == null)
                {
                    options.TenantId = GetStringOrDefault(kAuth_TenantIdKey,
                        GetStringOrDefault(EnvVars.PCS_AUTH_TENANT,
                        GetStringOrDefault("PCS_WEBUI_AUTH_AAD_TENANT",
                            "common")));
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
