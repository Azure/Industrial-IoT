// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.OpenApi
{
    using global::Azure.IIoT.OpcUa.Publisher.Module.OpenApi;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// ActionDescriptorCollectionProvider extensions
    /// </summary>
    internal static class ActionDescriptorEx
    {
        /// <summary>
        /// Collect configured scopes from all controllers registered as services
        /// </summary>
        /// <param name="services"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="options"></param>
        public static IEnumerable<OpenApiInfo> GetOpenApiInfos(
            this IActionDescriptorCollectionProvider services, string? title, string? description,
            OpenApiOptions? options)
        {
            var versions = services.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .SelectMany(d => d.GetApiVersions())
                .Distinct()
                .ToList();
            if (versions.Count == 0)
            {
                versions.Add("1");
            }
            return versions.Select(version => new OpenApiInfo
            {
                Title = title ?? "Api",
                Description = description ?? "Api",
                Version = "v" + version,
                Contact = new OpenApiContact
                {
                    Url = options?.ProjectUri ??
                        new Uri("https://www.github.com/Azure/Industrial-IoT"),
                },
                License = options?.License ?? new OpenApiLicense()
                {
                    Name = "MIT LICENSE",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });
        }
    }
}
