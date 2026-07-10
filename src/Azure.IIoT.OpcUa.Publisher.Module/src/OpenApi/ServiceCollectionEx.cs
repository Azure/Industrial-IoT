// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.OpenApi
{
    using Asp.Versioning;
    using global::Azure.IIoT.OpcUa.Publisher.Module.OpenApi;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Swashbuckle.AspNetCore.SwaggerGen;
    using System;
    using System.IO;

    /// <summary>
    /// Service collection extensions
    /// </summary>
    public static class ServiceCollectionEx
    {
        /// <summary>
        /// Configure OpenApi
        /// </summary>
        /// <param name="services"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        public static IServiceCollection AddSwagger(this IServiceCollection services,
            string title, string description)
        {
            return services
                .AddApiVersioning(o =>
                {
                    o.AssumeDefaultVersionWhenUnspecified = true;
                    o.ReportApiVersions = true;
                    o.DefaultApiVersion = new ApiVersion(1, 0);
                }).AddMvc().Services
                .AddSwaggerGen()
                .AddTransient<IPostConfigureOptions<OpenApiOptions>, OpenApiConfig>()
                .AddTransient<IConfigureOptions<SwaggerGenOptions>>(provider =>
                {
                    var api = provider.GetRequiredService<IActionDescriptorCollectionProvider>();
                    var config = provider.GetRequiredService<IOptions<OpenApiOptions>>();
                    var infos = api.GetOpenApiInfos(title, description, config.Value);

                    return new ConfigureNamedOptions<SwaggerGenOptions>(Options.DefaultName, options =>
                    {
                        // Add annotations
                        options.EnableAnnotations();

                        // Add autorest extensions
                        options.SchemaFilter<AutoRestSchemaExtensions>();
                        options.ParameterFilter<AutoRestSchemaExtensions>();
                        options.RequestBodyFilter<AutoRestSchemaExtensions>();
                        options.DocumentFilter<ApiVersionExtensions>();

                        // Ensure the routes are added to the right Swagger doc
                        options.DocInclusionPredicate((version, descriptor) =>
                        {
                            if (descriptor.ActionDescriptor is ControllerActionDescriptor desc)
                            {
                                return desc.MatchesVersion(version);
                            }
                            return true;
                        });

                        foreach (var info in infos)
                        {
                            // Generate doc for version
                            options.SwaggerDoc(info.Version, info);
                        }

                        // Add help
                        foreach (var file in Directory.GetFiles(AppContext.BaseDirectory, "*.xml"))
                        {
                            options.IncludeXmlComments(file, true);
                        }

                        options.OperationFilter<AutoRestOperationExtensions>();
                    });
                });
        }
    }
}
