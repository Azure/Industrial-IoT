// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.OpenApi
{
    using Microsoft.OpenApi;
    using Swashbuckle.AspNetCore.SwaggerGen;
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Add autorest operation extensions
    /// </summary>
    internal class AutoRestOperationExtensions : IOperationFilter
    {
        /// <inheritdoc/>
        public virtual void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var versionParameter = operation.Parameters?.SingleOrDefault(p => p.Name == "version");
            if (versionParameter != null)
            {
                operation.Parameters!.Remove(versionParameter);
            }
            if (operation.OperationId == null)
            {
                operation.OperationId = context.MethodInfo.Name;
                if (operation.OperationId.EndsWith("Async", StringComparison.InvariantCultureIgnoreCase))
                {
                    var name = operation.OperationId;
                    operation.OperationId = name[0..^5];
                }
            }
            if (operation.OperationId.Contains("CreateOrUpdate", StringComparison.Ordinal) &&
                (context.ApiDescription.HttpMethod?.EqualsIgnoreCase("PATCH") ?? false))
            {
                operation.OperationId = operation.OperationId.Replace(
                    "CreateOrUpdate", "Update", StringComparison.Ordinal);
            }

            var attribute = context.MethodInfo
                .GetCustomAttributes<AutoRestExtensionAttribute>().FirstOrDefault();
            if (attribute != null)
            {
                operation.Extensions ??= new System.Collections.Generic.Dictionary<string, IOpenApiExtension>();
                if (attribute.LongRunning)
                {
                    operation.Extensions.Add("x-ms-long-running-operation",
                        new JsonNodeExtension(JsonValue.Create(true)!));
                }
                if (!string.IsNullOrEmpty(attribute.NextPageLinkName))
                {
                    operation.Extensions.Add("x-ms-pageable",
                        new JsonNodeExtension(new JsonObject
                        {
                            ["nextLinkName"] = JsonValue.Create(attribute.NextPageLinkName)
                        }));
                }
            }

            if (operation.Responses != null)
            {
                foreach (var produces in operation.Responses.ToList())
                {
                    produces.Value.Description = produces.Value.Description.SingleSpacesNoLineBreak();
                }
            }

            if (operation.Parameters != null)
            {
                foreach (var param in operation.Parameters)
                {
                    if (param is OpenApiParameter p)
                    {
                        p.Description = p.Description.SingleSpacesNoLineBreak();
                        if (p.Schema is OpenApiSchema paramSchema)
                        {
                            paramSchema.Description = paramSchema.Description.SingleSpacesNoLineBreak();
                        }
                    }
                }
            }
            if (operation.RequestBody is OpenApiRequestBody body)
            {
                body.Description = body.Description.SingleSpacesNoLineBreak();
            }
            operation.Description = operation.Description.SingleSpacesNoLineBreak();
        }
    }
}
