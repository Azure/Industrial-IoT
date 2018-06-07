// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Web.Auth {
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
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
            IAuthConfig config, string clientId, bool inDevelopment) => services
            // Allow access to context from within token providers and other client auth
            .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()

            // Add jwt bearer auth
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => {

                options.Authority = config.TrustedIssuer;
                options.Audience = clientId;
                options.SaveToken = true; // Save token to allow us to request another

                options.TokenValidationParameters = new TokenValidationParameters {
                    //  RequireSignedTokens = true,
                    //  ValidIssuer = config.TrustedIssuer,
                    //  ValidAudience = clientId,
                    ClockSkew = config.AllowedClockSkew,

                    //  ValidateLifetime = true,
                    //  ValidateAudience = !string.IsNullOrEmpty(clientId),
                    ValidateAudience = false,
                    // ValidateLifetime = false,
                };

                options.Events = new JwtBearerEvents {
                    OnAuthenticationFailed = ctx => {

                     //   ctx.Request.Headers.TryGetValue("Authorization", out var values);
                     //   if (values.Count == 1) {
                     //       var token = TokenResultModelEx.Parse(values.ToString());
                     //   }

                        if (config.AuthRequired) {
                            ctx.NoResult();
                            return WriteErrorAsync(ctx.Response,
                                inDevelopment ? ctx.Exception : null);
                        }
                        return Task.CompletedTask;
                    }
                };
            });

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


        //  private bool ValidateToken(string token, HttpContext context) {
        //      if (string.IsNullOrEmpty(token)) return false;
        //
        //      try {
        //          SecurityToken validatedToken;
        //          var handler = new JwtSecurityTokenHandler();
        //          handler.ValidateToken(token, tokenValidationParams, out validatedToken);
        //          var jwtToken = new JwtSecurityToken(token);
        //
        //          // Validate the signature algorithm
        //          if (config.JwtAllowedAlgos.Contains(jwtToken.SignatureAlgorithm)) {
        //              // Store the user info in the request context, so the authorization
        //              // header doesn't need to be parse again later in the User controller.
        //              context.Request.SetCurrentUserClaims(jwtToken.Claims);
        //
        //              return true;
        //          }
        //
        //          log.Error("JWT token signature algorithm is not allowed.", () => new { jwtToken.SignatureAlgorithm });
        //      }
        //      catch (Exception e) {
        //          log.Error("Failed to validate JWT token", () => new { e });
        //      }
        //
        //      return false;
        //  }
        //
    }
}
