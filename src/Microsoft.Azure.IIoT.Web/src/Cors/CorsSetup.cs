// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Web.Cors {
    using Microsoft.Azure.IIoT.Web.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
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
        public CorsSetup(ICorsConfig config, ILogger logger) {
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
            app.UseCors(builder => {
                _log.Info("CORS is enabled", () => { });

                if (_config.CorsWhitelist == "*") {
                    _log.Info("Allow all headers, origins and methods", () => { });
                    builder.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod();
                    return;
                }

                var model = ReadWhiteList();
                if (model != null) {
                    Configure(nameof(model.Methods), model.Methods,
                        () => builder.AllowAnyMethod(), p => builder.WithMethods(p));
                    Configure(nameof(model.Origins), model.Origins,
                        () => builder.AllowAnyOrigin(), p => builder.WithOrigins(p));
                    Configure(nameof(model.Headers), model.Headers,
                        () => builder.AllowAnyHeader(), p => builder.WithHeaders(p));
                }
            });
        }

        /// <summary>
        /// Configure
        /// </summary>
        /// <param name="name"></param>
        /// <param name="policies"></param>
        /// <param name="all"></param>
        /// <param name="specific"></param>
        private void Configure(string name, string[] policies, Action all,
            Action<string[]> specific) {
            if (policies == null) {
                _log.Info($"No setting for CORS {name} policy was found, ignore",
                    () => { });
            }
            else if (policies.Contains("*")) {
                _log.Info($"CORS policy for {name} allows any header", () => { });
                all();
            }
            else {
                _log.Info($"Add specified {name} policies to CORS policy",
                    () => policies);
                specific(policies);
            }
        }

        /// <summary>
        /// Helper to read white list
        /// </summary>
        /// <returns></returns>
        private CorsWhitelistModel ReadWhiteList() {
            try {
                var model = JsonConvertEx.DeserializeObject<CorsWhitelistModel>(
                    _config.CorsWhitelist);
                if (model == null) {
                    _log.Error("Invalid CORS whitelist. Ignored", () => new {
                        _config.CorsWhitelist
                    });
                }
                return model;
            }
            catch (Exception ex) {
                _log.Error("Invalid CORS whitelist. Ignored", () => new {
                    _config.CorsWhitelist,
                    ex.Message
                });
                return null;
            }
        }

        private readonly ICorsConfig _config;
        private readonly ILogger _log;
    }
}
