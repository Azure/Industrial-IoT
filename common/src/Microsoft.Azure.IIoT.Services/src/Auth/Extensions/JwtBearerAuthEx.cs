// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Auth {
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;

    /// <summary>
    /// Configure JWT bearer authentication
    /// </summary>
    public static class JwtBearerAuthEx {

        /// <summary>
        /// Helper to add jwt bearer authentication
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <param name="inDevelopment"></param>
        public static void AddJwtBearerAuthentication(this IServiceCollection services,
            IAuthConfig config, bool inDevelopment) {

            if (config.HttpsRedirectPort > 0) {
                services.AddHsts(options => {
                    options.Preload = true;
                    options.IncludeSubDomains = true;
                    options.MaxAge = TimeSpan.FromDays(60);
                });
                services.AddHttpsRedirection(options => {
                    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
                    options.HttpsPort = config.HttpsRedirectPort;
                });
            }

            // Allow access to context from within token providers and other client auth
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Add jwt bearer auth
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.Authority = config.GetAuthorityUrl() + "/v2.0";
                    options.SaveToken = true; // Save token to allow request on behalf

                    options.TokenValidationParameters = new TokenValidationParameters {
                        ClockSkew = config.AllowedClockSkew,
                        ValidateIssuer = true,
                        IssuerValidator = (iss, t, p) => ValidateIssuer(iss, config),
                        ValidateAudience = !string.IsNullOrEmpty(config.Audience),
                        ValidAudience = config.Audience
                    };
                    options.Events = new JwtBearerEvents {
                        OnAuthenticationFailed = ctx => {
                            if (config.AuthRequired) {
                                ctx.NoResult();
                                return WriteErrorAsync(ctx.Response, inDevelopment ?
                                    ctx.Exception : null);
                            }
                            return Task.CompletedTask;
                        },
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
        }

        /// <summary>
        /// Validate the issuer. The issuer is considered as valid if it
        /// has the same http scheme and authority as the trusted issuer uri
        /// from the configuration file or default uri, plus it has to have
        /// a tenant Id, and optionally v2.0 but nothing more..
        /// </summary>
        /// <param name="issuer">Issuer to validate (will be tenanted)</param>
        /// <param name="config">Authentication configuration</param>
        /// <returns>The <c>issuer</c> if it's valid</returns>
        private static string ValidateIssuer(string issuer, IAuthConfig config) {
            var uri = new Uri(issuer);
            var authorityUri = new Uri(config?.TrustedIssuer ?? kDefaultIssuerUri);
            if (uri.Scheme != authorityUri.Scheme ||
                uri.Authority != authorityUri.Authority) {
                throw new SecurityTokenInvalidIssuerException(
                    "Issuer has wrong authority.");
            }
            var parts = uri.AbsolutePath.Split(new char[] { '/' },
                StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) {
                throw new SecurityTokenInvalidIssuerException(
                    "Issuer is not tenanted.");
            }
            if (parts.Length >= 1 && !Guid.TryParse(parts[0], out var tenantId)) {
                throw new SecurityTokenInvalidIssuerException(
                    "No valid tenant Id for the issuer.");
            }
            if (parts.Length > 1 && parts[2] != "v2.0") {
                throw new SecurityTokenInvalidIssuerException(
                    "Only accepted protocol versions are AAD v1.0 or V2.0");
            }
            return issuer;
        }

        /// <summary>
        /// Helper to write response error
        /// </summary>
        /// <param name="response"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        private static Task WriteErrorAsync(HttpResponse response, Exception ex) {
            response.StatusCode = 500;
            response.ContentType = "text/plain";
            if (ex != null) {
                // Debug only, in production do not share exceptions with the remote host.
                return response.WriteAsync(ex.ToString());
            }
            return response.WriteAsync(
                "An error occurred processing your authentication.");
        }

        private const string kDefaultIssuerUri = "https://sts.windows.net/";
    }
}
