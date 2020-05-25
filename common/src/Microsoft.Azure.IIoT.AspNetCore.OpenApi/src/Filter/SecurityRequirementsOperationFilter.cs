// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.OpenApi {
    using Swashbuckle.AspNetCore.SwaggerGen;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.Extensions.Options;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.OpenApi.Models;

    /// <summary>
    /// Gather security operations
    /// </summary>
    internal class SecurityRequirementsOperationFilter : AutoRestOperationExtensions {

        /// <summary>
        /// Create filter using injected and configured authorization options
        /// </summary>
        /// <param name="options"></param>
        public SecurityRequirementsOperationFilter(IOptions<AuthorizationOptions> options) {
            _options = options;
        }

        /// <inheritdoc/>
        public override void Apply(OpenApiOperation operation, OperationFilterContext context) {
            base.Apply(operation, context);
            var descriptor = context.ApiDescription.ActionDescriptor as
                ControllerActionDescriptor;
            var claims = descriptor.GetRequiredPolicyGlaims(_options.Value);
            if (claims.Any()) {
                var oAuthScheme = new OpenApiSecurityScheme {
                    Reference = new OpenApiReference {
                        Type = ReferenceType.SecurityScheme,
                        Id = "oauth2"
                    }
                };

                // TODO: Investigate
                // responses cause csharp api do not throw exception on error
                // operation.Responses.Add("401", new OpenApiResponse {
                //     Description = "Unauthorized" });
                // operation.Responses.Add("403", new OpenApiResponse {
                //     Description = "Forbidden (you are not allowed to access this resource)" });
                // operation.Responses.Add("429", new OpenApiResponse {
                //     Description = "Too many requests" });
                // operation.Responses.Add("500", new OpenApiResponse {
                //     Description = "Unknown server error occured" });

                // Add security description
                operation.Security = new List<OpenApiSecurityRequirement> {
                    new OpenApiSecurityRequirement {
                        [ oAuthScheme ] = claims.ToList()
                    }
                };
            }
        }

        private readonly IOptions<AuthorizationOptions> _options;
    }
}

