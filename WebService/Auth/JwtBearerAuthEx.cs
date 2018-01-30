// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.Auth {
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
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
            IClientAuthConfig config, bool inDevelopment) {

            // This can be removed after https://github.com/aspnet/IISIntegration/issues/371
            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => {

                options.Authority = config.JwtIssuer; //["jwt:authority"],
                options.Audience = config.JwtAudience;

                options.TokenValidationParameters.RequireSignedTokens = true;
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidIssuer = config.JwtIssuer;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidAudience = config.JwtAudience;
                options.TokenValidationParameters.ValidateLifetime = true;
                options.TokenValidationParameters.ClockSkew = config.JwtClockSkew;

                options.Events = new JwtBearerEvents {
                    OnMessageReceived = ctx => {
                        if (!string.IsNullOrEmpty(ctx.Token)) {
                            if (config.AuthRequired && config.JwtAllowedAlgos.Any()) {
                                // Validate security algorithm is one of the configured ones
                                var jwtToken = new JwtSecurityToken(ctx.Token);
                                if (!config.JwtAllowedAlgos.Contains(jwtToken.SignatureAlgorithm)) {
                                    ctx.NoResult();
                                    return WriteErrorAsync(ctx.Response, null);
                                }
                            }
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = ctx => {
                        if (config.AuthRequired) {
                            ctx.NoResult();
                            return WriteErrorAsync(ctx.Response,
                                inDevelopment ? ctx.Exception : null);
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
        //  private async Task<bool> InitializeTokenValidationAsync(CancellationToken token) {
        //      if (tokenValidationInitialized) return true;
        //
        //      try {
        //          log.Info("Initializing OpenID configuration", () => { });
        //          var openIdConfig = await openIdCfgMan.GetConfigurationAsync(token);
        //
        //          tokenValidationParams = new TokenValidationParameters {
        //              // Validate the token signature
        //              RequireSignedTokens = true,
        //              ValidateIssuerSigningKey = true,
        //              IssuerSigningKeys = openIdConfig.SigningKeys,
        //
        //              // Validate the token issuer
        //              ValidateIssuer = true,
        //              ValidIssuer = config.JwtIssuer,
        //
        //              // Validate the token audience
        //              ValidateAudience = true,
        //              ValidAudience = config.JwtAudience,
        //
        //              // Validate token lifetime
        //              ValidateLifetime = true,
        //              ClockSkew = config.JwtClockSkew
        //          };
        //
        //          tokenValidationInitialized = true;
        //      }
        //      catch (Exception e) {
        //          log.Error("Failed to setup OpenId Connect", () => new { e });
        //      }
        //
        //      return tokenValidationInitialized;
        //  }
    }
}
