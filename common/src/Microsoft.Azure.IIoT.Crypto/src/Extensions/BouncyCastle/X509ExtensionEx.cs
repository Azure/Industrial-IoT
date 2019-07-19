// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.BouncyCastle {
    using Org.BouncyCastle.Asn1;
    using Org.BouncyCastle.Asn1.X509;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using X509Extension = System.Security.Cryptography.X509Certificates.X509Extension;

    /// <summary>
    /// X509 extension extensions
    /// </summary>
    internal static class X509ExtensionEx {

        /// <summary>
        /// Convert to x509 extensions
        /// </summary>
        /// <param name="extensions"></param>
        /// <returns></returns>
        internal static IEnumerable<X509Extension> ToX509Extensions(
            this X509Extensions extensions) {
            if (extensions == null) {
                return Enumerable.Empty<X509Extension>();
            }
            var result = new List<X509Extension>();
            foreach (var oidObject in extensions.ExtensionOids) {
                var oid = (DerObjectIdentifier)oidObject;
                var extension = extensions.GetExtension(oid);

                result.Add(new Oid(oid.Id).CreateX509Extension(
                    extension.Value.GetDerEncoded(), extension.IsCritical));
            }
            return result;
        }
    }
}