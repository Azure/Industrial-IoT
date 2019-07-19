// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.BouncyCastle {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Org.BouncyCastle.Asn1;
    using Org.BouncyCastle.Asn1.X509;
    using Org.BouncyCastle.Math;
    using Org.BouncyCastle.X509;
    using Org.BouncyCastle.X509.Extension;

    /// <summary>
    /// Crl extensions
    /// </summary>
    internal static class X509CrlEx {

        /// <summary>
        /// Convert to bouncy castle crl
        /// </summary>
        /// <param name="crl"></param>
        /// <returns></returns>
        internal static X509Crl ToX509Crl(this Crl crl) {
            return ToX509Crl(crl.RawData);
        }

        /// <summary>
        /// Convert to bouncy castle crl
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        internal static X509Crl ToX509Crl(byte[] buffer) {
            return new X509CrlParser().ReadCrl(buffer);
        }

        /// <summary>
        /// Read the Crl number from a X509Crl.
        /// </summary>
        internal static byte[] GetCrlNumber(this X509Crl crl) {
            var crlNumber = BigInteger.One;
            var asn1Object = GetExtensionValue(crl, X509Extensions.CrlNumber);
            if (asn1Object != null) {
                crlNumber = DerInteger.GetInstance(asn1Object).PositiveValue;
            }
            return crlNumber?.ToByteArrayUnsigned();
        }

        /// <summary>
        /// Get the value of an extension oid.
        /// </summary>
        private static Asn1Object GetExtensionValue(
            IX509Extension extension, DerObjectIdentifier oid) {
            var asn1Octet = extension.GetExtensionValue(oid);
            if (asn1Octet != null) {
                return X509ExtensionUtilities.FromExtensionValue(asn1Octet);
            }
            return null;
        }
    }
}
