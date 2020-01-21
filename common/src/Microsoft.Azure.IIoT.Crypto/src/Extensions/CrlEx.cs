// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using Microsoft.Azure.IIoT.Crypto.BouncyCastle;
    using Microsoft.Azure.IIoT.Crypto.Utils;
    using System;

    /// <summary>
    /// Crl extensions
    /// </summary>
    public static class CrlEx {

        /// <summary>
        /// Create crl from memory buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Crl ToCrl(byte[] buffer) {
            var parsed = X509CrlEx.ToX509Crl(buffer);
            return new Crl {
                SerialNumber = parsed.GetCrlNumber(),
                Issuer = FixUpIssuer(parsed.IssuerDN.ToString()),
                ThisUpdate = parsed.ThisUpdate,
                NextUpdate = parsed.NextUpdate?.Value,
                RawData = buffer
            };
        }

        /// <summary>
        /// Verifies the signature on the CRL.
        /// </summary>
        /// <param name="crl"></param>
        /// <param name="issuer"></param>
        public static void Verify(this Crl crl, Certificate issuer) {
            crl.ToX509Crl().Verify(issuer.ToX509Certificate().GetPublicKey());
        }

        /// <summary>
        /// Verifies the signature on the CRL.
        /// </summary>
        /// <param name="crl"></param>
        /// <param name="issuer"></param>
        /// <returns></returns>
        public static bool HasValidSignature(this Crl crl, Certificate issuer) {
            try {
                Verify(crl, issuer);
                return true;
            }
            catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// Returns true the certificate is in the CRL.
        /// </summary>
        /// <param name="crl"></param>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static bool IsRevoked(this Crl crl, Certificate certificate) {
            return crl.ToX509Crl().IsRevoked(certificate.ToX509Certificate());
        }

        /// <summary>
        /// Helper to make issuer match System.Security conventions
        /// </summary>
        /// <param name="issuerDN"></param>
        /// <returns></returns>
        private static string FixUpIssuer(string issuerDN) {
            // replace state ST= with S=
            issuerDN = issuerDN.Replace("ST=", "S=");
            // reverse DN order
            var issuerList = CertUtils.ParseDistinguishedName(issuerDN);
            issuerList.Reverse();
            return string.Join(", ", issuerList);
        }
    }
}
