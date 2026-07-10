// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System
{
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Assembly type extensions.
    /// </summary>
    public static class AssemblyEx
    {
        /// <summary>
        /// Get assembly version.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is <c>null</c>.</exception>
        /// <exception cref="KeyNotFoundException"></exception>
        public static Version GetReleaseVersion(this Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            var version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            if (version == null || !Version.TryParse(version, out var assemblyVersion))
            {
                throw new KeyNotFoundException("Version attribute not found");
            }
            return assemblyVersion;
        }
    }
}
