// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System
{
    using System.Reflection;

    /// <summary>
    /// Assembly type extensions
    /// </summary>
    public static class AssemblyEx
    {
        /// <summary>
        /// Get assembly info version
        /// </summary>
        public static string GetInformationalVersion(this Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "";
        }
    }
}
