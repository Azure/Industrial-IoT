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
    using System.Linq;

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
            //
            // fix current bug where properties are not added correctly
            // Lookup property schema in schema repo
            //
            if (context.PropertyInfo != null) {
                // Query was passed a parameter with properties
                var propertySchema = context.SchemaRepository.Schemas
                    .Where(p => p.Key.EqualsIgnoreCase(context.ParameterInfo.ParameterType.Name))
                    .SelectMany(p => p.Value.Properties)
                    .Where(p => p.Key.EqualsIgnoreCase(context.PropertyInfo.Name))
                    .FirstOrDefault();
                if (propertySchema.Value != null) {
                    // Replace parameter definition with property schema
                    parameter.Name = propertySchema.Key;
                    parameter.Schema = propertySchema.Value;
                    parameter.Extensions = parameter.Schema.Extensions;
                    parameter.Reference = parameter.Schema.Reference;
                }
            }
            else if (context.ParameterInfo != null) {
                // Query was passed a parameter with properties
                var propertySchema = context.SchemaRepository.Schemas
                    .Where(p => p.Key.EqualsIgnoreCase(context.ParameterInfo.ParameterType.Name))
                    .FirstOrDefault();
                if (propertySchema.Value != null) {
                    // Replace parameter definition with property schema
                    parameter.Name = propertySchema.Key;
                    parameter.Schema = propertySchema.Value;
                    parameter.Extensions = parameter.Schema.Extensions;
                    parameter.Reference = parameter.Schema.Reference;
                    return;
                }
                // Simple parameter was passed - lookup from repo
                AddExtension(context.ParameterInfo.ParameterType, parameter.Extensions);
            }
        }

        /// <summary>
        /// Add enum extension
        /// </summary>
        /// <param name="paramType"></param>
        /// <param name="extensions"></param>
        /// <returns></returns>
        private static void AddExtension(Type paramType,
            IDictionary<string, IOpenApiExtension> extensions) {
            if (paramType == null) {
                return;
            }
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

