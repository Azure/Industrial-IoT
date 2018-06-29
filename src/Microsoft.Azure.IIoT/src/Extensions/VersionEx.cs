// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using Reflection;

    public static class VersionEx {
        /// <summary>
        /// Get assembly version
        /// </summary>
        public static Version Assembly {
            get {
                if (_assemblyVersion == null) {
                    var assembly = typeof(VersionEx).GetTypeInfo().Assembly;
                    var ver = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
                    if (ver == null || !Version.TryParse(ver, out _assemblyVersion)) {
                        _assemblyVersion = new Version(0, 0, 0, 0);
                    }
                }
                return _assemblyVersion;
            }
        }

        private static Version _assemblyVersion;

        /// <summary>
        /// Compresses the version into a 32 bit integer
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public static uint ToUInt(this Version version) {
            return
                (uint)((byte)version.Major << 24) |
                (uint)((byte)version.Minor << 16) |
                (uint)((byte)version.Build << 8) |
                       (byte)version.Revision;
        }
    }
}
