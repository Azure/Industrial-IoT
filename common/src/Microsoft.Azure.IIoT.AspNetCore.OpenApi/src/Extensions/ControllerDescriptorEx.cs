// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AspNetCore.Mvc.Controllers {
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Authorization.Infrastructure;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;

    /// <summary>
    /// Controller descriptor extensions
    /// </summary>
    public static class ControllerDescriptorEx {

        /// <summary>
        /// Retrieve claims from descriptor
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetRequiredPolicyGlaims(
            this ControllerActionDescriptor descriptor, AuthorizationOptions options) {
            var attributes = descriptor.GetAttributes<AuthorizeAttribute>(false);
            var requirements = attributes
                .Select(attr => attr.Policy)
                .Select(options.GetPolicy)
                .Where(x => x != null)
                .SelectMany(x => x.Requirements)
                .Distinct();
            var claims = requirements.OfType<ClaimsAuthorizationRequirement>()
                .Select(x => x.ClaimType);
            var roles = requirements.OfType<RolesAuthorizationRequirement>()
                .SelectMany(x => x.AllowedRoles)
                .Concat(attributes.Where(a => a.Roles != null).Select(a => a.Roles));
            if (roles.Any()) {
                claims = claims.Append(ClaimTypes.Role);
            }
            if (requirements.OfType<DenyAnonymousAuthorizationRequirement>().Any()) {
                claims = claims.Append(ClaimTypes.Authentication);
            }
            return claims.Distinct();
        }

        /// <summary>
        /// Retrieve versions from descriptor
        /// </summary>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetApiVersions(
            this ControllerActionDescriptor descriptor) {
            var attributes = descriptor.ControllerTypeInfo.GetCustomAttributes(false)
                .OfType<ApiVersionAttribute>();
            return attributes
                .SelectMany(attr => attr.Versions
                    .Select(v => v.ToString()))
                .Distinct();
        }

        /// <summary>
        /// Matches version string
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public static bool MatchesVersion(this ControllerActionDescriptor descriptor,
            string version) {
            var versions = descriptor.GetApiVersions();
            var maps = descriptor.MethodInfo.GetCustomAttributes(false)
                .OfType<MapToApiVersionAttribute>()
                .SelectMany(attr => attr.Versions
                    .Select(v => v.ToString()))
                .ToArray();
            return versions.Any(v => $"v{v}" == version) &&
                (!maps.Any() || maps.Any(v => $"v{v}" == version));
        }

        /// <summary>
        /// Rerturn controller and action attributes sorted
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetAttributes<T>(
            this ControllerActionDescriptor descriptor, bool inherit) {
            var controllerAttributes = descriptor.ControllerTypeInfo
                .GetCustomAttributes(inherit);
            var actionAttributes = descriptor.MethodInfo
                .GetCustomAttributes(inherit);
            return controllerAttributes
                .Append(actionAttributes)
                .OfType<T>();
        }
    }
}
