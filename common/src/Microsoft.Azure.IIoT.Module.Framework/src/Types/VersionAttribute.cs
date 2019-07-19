// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework {
    using System;

    /// <summary>
    /// Attribute to version a controller implementation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class VersionAttribute : Attribute {

        /// <summary>
        /// Create versioning attribute
        /// </summary>
        /// <param name="major"></param>
        public VersionAttribute(int major) {
            Set(major, null, null);
        }

        /// <summary>
        /// Create versioning attribute
        /// </summary>
        /// <param name="major"></param>
        /// <param name="minor"></param>
        public VersionAttribute(int major, int minor) {
            Set(major, minor, null);
        }

        /// <summary>
        /// Create versioning attribute
        /// </summary>
        /// <param name="major"></param>
        /// <param name="minor"></param>
        /// <param name="revision"></param>
        public VersionAttribute(int major, int minor, int revision) {
            Set(major, minor, revision);
        }

        /// <summary>
        /// Create versioning attribute
        /// </summary>
        /// <param name="version"></param>
        public VersionAttribute(string version) {
            Value = version;
        }

        /// <summary>
        /// Return string version
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Return numeric version
        /// </summary>
        public ulong Numeric { get; private set; }

        /// <summary>
        /// Version string
        /// </summary>
        /// <returns></returns>
        private void Set(int major, int? minor, int? revision) {
            var version = major.ToString();
            var numeric = (uint)major << 32;
            if (minor != null) {
                version += "." + minor.ToString();
                numeric |= (uint)minor << 16;
                if (revision != null) {
                    version += "." + revision.ToString();
                    numeric |= (uint)revision;
                }
            }
            Value = version;
            Numeric = numeric;
        }
    }
}
