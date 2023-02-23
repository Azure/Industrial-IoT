// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System
{
    using Newtonsoft.Json.Linq;
    using System.Linq;
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

        /// <summary>
        /// Get assembly info version
        /// </summary>
        public static JObject GetVersionInfoObject(this Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            var o = new JObject();
            foreach (var p in assembly.GetType("ThisAssembly")?
                .GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                .Select(f => new JProperty(f.Name, f.GetValue(null))) ??
                    new JProperty("AssemblyVersion", "No version information found.")
                        .YieldReturn())
            {
                o.Add(p);
            }
            return o;
        }
    }
}
