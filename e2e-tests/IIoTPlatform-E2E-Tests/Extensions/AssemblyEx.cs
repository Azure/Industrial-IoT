// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using System.Reflection;
    using System.Collections.Generic;

    /// <summary>
    /// Assembly type extensions
    /// </summary>
    public static class AssemblyEx {

        /// <summary>
        /// Get assembly version
        /// </summary>
        public static Version GetReleaseVersion(this Assembly assembly) {
            if (assembly == null) {
                throw new ArgumentNullException(nameof(assembly));
            }
            var ver = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            if (ver == null || !Version.TryParse(ver, out var assemblyVersion)) {
                throw new KeyNotFoundException("Version attribute not found");
            }
            return assemblyVersion;
        }
    }
}
