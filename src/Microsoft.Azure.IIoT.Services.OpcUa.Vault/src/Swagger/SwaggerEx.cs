// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Swagger
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Services.Swagger;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Swashbuckle.AspNetCore.Swagger;
    using Swashbuckle.AspNetCore.SwaggerGen;

    /// <summary>
    /// Configure swagger
    /// </summary>
    public static class SwaggerEx
    {

        /// <summary>
        /// Configure swagger
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <param name="info"></param>
        public static void AddSwaggerEx(
            this IServiceCollection services,
            ISwaggerConfig config,
            Info info)
        {

            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            // Generate swagger documentation
            services.AddSwaggerGen(options =>
            {
                // Generate doc for version
                options.SwaggerDoc(info.Version, info);

                // Add annotations
                options.EnableAnnotations();

                // send all enums as string
                // required for x-ms-enum
                options.DescribeAllEnumsAsStrings();

                // Add help
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
                    config.GetType().Assembly.GetName().Name + ".xml"), true);

                // Add autorest extensions
                options.SchemaFilter<AutoRestSchemaExtensions>();

                // If auth enabled, need to have bearer token to access any api
                if (config.WithAuth) {
                    var resource = config as IClientConfig;
                    if (string.IsNullOrEmpty(config.SwaggerAppId) || resource == null) {
                        options.AddSecurityDefinition("bearer", new ApiKeyScheme {
                            Description =
                                "Authorization token in the form of 'bearer <token>'",
                            Name = "Authorization",
                            In = "header"
                        });
                        options.OperationFilter<AutoRestOperationExtensions>();
                    }
                    else {
                        options.AddSecurityDefinition("oauth2", new OAuth2Scheme {
                            Type = "oauth2",
                            Description = "Implicit oauth2 token flow.",
                            Flow = "implicit",
                            AuthorizationUrl = resource.GetAuthorityUrl() +
                                "/oauth2/authorize",
                            Scopes = services.GetRequiredScopes()
                                .ToDictionary(k => k, k => $"Access {k} operations")
                        });
                        options.OperationFilter<SecurityRequirementsOperationFilter>();
                    }
                }
                else {
                    options.OperationFilter<AutoRestOperationExtensions>();
                }
            });
        }

        /// <summary>
        /// Use swagger in application
        /// </summary>
        /// <param name="app"></param>
        /// <param name="config"></param>
        /// <param name="info"></param>
        public static void UseSwaggerEx(this IApplicationBuilder app,
            ISwaggerConfig config, Info info)
        {

            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            // Enable swagger and swagger ui
            app.UseSwagger(options =>
            {
                options.PreSerializeFilters.Add((doc, request) =>
                {
                    if (request.Headers.TryGetValue(HttpHeader.Location,
                            out var values) && values.Count > 0)
                    {
                        doc.BasePath = "/" + values[0];
                    }
                    doc.Schemes = new List<string> { "https" };
                    // TODO
                    //if (config.WithHttpScheme)
                    {
                        doc.Schemes.Add("http");
                    }
                });
                options.RouteTemplate = "{documentName}/swagger.json";
            });
            if (!config.UIEnabled)
            {
                return;
            }
            app.UseSwaggerUI(options =>
            {
                if (config.WithAuth)
                {
                    options.OAuthAppName(info.Title);
                    options.OAuthClientId(config.SwaggerAppId);
                    if (!string.IsNullOrEmpty(config.SwaggerAppSecret))
                    {
                        options.OAuthClientSecret(config.SwaggerAppSecret);
                    }
                    var resource = config as IAuthConfig;
                    if (!string.IsNullOrEmpty(resource?.Audience))
                    {
                        options.OAuthAdditionalQueryStringParams(
                            new Dictionary<string, string>
                            {
                                ["resource"] = resource.Audience
                            });
                    }
                }
                options.RoutePrefix = "";
                options.SwaggerEndpoint("v1/swagger.json", info.Version);
            });
        }

        /// <summary>
        /// Add extensions for autorest to schemas
        /// </summary>
        private class AutoRestSchemaExtensions : ISchemaFilter, IParameterFilter
        {

            /// <inheritdoc/>
            public void Apply(Schema model, SchemaFilterContext context)
            {
                AddExtension(context.SystemType, model.Extensions);
            }

            /// <inheritdoc/>
            public void Apply(IParameter parameter, ParameterFilterContext context)
            {
                AddExtension(context.ParameterInfo.ParameterType, parameter.Extensions);
            }

            /// <summary>
            /// Add enum extension
            /// </summary>
            /// <param name="paramType"></param>
            /// <param name="extensions"></param>
            /// <returns></returns>
            private static void AddExtension(Type paramType,
                Dictionary<string, object> extensions)
            {
                if (paramType.IsGenericType &&
                    paramType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    // Most of the model enums are nullable
                    paramType = paramType.GetGenericArguments()[0];
                }
                if (paramType.IsEnum)
                {
                    extensions.Add("x-ms-enum", new
                    {
                        name = paramType.Name,
                        modelAsString = false
                    });
                }
            }
        }


        /// <summary>
        /// Add autorest operation extensions
        /// </summary>
        private class AutoRestOperationExtensions : IOperationFilter
        {

            /// <inheritdoc/>
            public virtual void Apply(Operation operation, OperationFilterContext context)
            {
                var name = context.MethodInfo.Name;
                if (name.EndsWith("Async", StringComparison.InvariantCultureIgnoreCase))
                {
                    var autoOperationId = name.Substring(0, name.Length - 5);
                    if (autoOperationId.Length < operation.OperationId.Length)
                    {
                        operation.OperationId = autoOperationId;
                    }
                }
                if (operation.OperationId.Contains("CreateOrUpdate") &&
                    context.ApiDescription.HttpMethod.EqualsIgnoreCase("PATCH"))
                {
                    operation.OperationId = operation.OperationId.Replace("CreateOrUpdate", "Update");
                }
                var attribute = context.MethodInfo
                    .GetCustomAttributes<AutoRestExtensionAttribute>().FirstOrDefault();
                if (attribute != null)
                {
                    if (attribute.LongRunning)
                    {
                        operation.Extensions.Add("x-ms-long-running-operation", true);
                    }
                    if (!string.IsNullOrEmpty(attribute.NextPageLinkName))
                    {
                        operation.Extensions.Add("x-ms-pageable",
                            new Dictionary<string, string> {
                                { "nextLinkName", attribute.NextPageLinkName }
                            });
                    }
                    if (attribute.ResponseTypeIsFileStream)
                    {
                        operation.Responses = operation.Responses ??
                            new Dictionary<string, Response>();
                        operation.Responses.AddOrUpdate(HttpStatusCode.OK.ToString(),
                            new Response
                            {
                                Description = "OK",
                                Schema = new Schema
                                {
                                    Type = "file"
                                }
                            });
                    }
                }
            }
        }

        /// <summary>
        /// Gather security operations
        /// </summary>
        private class SecurityRequirementsOperationFilter : AutoRestOperationExtensions
        {

            /// <summary>
            /// Create filter using injected and configured authorization options
            /// </summary>
            /// <param name="options"></param>
            public SecurityRequirementsOperationFilter(IOptions<AuthorizationOptions> options)
            {
                _options = options;
            }

            /// <inheritdoc/>
            public override void Apply(Operation operation, OperationFilterContext context)
            {
                base.Apply(operation, context);
                var descriptor = context.ApiDescription.ActionDescriptor as
                    ControllerActionDescriptor;
                var claims = descriptor.GetRequiredPolicyGlaims(_options.Value);
                if (claims.Any())
                {
                    // responses cause csharp api do not throw exception on error
                    //operation.Responses.Add("401",
                    //    new Response { Description = "Unauthorized" });
                    //operation.Responses.Add("403",
                    //    new Response { Description = "Forbidden" });

                    // Add security description
                    operation.Security = new List<IDictionary<string, IEnumerable<string>>> {
                        new Dictionary<string, IEnumerable<string>> {
                            { "oauth2", claims }
                        }
                    };
                }
            }

            private readonly IOptions<AuthorizationOptions> _options;
        }
    }
}

