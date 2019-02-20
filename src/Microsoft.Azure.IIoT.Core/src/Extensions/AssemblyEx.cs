// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using IO;
    using Reflection;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Assembly type extensions
    /// </summary>
    public static class AssemblyEx {

        /// <summary>
        /// Read embedded resource as buffer
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="resourceId"></param>
        /// <returns></returns>
        public static ArraySegment<byte> GetManifestResource(this Assembly assembly,
            string resourceId) {
            if (assembly == null) {
                throw new ArgumentNullException(nameof(assembly));
            }
            if (string.IsNullOrEmpty(resourceId)) {
                throw new ArgumentNullException(nameof(resourceId));
            }
            var resource = $"{assembly.GetName().Name}.{resourceId}";
            using (var stream = assembly.GetManifestResourceStream(resource)) {
                return stream.ReadAsBuffer();
            }
        }

        /// <summary>
        /// Read all embedded resources as resource id and buffer tuples.
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<string, ArraySegment<byte>>> GetManifestResources(
            this Assembly assembly, string extension = null) => assembly?
            .GetManifestResourceNames()
            .Where(r => extension == null ||
                r.EndsWith(extension, StringComparison.Ordinal))
            .Select(r => {
                using (var stream = assembly.GetManifestResourceStream(r)) {
                    if (stream == null) {
                        throw new FileNotFoundException(r + " not found");
                    }
                    return Tuple.Create(r.Replace($"{assembly.GetName().Name}.", ""),
                        stream.ReadAsBuffer());
                }
            });

        /// <summary>
        /// Get assembly version
        /// </summary>
        public static Version GetFileVersion(this Assembly assembly) {
            if (assembly == null) {
                throw new ArgumentNullException(nameof(assembly));
            }
            var ver = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            if (ver == null || !Version.TryParse(ver, out var assemblyVersion)) {
                throw new KeyNotFoundException("Version attribute not found");
            }
            return assemblyVersion;
        }


        /// <summary>
        /// Loads from resource
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="resourceId"></param>
        /// <returns></returns>
        public static T DeserializeFromXmlManifestResource<T>(this Assembly assembly,
            string resourceId) {
            return assembly.GetManifestResourceStream(resourceId).DeserializeFromXml<T>();
        }
    }
}
