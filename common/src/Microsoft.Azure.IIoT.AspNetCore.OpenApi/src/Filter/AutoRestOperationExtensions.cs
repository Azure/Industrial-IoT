// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.OpenApi {
    using Microsoft.OpenApi.Models;
    using Microsoft.OpenApi.Any;
    using Swashbuckle.AspNetCore.SwaggerGen;
    using System;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Add autorest operation extensions
    /// </summary>
    internal class AutoRestOperationExtensions : IOperationFilter {

        /// <inheritdoc/>
        public virtual void Apply(OpenApiOperation operation, OperationFilterContext context) {
            var versionParameter = operation.Parameters.SingleOrDefault(p => p.Name == "version");
            if (versionParameter != null) {
                operation.Parameters.Remove(versionParameter);
            }
            if (operation.OperationId == null) {
                operation.OperationId = context.MethodInfo.Name;
                if (operation.OperationId.EndsWith("Async", StringComparison.InvariantCultureIgnoreCase)) {
                    var name = operation.OperationId;
                    operation.OperationId = name[0..^5];
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

            // Fix up produces
            foreach (var produces in operation.Responses.ToList()) {
                if (produces.Key != "200") {
                    operation.Responses.Remove(produces.Key);
                }
            }

            foreach (var param in operation.Parameters) {
                param.Description = param.Description.SingleSpacesNoLineBreak();
                if (param.Schema != null) {
                    param.Schema.Description = param.Schema.Description.SingleSpacesNoLineBreak();
                }
            }
            if (operation.RequestBody != null) {
                operation.RequestBody.Description =
                    operation.RequestBody.Description.SingleSpacesNoLineBreak();
            }
            operation.Description = operation.Description.SingleSpacesNoLineBreak();
        }
    }
}

