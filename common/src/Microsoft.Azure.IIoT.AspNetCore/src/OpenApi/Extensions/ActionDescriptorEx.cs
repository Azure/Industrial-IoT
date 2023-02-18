// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.OpenApi.Models {
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// ActionDescriptorCollectionProvider extensions
    /// </summary>
    public static class ActionDescriptorEx {

        /// <summary>
        /// Collect configured scopes from all controllers registered as services
        /// </summary>
        /// <param name="services"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetRequiredScopes(
            this IActionDescriptorCollectionProvider services, IOptions<AuthorizationOptions> options) {
            return services.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Cast<ControllerActionDescriptor>()
                .SelectMany(d => d.GetRequiredPolicyGlaims(options.Value))
                .Distinct();
        }

        /// <summary>
        /// Collect configured scopes from all controllers registered as services
        /// </summary>
        /// <param name="services"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static IEnumerable<OpenApiInfo> GetOpenApiInfos(
            this IActionDescriptorCollectionProvider services, string title, string description) {
            var versions = services.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Cast<ControllerActionDescriptor>()
                .SelectMany(d => d.GetApiVersions())
                .Distinct()
                .ToList(); ;
            if (versions.Count == 0) {
                versions.Add("1");
            }
            return versions.Select(version => new OpenApiInfo {
                Title = title ?? "Api",
                Description = description ?? "Api",
                Version = "v" + version,
                Contact = new OpenApiContact {
                    Url = new Uri("https://www.github.com/Azure/Industrial-IoT"),
                },
                License = new OpenApiLicense() {
                    Name = "MIT LICENSE",
                    Url = new System.Uri("https://opensource.org/licenses/MIT")
                }
            });
        }
    }
}

