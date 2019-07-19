// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Security.Cryptography.X509Certificates {
    using Microsoft.Azure.IIoT.Crypto.Utils;
    using System.Linq;

    /// <summary>
    /// Distinguished names extensions
    /// </summary>
    public static class X500DistinguishedNameEx {

        /// <summary>
        /// Compare names
        /// </summary>
        /// <param name="name"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool SameAs(this X500DistinguishedName name, X500DistinguishedName other) {
            var n1 = name?.Name;
            var n2 = other?.Name;
            return CertUtils.CompareDistinguishedName(n1, n2);
        }

        /// <summary>
        /// Sets the parameters to suitable defaults.
        /// </summary>
        public static X500DistinguishedName Create(string subjectName) {
            // parse the subject name if specified.
            if (!string.IsNullOrEmpty(subjectName)) {
                var subjectNameEntries = CertUtils.ParseDistinguishedName(subjectName)
                    .Select(e => e.Contains("=") ? e : "CN=" + e);
                // enforce proper formatting for the subject name string
                subjectName = string.Join(", ", subjectNameEntries);
            }
            return new X500DistinguishedName(subjectName);
        }
    }
}