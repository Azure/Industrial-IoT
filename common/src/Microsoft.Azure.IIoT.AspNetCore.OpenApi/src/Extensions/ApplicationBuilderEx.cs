// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.OpenApi.Models {
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Hosting.Server;
    using Microsoft.AspNetCore.Hosting.Server.Features;

    /// <summary>
    /// Enable OpenApi
    /// </summary>
    public static class ApplicationBuilderEx {

        /// <summary>
        /// Use swagger in application
        /// </summary>
        /// <param name="app"></param>
        /// <param name="infos"></param>
        public static void UseSwagger(this IApplicationBuilder app, params OpenApiInfo[] infos) {

            if (infos == null) {
                throw new ArgumentNullException(nameof(infos));
            }

            var config = app.ApplicationServices.GetRequiredService<IOpenApiConfig>();
            var server = app.ApplicationServices.GetRequiredService<IServer>();
            var addresses = app.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses
                .Select(a => new Uri(a.Replace("://*", "://localhost")))
                .ToList() ?? new List<Uri>();
            var path = addresses.FirstOrDefault()?.PathAndQuery.Trim('/') ??
                string.Empty;

            // Enable swagger and swagger ui
            app.UseSwagger(options => {
                options.PreSerializeFilters.Add((doc, request) => {
                    doc.Servers = new List<OpenApiServer>();
                    foreach (var scheme in addresses
                        .Select(a => a.Scheme)
                        .Append("https")
                        .Append(request.Scheme)
                        .Distinct()) {
                        var basePath = path;
                        if (request.Headers.TryGetValue(HttpHeader.Location,
                                out var values) && values.Count > 0) {
                            basePath += "/" + values[0];
                        }
                        doc.Servers.Add(new OpenApiServer {
                            Description = $"{scheme} endpoint.",
                            Url = new UriBuilder {
                                Scheme = scheme,
                                Host = request.Host.Value,
                                Path = basePath
                            }.Uri.ToString()
                        });
                    }
                });
                options.SerializeAsV2 = true;
                options.RouteTemplate = "{documentName}/openapi.json";
            });
            if (!config.UIEnabled) {
                return;
            }

            app.UseSwaggerUI(options => {
                foreach (var info in infos) {
                    if (config.WithAuth) {
                        options.OAuthAppName(info.Title);
                        options.OAuthClientId(config.OpenApiAppId);
                        if (!string.IsNullOrEmpty(config.OpenApiAppSecret)) {
                            options.OAuthClientSecret(config.OpenApiAppSecret);
                        }
                        var resource = config as IAuthConfig;
                        if (!string.IsNullOrEmpty(resource?.Audience)) {
                            options.OAuthAdditionalQueryStringParams(
                                new Dictionary<string, string> {
                                    ["resource"] = resource.Audience
                                });
                        }
                    }
                    options.RoutePrefix = "";
                    options.SwaggerEndpoint($"{info.Version}/openapi.json",
                        info.Version);
                }
            });
        }
    }
}

