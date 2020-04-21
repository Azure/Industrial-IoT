// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.OpenApi.Models {
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting.Server;
    using Microsoft.AspNetCore.Hosting.Server.Features;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Enable OpenApi
    /// </summary>
    public static class ApplicationBuilderEx {

        /// <summary>
        /// Use swagger in application
        /// </summary>
        /// <param name="app"></param>
        public static void UseSwagger(this IApplicationBuilder app) {

            var config = app.ApplicationServices.GetRequiredService<IOpenApiConfig>();
            var auth = app.ApplicationServices.GetService<IServerAuthConfig>();
            var server = app.ApplicationServices.GetRequiredService<IServer>();
            var addresses = app.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses
                .Select(a => new Uri(a.Replace("://*", "://localhost")))
                .ToList() ?? new List<Uri>();

            // Enable swagger and swagger ui
            app.UseSwagger(options => {
                options.PreSerializeFilters.Add((doc, request) => {
                    doc.Servers = new List<OpenApiServer>();
                    foreach (var scheme in addresses
                        .Select(a => a.Scheme)
                        .Append("https")
                        .Append(request.Scheme)
                        .Distinct()) {
                        var url = $"{scheme}://{request.Host.Value}";

                        // If config.OpenApiServerHost is set, we will use that instead of request.Host.Value
                        if (!string.IsNullOrEmpty(config.OpenApiServerHost)) {
                            url = $"{scheme}://{config.OpenApiServerHost}";
                        }

                        doc.Servers.Add(new OpenApiServer {
                            Description = $"{scheme} endpoint.",
                            Url = url
                        });
                    }

                    // If request.PathBase exists, then we will prepend it to doc.Paths.
                    if (request.PathBase.HasValue) {
                        var pathBase = request.PathBase.Value;
                        var prefixedPaths = new OpenApiPaths();
                        foreach (var path in doc.Paths) {
                            prefixedPaths.Add(pathBase + path.Key, path.Value);
                        }
                        doc.Paths = prefixedPaths;
                    }
                });
                options.SerializeAsV2 = true;
                options.RouteTemplate = "swagger/{documentName}/openapi.json";
            });
            if (!config.UIEnabled) {
                return;
            }

            var api = app.ApplicationServices.GetRequiredService<IActionDescriptorCollectionProvider>();
            var infos = api.GetOpenApiInfos(null, null);

            // Where to host the ui
            app.UseSwaggerUI(options => {
                foreach (var info in infos) {
                    if (config.WithAuth) {
                        options.OAuthAppName(info.Title);
                        options.OAuthClientId(config.OpenApiAppId);
                        if (!string.IsNullOrEmpty(config.OpenApiAppSecret)) {
                            options.OAuthClientSecret(config.OpenApiAppSecret);
                        }
                        var resource = auth?.JwtBearerProviders?.FirstOrDefault();
                        if (!string.IsNullOrEmpty(resource?.Audience)) {
                            options.OAuthAdditionalQueryStringParams(
                                new Dictionary<string, string> {
                                    ["resource"] = resource.Audience
                                });
                        }
                    }
                    options.SwaggerEndpoint($"{info.Version}/openapi.json",
                        info.Version);
                }
            });
        }
    }
}

