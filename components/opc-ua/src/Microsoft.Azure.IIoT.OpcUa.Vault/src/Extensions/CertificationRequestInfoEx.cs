// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Security.Cryptography.X509Certificates {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Org.BouncyCastle.Asn1;
    using Org.BouncyCastle.Asn1.Pkcs;
    using Org.BouncyCastle.Asn1.X509;

    /// <summary>
    /// Certificate request info extensions
    /// </summary>
    public static class CertificationRequestInfoEx {

        /// <summary>
        /// Get alt name extension from info
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static X509SubjectAltNameExtension GetSubjectAltNameExtension(
            this CertificationRequestInfo info) {
            try {
                foreach (Asn1Encodable attribute in info.Attributes) {
                    var sequence = Asn1Sequence.GetInstance(attribute.ToAsn1Object());
                    var oid = DerObjectIdentifier.GetInstance(sequence[0].ToAsn1Object());
                    if (oid.Equals(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest)) {
                        var extensionInstance = Asn1Set.GetInstance(sequence[1]);
                        var extensionSequence = Asn1Sequence.GetInstance(extensionInstance[0]);
                        var extensions = X509Extensions.GetInstance(extensionSequence);
                        var extension = extensions.GetExtension(X509Extensions.SubjectAlternativeName);
                        var asnEncodedAltNameExtension = new AsnEncodedData(
                            X509Extensions.SubjectAlternativeName.ToString(),
                            extension.Value.GetOctets());
                        var altNameExtension = new X509SubjectAltNameExtension(
                            asnEncodedAltNameExtension, extension.IsCritical);
                        return altNameExtension;
                    }
                }
            }
            catch (Exception ex) {
                throw new ArgumentException("CSR altNameExtension invalid.", ex);
            }
            return null;
        }
    }
}
