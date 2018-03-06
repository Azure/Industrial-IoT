// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Auth {

    /// <summary>
    /// Cors setup implementation
    /// </summary>
    public class CorsSetup : ICorsSetup
    {
        private readonly IClientAuthConfig config;
        private readonly ILogger log;

        /// <summary>
        /// Default constructor
        /// </summary>
        public CorsSetup(
            IClientAuthConfig config,
            ILogger logger)
        {
            this.config = config;
            log = logger;
        }

        /// <summary>
        /// Configure app to use cors middleware
        /// </summary>
        /// <param name="app"></param>
        public void UseMiddleware(IApplicationBuilder app)
        {
            if (config.CorsEnabled)
            {
                log.Warn("CORS is enabled", () => { });
                app.UseCors(BuildCorsPolicy);
            }
            else
            {
                log.Info("CORS is disabled", () => { });
            }
        }

        private void BuildCorsPolicy(CorsPolicyBuilder builder)
        {
            CorsWhitelistModel model;
            try
            {
                model = JsonConvert.DeserializeObject<CorsWhitelistModel>(config.CorsWhitelist);
                if (model == null)
                {
                    log.Error("Invalid CORS whitelist. Ignored", () => new { config.CorsWhitelist });
                    return;
                }
            }
            catch (Exception ex)
            {
                log.Error("Invalid CORS whitelist. Ignored", () => new { config.CorsWhitelist, ex.Message });
                return;
            }

            if (model.Origins == null)
            {
                log.Info("No setting for CORS origin policy was found, ignore", () => { });
            }
            else if (model.Origins.Contains("*"))
            {
                log.Info("CORS policy allowed any origin", () => { });
                builder.AllowAnyOrigin();
            }
            else
            {
                log.Info("Add specified origins to CORS policy", () => new { model.Origins });
                builder.WithOrigins(model.Origins);
            }

            if (model.Origins == null)
            {
                log.Info("No setting for CORS method policy was found, ignore", () => { });
            }
            else if (model.Methods.Contains("*"))
            {
                log.Info("CORS policy allowed any method", () => { });
                builder.AllowAnyMethod();
            }
            else
            {
                log.Info("Add specified methods to CORS policy", () => new { model.Methods });
                builder.WithMethods(model.Methods);
            }

            if (model.Origins == null)
            {
                log.Info("No setting for CORS header policy was found, ignore", () => { });
            }
            else if (model.Headers.Contains("*"))
            {
                log.Info("CORS policy allowed any header", () => { });
                builder.AllowAnyHeader();
            }
            else
            {
                log.Info("Add specified headers to CORS policy", () => new { model.Headers });
                builder.WithHeaders(model.Headers);
            }
        }
    }
}
