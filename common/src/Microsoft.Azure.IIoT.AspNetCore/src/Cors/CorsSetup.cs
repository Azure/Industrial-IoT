// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Cors {
    using Microsoft.Azure.IIoT.AspNetCore.Models;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Logging;
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
            _logger = logger;
        }

        /// <summary>
        /// Configure app to use cors middleware
        /// </summary>
        /// <param name="app"></param>
        public void UseMiddleware(IApplicationBuilder app) {
            if (!_config.CorsEnabled) {
                _logger.LogInformation("CORS is disabled");
                return;
            }
            app.UseCors(builder => {
                _logger.LogInformation("CORS is enabled");

                if (_config.CorsWhitelist == "*") {
                    _logger.LogInformation("Allow all headers, origins and methods");
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
                _logger.LogInformation("No setting for CORS {Name} policy was found, ignore", name);
            }
            else if (policies.Contains("*")) {
                _logger.LogInformation("CORS policy for {Name} allows any header", name);
                all();
            }
            else {
                _logger.LogInformation("Add specified {Name} policies to CORS policy", name);
                specific(policies);
            }
        }

        /// <summary>
        /// Helper to read white list
        /// </summary>
        /// <returns></returns>
        private CorsWhitelistModel ReadWhiteList() {
            try {
                var model = JsonConvert.DeserializeObject<CorsWhitelistModel>(
                    _config.CorsWhitelist);
                if (model == null) {
                    _logger.LogError("Invalid CORS whitelist {Whitelist}. Ignored",
                        _config.CorsWhitelist);
                }
                return model;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Invalid CORS whitelist {Whitelist}. Ignored",
                    _config.CorsWhitelist);
                return null;
            }
        }

        private readonly ICorsConfig _config;
        private readonly ILogger _logger;
    }
}
