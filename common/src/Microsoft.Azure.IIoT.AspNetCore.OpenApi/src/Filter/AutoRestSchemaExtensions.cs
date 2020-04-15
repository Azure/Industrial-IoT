// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.OpenApi {
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.OpenApi.Any;
    using Microsoft.OpenApi.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Swashbuckle.AspNetCore.SwaggerGen;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Add extensions for autorest to schemas
    /// </summary>
    internal class AutoRestSchemaExtensions : ISchemaFilter, IParameterFilter, IRequestBodyFilter {

        /// <inheritdoc/>
        public void Apply(OpenApiSchema model, SchemaFilterContext context) {
            if (context.Type == null) {
                return;
            }
            AdjustSchema(context.Type, model);
            model.Description = model.Description.SingleSpacesNoLineBreak();
            if (model.Items != null) {
                model.Items.Description = model.Items.Description.SingleSpacesNoLineBreak();
            }
        }

        /// <inheritdoc/>
        public void Apply(OpenApiRequestBody requestBody, RequestBodyFilterContext context) {
            requestBody.Description = requestBody.Description.SingleSpacesNoLineBreak();
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
                    propertySchema.Value.Description =
                        propertySchema.Value.Description.SingleSpacesNoLineBreak();
                    // Replace parameter definition with property schema
                    parameter.Name = propertySchema.Key;
                    // Quick and dirty clone of the schema for the parameter
                    parameter.Schema = JsonConvert.DeserializeObject<OpenApiSchema>(
                        JsonConvert.SerializeObject(propertySchema.Value));
                }
                parameter.Required = context.PropertyInfo
                    .GetCustomAttributes(typeof(RequiredAttribute), true)
                    .Any();
                AdjustSchema(context.PropertyInfo.PropertyType, parameter.Schema);
            }
            else if (context.ParameterInfo != null) {
                // Query was passed a parameter with properties
                AdjustSchema(context.ParameterInfo.ParameterType, parameter.Schema);
            }
            if (parameter.Schema != null) {
                parameter.Schema.Description = parameter.Schema.Description.SingleSpacesNoLineBreak();
            }
            parameter.Description = parameter.Description.SingleSpacesNoLineBreak();
        }

        /// <summary>
        /// Adjust schema
        /// </summary>
        /// <param name="paramType"></param>
        /// <param name="model"></param>
        internal static void AdjustSchema(Type paramType, OpenApiSchema model) {
            if (model == null) {
                return;
            }
            if (paramType != null) {
                if (paramType.IsGenericType) {
                    if (paramType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                        // Most of the model enums are nullable
                        model.Nullable = true;
                    }
                    paramType = paramType.GetGenericArguments()[0];
                }
                if (paramType == typeof(VariantValue)) {
                    model.Type = null; // any
                    model.Format = null;
                    model.Nullable = true;
                    model.Description = "A variant which can be represented by any value including null.";
                }
                if (paramType == typeof(uint)) {
                    model.Type = "integer";
                    model.Format = "int64";
                }
                else if (paramType.IsEnum) {
                    model.Type = "string";
                    model.Enum = Enum.GetValues(paramType)
                        .Cast<object>()
                        .Select(v => JsonConvert.SerializeObject(v, new StringEnumConverter())
                            .TrimQuotes())
                        .Select(n => (IOpenApiAny)new OpenApiString(n))
                        .ToList();
                    model.Extensions.AddOrUpdate("x-ms-enum", new OpenApiObject {
                        ["name"] = new OpenApiString(paramType.Name),
                        ["modelAsString"] = new OpenApiBoolean(false)
                    });
                }
            }
        }
    }
}

