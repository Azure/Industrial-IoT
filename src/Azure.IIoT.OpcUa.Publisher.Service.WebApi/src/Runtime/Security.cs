// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi
{
    using Furly.Extensions.Configuration;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Identity.Web;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Service auth configuration
    /// </summary>
    public static class Security
    {
        /// <summary>
        /// Add authentication
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddAuthentication(this IServiceCollection services,
            IConfiguration configuration)
        {
            //
            // Detect Microsoft.Identity.Web detects EasyAuth and register authentication if so.
            //
            if (AppServicesAuthenticationInformation.IsAppServicesAadAuthenticationEnabled)
            {
                services.AddAuthentication(AppServicesAuthenticationDefaults.AuthenticationScheme)
                   .AddMicrosoftIdentityWebApi(configuration,
                    subscribeToJwtBearerMiddlewareDiagnosticsEvents: false)
                   ;
                return services;
            }

            //
            // Otherwise we check if a client id was configured and allow anonymous access if not.
            //
            var options = new MicrosoftIdentityOptions();
            new MicrosoftIdentity(configuration).Configure(options);
            if (string.IsNullOrEmpty(options.ClientId))
            {
                return services.AddSingleton<IAuthorizationHandler, AllowAnonymous>();
            }

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
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(_ => { }, _ => { },
                    subscribeToJwtBearerMiddlewareDiagnosticsEvents: false)
                ;
            return services;
        }

        /// <summary>
        /// This authoriuation handler will bypass all requirements
        /// </summary>
        internal sealed class AllowAnonymous : IAuthorizationHandler
        {
            /// <inheritdoc/>
            public AllowAnonymous(ILogger<AllowAnonymous> logger)
            {
                _logger = logger;
            }

            /// <inheritdoc/>
            public Task HandleAsync(AuthorizationHandlerContext context)
            {
                // Simply pass all requirements
                var authorized = false;
                foreach (var requirement in context.PendingRequirements.ToList())
                {
                    authorized = true;
                    context.Succeed(requirement);
                }
                if (authorized)
                {
                    _logger.LogWarning(@"

    An anonyomous user was authorized because of missing configuration.
                    !!! Do not use in production !!!
    Configure app service authentication (see http://aka.ms/easyauth)
");
                }
                return Task.CompletedTask;
            }

            private readonly ILogger<AllowAnonymous> _logger;
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
                options.ClientId ??= GetStringOrDefault(kAuth_ClientIdKey,
                        GetStringOrDefault(EnvVars.PCS_AAD_SERVICE_APPID, string.Empty));
                options.ClientSecret ??= GetStringOrDefault(kAuth_ClientSecretKey,
                        GetStringOrDefault(EnvVars.PCS_AAD_SERVICE_SECRET, string.Empty));
                options.Domain ??= GetStringOrDefault(kAuth_DomainKey);
                if (options.Domain == null &&
                    Uri.TryCreate(GetStringOrDefault(EnvVars.PCS_AAD_AUDIENCE),
                        UriKind.Absolute, out var uri))
                {
                    options.Domain = $"{uri.Host.Split('.')[0]}.onmicrosoft.com";
                }

                options.Instance ??= GetStringOrDefault(kAuth_InstanceUrlKey,
                        GetStringOrDefault(EnvVars.PCS_AAD_INSTANCE,
                            "https://login.microsoftonline.com")).Trim();
                options.TenantId ??= GetStringOrDefault(kAuth_TenantIdKey,
                        GetStringOrDefault(EnvVars.PCS_AUTH_TENANT, string.Empty));
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
