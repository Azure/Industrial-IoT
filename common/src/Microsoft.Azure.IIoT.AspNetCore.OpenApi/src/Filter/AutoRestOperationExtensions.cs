// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.OpenApi {
    using Swashbuckle.AspNetCore.SwaggerGen;
    using System;
    using System.Linq;
    using System.Reflection;
    using Microsoft.OpenApi.Models;
    using Microsoft.OpenApi.Any;

    /// <summary>
    /// Add autorest operation extensions
    /// </summary>
    internal class AutoRestOperationExtensions : IOperationFilter {

        /// <inheritdoc/>
        public virtual void Apply(OpenApiOperation operation, OperationFilterContext context) {
            var name = context.MethodInfo.Name;
            if (name.EndsWith("Async", StringComparison.InvariantCultureIgnoreCase)) {
                var autoOperationId = name.Substring(0, name.Length - 5);
                if (autoOperationId.Length < operation.OperationId.Length) {
                    operation.OperationId = autoOperationId;
                }
            }
            if (operation.OperationId.Contains("CreateOrUpdate") &&
                context.ApiDescription.HttpMethod.EqualsIgnoreCase("PATCH")) {
                operation.OperationId = operation.OperationId.Replace("CreateOrUpdate", "Update");
            }
            var attribute = context.MethodInfo
                .GetCustomAttributes<AutoRestExtensionAttribute>().FirstOrDefault();
            if (attribute != null) {
                if (attribute.LongRunning) {
                    operation.Extensions.Add("x-ms-long-running-operation", new OpenApiBoolean(true));
                }
                if (!string.IsNullOrEmpty(attribute.NextPageLinkName)) {
                    operation.Extensions.Add("x-ms-pageable",
                        new OpenApiObject {
                            ["nextLinkName"] = new OpenApiString(attribute.NextPageLinkName)
                        });
                }
            }
        }
    }
}

