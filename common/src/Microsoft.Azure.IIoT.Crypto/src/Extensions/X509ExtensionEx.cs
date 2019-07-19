// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Security.Cryptography.X509Certificates {
    using Microsoft.Azure.IIoT.Crypto.Models;

    /// <summary>
    /// Extension factory
    /// </summary>
    public static class X509ExtensionEx {

        /// <summary>
        /// Create typed extension based on oid
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="rawData"></param>
        /// <param name="critical"></param>
        /// <returns></returns>
        public static X509Extension CreateX509Extension(this Oid oid,
            byte[] rawData, bool critical = false) {

            switch (oid.Value) {
                case Oids.AuthorityInformationAccess:
                    return new X509AuthorityInformationAccessExtension(
                        rawData, critical);
                case Oids.CrlDistributionPoints:
                    return new X509CrlDistributionPointsExtension(
                        rawData, critical);
                case Oids.AuthorityKeyIdentifier:
                case Oids.AuthorityKeyIdentifier2:
                    return new X509AuthorityKeyIdentifierExtension(
                        rawData, critical);
                case Oids.SubjectAltName:
                case Oids.SubjectAltName2:
                    return new X509SubjectAltNameExtension(
                        rawData, critical);
                case Oids.BasicConstraints:
                case Oids.BasicConstraints2:
                    return new X509BasicConstraintsExtension(
                        new AsnEncodedData(oid, rawData), critical);
                case Oids.KeyUsage:
                    return new X509KeyUsageExtension(
                        new AsnEncodedData(oid, rawData), critical);
                case Oids.EnhancedKeyUsage:
                    return new X509EnhancedKeyUsageExtension(
                        new AsnEncodedData(oid, rawData), critical);
                case Oids.SubjectKeyIdentifier:
                    return new X509SubjectKeyIdentifierExtension(
                        new AsnEncodedData(oid, rawData), critical);
                default:
                    return new X509Extension(oid, rawData, critical);
            }
        }
    }
}
