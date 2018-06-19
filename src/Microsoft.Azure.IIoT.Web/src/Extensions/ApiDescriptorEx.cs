// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Swashbuckle.AspNetCore.Swagger {
    using Microsoft.Extensions.DependencyInjection;
    using System.Collections.Generic;
    using Swashbuckle.AspNetCore.SwaggerGen;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Options;
    using System.Linq;
    using Microsoft.AspNetCore.Authorization.Infrastructure;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Mvc.Controllers;

    public static class ApiDescriptorEx {

        /// <summary>
        /// Collect configured scopes from all controllers registered as services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetRequiredScopes(
            this IServiceCollection services) {
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>();
            return provider.GetRequiredService<IActionDescriptorCollectionProvider>()
                .ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Cast<ControllerActionDescriptor>()
                .SelectMany(d => d.GetRequiredPolicyGlaims(options.Value))
                .Distinct();
        }

        /// <summary>
        /// Retrieve claims from descriptor
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetRequiredPolicyGlaims(
            this ControllerActionDescriptor descriptor, AuthorizationOptions options) {
            var attributes = descriptor.GetControllerAndActionAttributes(false)
                .OfType<AuthorizeAttribute>();
            var requirements = attributes
                .Select(attr => attr.Policy)
                .Select(options.GetPolicy)
                .SelectMany(x => x.Requirements)
                .Distinct();
            var claims = requirements.OfType<ClaimsAuthorizationRequirement>()
                .Select(x => x.ClaimType);
            var roles = requirements.OfType<RolesAuthorizationRequirement>()
                .SelectMany(x => x.AllowedRoles)
                .Concat(attributes.Where(a => a.Roles != null).Select(a => a.Roles));
            if (roles.Any()) {
                if (descriptor.ActionName == "ListAsync") {
                    System.Console.WriteLine("TEST");
                }
                claims = claims.Append(ClaimTypes.Role);
            }
            if (requirements.OfType<DenyAnonymousAuthorizationRequirement>().Any()) {
                claims = claims.Append(ClaimTypes.Authentication);
            }
            return claims.Distinct();
        }
    }
}
