// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.OpenApi {
    using Swashbuckle.AspNetCore.SwaggerGen;
    using System;
    using System.Collections.Generic;
    using Microsoft.OpenApi.Models;
    using Microsoft.OpenApi.Interfaces;
    using Microsoft.OpenApi.Any;

    /// <summary>
    /// Add extensions for autorest to schemas
    /// </summary>
    internal class AutoRestSchemaExtensions : ISchemaFilter, IParameterFilter {

        /// <inheritdoc/>
        public void Apply(OpenApiSchema model, SchemaFilterContext context) {
            AddExtension(context.Type, model.Extensions);
        }

        /// <inheritdoc/>
        public void Apply(OpenApiParameter parameter, ParameterFilterContext context) {
            AddExtension(context.ParameterInfo.ParameterType, parameter.Extensions);
        }

        /// <summary>
        /// Add enum extension
        /// </summary>
        /// <param name="paramType"></param>
        /// <param name="extensions"></param>
        /// <returns></returns>
        private static void AddExtension(Type paramType,
            IDictionary<string, IOpenApiExtension> extensions) {
            if (paramType.IsGenericType &&
                paramType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                // Most of the model enums are nullable
                paramType = paramType.GetGenericArguments()[0];
            }
            if (paramType.IsEnum) {
                extensions.Add("x-ms-enum", new OpenApiObject {
                    ["name"] = new OpenApiString(paramType.Name),
                    ["modelAsString"] = new OpenApiBoolean(false)
                    // TODO: Investigate,
                    //  values = paramType
                    //      .GetFields(BindingFlags.Static | BindingFlags.Public)
                    //      .Select(field => new {
                    //          name = field.Name,
                    //          value = field.Name,
                    //      })
                });
            }
        }
    }
}

