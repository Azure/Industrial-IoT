// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth {
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using System.Linq;


    /// <summary>
    /// Configure JWT bearer authentication
    /// </summary>
    public static class JwtBearerAuthEx {

        /// <summary>
        /// Use jwt bearer auth
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseJwtBearerAuthentication(this IApplicationBuilder app) {
            return app.UseAuthentication();
        }

        /// <summary>
        /// Helper to add jwt bearer authentication
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="provider"></param>
        public static AuthenticationBuilder AddJwtBearerProvider(this AuthenticationBuilder builder,
            string provider) {

            builder.Services.TryAddTransient<IServerAuthConfig, ServiceAuthAggregateConfig>();
            // Allow access to context from within token providers and other client auth
            builder.Services.AddHttpContextAccessor();

            // Add provider configuration
            builder.Services.AddTransient<IConfigureOptions<JwtBearerOptions>>(services => {
                var auth = services.GetRequiredService<IServerAuthConfig>();
                var environment = services.GetRequiredService<IWebHostEnvironment>();
                return new ConfigureNamedOptions<JwtBearerOptions>(provider, options => {

                    // Find whether the scheme is configurable
                    var config = auth.JwtBearerProviders?
                        .FirstOrDefault(s => s.GetProviderName() == provider);
                    if (config == null) {
                        // Not configurable - this is ok as this might not be enabled
                        // Will not be enabled for authorization
                        return;
                    }
                    options.Authority = config.GetAuthorityUrl();
                    options.SaveToken = true; // Save token to allow request on behalf
                    options.RequireHttpsMetadata =
                       !new Uri(options.Authority).DnsSafeHost.EqualsIgnoreCase("localhost");

                    options.TokenValidationParameters = new TokenValidationParameters {
                        ClockSkew = config.AllowedClockSkew,
                        ValidateIssuer = true,
                        IssuerValidator = (iss, t, p) => ValidateIssuer(iss, config),
                        ValidateAudience = !string.IsNullOrEmpty(config.Audience),
                        ValidAudience = config.Audience
                    };
                    options.MetadataAddress = config.GetAuthorityUrl() + "/.well-known/openid-configuration";
                    options.Events = new JwtBearerEvents {
                        OnTokenValidated = ctx => {
                            if (ctx.SecurityToken is JwtSecurityToken accessToken) {
                                if (ctx.Principal.Identity is ClaimsIdentity identity) {
                                    identity.AddClaim(new Claim("access_token",
                                        accessToken.RawData));
                                }
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
            });
            builder.Services.AddSingleton(new Provider(provider, JwtBearerDefaults.AuthenticationScheme));
            return builder.AddJwtBearer(provider, configureOptions: null);
        }

        /// <summary>
        /// Validate the issuer. The issuer is considered as valid if it is the same
        /// uri or it has the same http scheme and authority as the trusted issuer uri
        /// from the configuration file or default uri, plus if it is not fully the
        /// same it has to have a tenant Id, and optionally v2.0 but nothing more...
        /// </summary>
        /// <param name="issuer">Issuer to validate (will be tenanted)</param>
        /// <param name="config">Authentication configuration</param>
        /// <returns>The <c>issuer</c> if it's valid</returns>
        private static string ValidateIssuer(string issuer, IOAuthServerConfig config) {
            var uri = new Uri(issuer);
            var trustedIssuer = new Uri(string.IsNullOrEmpty(config?.TrustedIssuer) ?
                kDefaultIssuerUri : config.TrustedIssuer);
            if (uri == trustedIssuer) {
                return issuer; // Configured issuer correct.
            }
            if (uri.Scheme != trustedIssuer.Scheme ||
                uri.Authority != trustedIssuer.Authority) {
                throw new SecurityTokenInvalidIssuerException(
                    "Issuer has wrong authority.");
            }
            var parts = uri.AbsolutePath.Split(new char[] { '/' },
                StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) {
                throw new SecurityTokenInvalidIssuerException(
                    "Issuer is not tenanted.");
            }
            if (parts.Length >= 1 && !Guid.TryParse(parts[0], out _)) {
                throw new SecurityTokenInvalidIssuerException(
                    "No valid tenant Id for the issuer.");
            }
            if (parts.Length > 1 && parts[2] != "v2.0") {
                throw new SecurityTokenInvalidIssuerException(
                    "Only accepted protocol versions are AAD v1.0 or V2.0");
            }
            return issuer;
        }

        private const string kDefaultIssuerUri = "https://sts.windows.net/";
    }
}
