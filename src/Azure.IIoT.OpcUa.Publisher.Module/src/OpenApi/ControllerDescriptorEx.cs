// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AspNetCore.Mvc.Controllers
{
    using Asp.Versioning;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Controller descriptor extensions
    /// </summary>
    internal static class ControllerDescriptorEx
    {
        /// <summary>
        /// Retrieve versions from descriptor
        /// </summary>
        /// <param name="descriptor"></param>
        public static IEnumerable<string> GetApiVersions(
            this ControllerActionDescriptor descriptor)
        {
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
        public static bool MatchesVersion(this ControllerActionDescriptor descriptor,
            string version)
        {
            var versions = descriptor.GetApiVersions();
            var maps = descriptor.MethodInfo.GetCustomAttributes(false)
                .OfType<MapToApiVersionAttribute>()
                .SelectMany(attr => attr.Versions
                    .Select(v => v.ToString()))
                .ToArray();
            return versions.Any(v => $"v{v}" == version) &&
                (maps.Length == 0 || maps.Any(v => $"v{v}" == version));
        }
    }
}
