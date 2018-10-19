// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Auth {
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Threading.Tasks;

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

            // Allow access to context from within token providers and other client auth
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Add jwt bearer auth
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {

                    options.Authority = config.Authority;
                    options.SaveToken = true; // Save token to allow us to request on behalf

                    options.TokenValidationParameters = new TokenValidationParameters {
                        ClockSkew = config.AllowedClockSkew,
                        ValidIssuer = config.TrustedIssuer,
                        ValidAudience = config.Audience,
                        ValidateAudience = false,
                        ValidateIssuer = false
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
            return response.WriteAsync("An error occurred processing your authentication.");
        }
    }
}
