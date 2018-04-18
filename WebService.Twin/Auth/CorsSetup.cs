// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Auth {
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.AspNetCore.Builder;
    using Newtonsoft.Json;
    using System;
    using System.Linq;

    /// <summary>
    /// Cors setup implementation
    /// </summary>
    public class CorsSetup : ICorsSetup {

        /// <summary>
        /// Default constructor
        /// </summary>
        public CorsSetup(IClientAuthConfig config, ILogger logger) {
            _config = config;
            _log = logger;
        }

        /// <summary>
        /// Configure app to use cors middleware
        /// </summary>
        /// <param name="app"></param>
        public void UseMiddleware(IApplicationBuilder app) {
            if (!_config.CorsEnabled) {
                _log.Info("CORS is disabled", () => { });
                return;
            }
            _log.Warn("CORS is enabled", () => { });
            app.UseCors(builder => {
                if (_config.CorsWhitelist == "*") {
                    _log.Info("Allow all headers, origins and methods", () => { });
                    builder
                        .AllowAnyHeader()
                        .AllowAnyOrigin()
                        .AllowAnyMethod();
                    return;
                }

                CorsWhitelistModel model;
                try {
                    model = JsonConvert.DeserializeObject<CorsWhitelistModel>(
                        _config.CorsWhitelist);
                    if (model == null) {
                        _log.Error("Invalid CORS whitelist. Ignored", () => new {
                            _config.CorsWhitelist
                        });
                        return;
                    }
                }
                catch (Exception ex) {
                    _log.Error("Invalid CORS whitelist. Ignored", () => new {
                        _config.CorsWhitelist,
                        ex.Message
                    });
                    return;
                }

                if (model.Origins == null) {
                    _log.Info("No setting for CORS origin policy was found, ignore",
                        () => { });
                }
                else if (model.Origins.Contains("*")) {
                    _log.Info("CORS policy allowed any origin", () => { });
                    builder.AllowAnyOrigin();
                }
                else {
                    _log.Info("Add specified origins to CORS policy", 
                        () => model.Origins);
                    builder.WithOrigins(model.Origins);
                }

                if (model.Origins == null) {
                    _log.Info("No setting for CORS method policy was found, ignore",
                        () => { });
                }
                else if (model.Methods.Contains("*")) {
                    _log.Info("CORS policy allowed any method", () => { });
                    builder.AllowAnyMethod();
                }
                else {
                    _log.Info("Add specified methods to CORS policy", 
                        () => model.Methods);
                    builder.WithMethods(model.Methods);
                }

                if (model.Origins == null) {
                    _log.Info("No setting for CORS header policy was found, ignore", 
                        () => { });
                }
                else if (model.Headers.Contains("*")) {
                    _log.Info("CORS policy allowed any header", () => { });
                    builder.AllowAnyHeader();
                }
                else {
                    _log.Info("Add specified headers to CORS policy", 
                        () => model.Headers);
                    builder.WithHeaders(model.Headers);
                }
            });
        }
        
        private readonly IClientAuthConfig _config;
        private readonly ILogger _log;
    }
}
