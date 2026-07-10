// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.OpenApi
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Microsoft.OpenApi;
    using Swashbuckle.AspNetCore.SwaggerGen;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Add extensions for autorest to schemas
    /// </summary>
    internal class AutoRestSchemaExtensions : ISchemaFilter, IParameterFilter, IRequestBodyFilter
    {
        /// <inheritdoc/>
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == null || schema is not OpenApiSchema s)
            {
                return;
            }
            AdjustSchema(context.Type, s);
            s.Description = s.Description.SingleSpacesNoLineBreak();
            if (s.Items is OpenApiSchema items)
            {
                items.Description = items.Description.SingleSpacesNoLineBreak();
            }
        }

        /// <inheritdoc/>
        public void Apply(IOpenApiRequestBody requestBody, RequestBodyFilterContext context)
        {
            if (requestBody is OpenApiRequestBody body)
            {
                body.Description = body.Description.SingleSpacesNoLineBreak();
            }
        }

        /// <inheritdoc/>
        public void Apply(IOpenApiParameter parameter, ParameterFilterContext context)
        {
            if (parameter is not OpenApiParameter p)
            {
                return;
            }
            //
            // fix current bug where properties are not added correctly
            // Lookup property schema in schema repo
            //
            if (context.PropertyInfo != null)
            {
                // Query was passed a parameter with properties
                var propertySchema = context.SchemaRepository.Schemas
                    .Where(pair => pair.Key.EqualsIgnoreCase(context.ParameterInfo.ParameterType.Name))
                    .SelectMany(pair => pair.Value.Properties ??
                        new Dictionary<string, IOpenApiSchema>())
                    .FirstOrDefault(pair => pair.Key.EqualsIgnoreCase(context.PropertyInfo.Name));
                if (propertySchema.Value is OpenApiSchema ps)
                {
                    ps.Description = ps.Description.SingleSpacesNoLineBreak();
                    // Replace parameter definition with property schema
                    p.Name = propertySchema.Key;
                    // Quick and dirty clone of the schema for the parameter
                    p.Schema = ps.CreateShallowCopy();
                }
                p.Required = context.PropertyInfo
                    .GetCustomAttributes(typeof(RequiredAttribute), true)
                    .Length > 0;
                if (p.Schema is OpenApiSchema pSchema)
                {
                    AdjustSchema(context.PropertyInfo.PropertyType, pSchema);
                }
            }
            else if (context.ParameterInfo != null && p.Schema is OpenApiSchema pSchema2)
            {
                // Query was passed a parameter with properties
                AdjustSchema(context.ParameterInfo.ParameterType, pSchema2);
            }
            if (p.Schema is OpenApiSchema outSchema)
            {
                outSchema.Description = outSchema.Description.SingleSpacesNoLineBreak();
            }
            p.Description = p.Description.SingleSpacesNoLineBreak();
        }

        /// <summary>
        /// Adjust schema
        /// </summary>
        /// <param name="paramType"></param>
        /// <param name="model"></param>
        internal static void AdjustSchema(Type paramType, OpenApiSchema? model)
        {
            if (model == null)
            {
                return;
            }
            if (paramType != null)
            {
                if (paramType.IsGenericType)
                {
                    if (paramType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        // Most of the model enums are nullable
                        MakeNullable(model);
                    }
                    paramType = paramType.GetGenericArguments()[0];
                }
                if (paramType.IsAssignableTo(typeof(JsonNode)))
                {
                    model.Type = JsonSchemaType.Object; // Any
                    model.Format = null;
                    MakeNullable(model);
                    model.Description = "A variant which can be represented by any value including null.";
                }
                if (paramType == typeof(uint))
                {
                    model.Type = JsonSchemaType.Integer;
                    model.Format = "int64";
                }
                else if (paramType.IsEnum)
                {
                    model.Type = JsonSchemaType.String;
                    model.Format = null;
                    model.Enum = Enum.GetValues(paramType)
                        .Cast<object>()
                        .Select(v => JsonSerializer.Serialize(v, v.GetType(), Json.Options)
                            .Trim('"'))
                        .Select(n => (JsonNode)JsonValue.Create(n)!)
                        .ToList();
                    model.Extensions ??= new Dictionary<string, IOpenApiExtension>();
                    model.Extensions["x-ms-enum"] = new JsonNodeExtension(new JsonObject
                    {
                        ["name"] = JsonValue.Create(paramType.Name),
                        ["modelAsString"] = JsonValue.Create(false)
                    });
                }
            }
        }

        private static void MakeNullable(OpenApiSchema model)
        {
            model.Type = (model.Type ?? JsonSchemaType.Object) | JsonSchemaType.Null;
        }
    }
}
