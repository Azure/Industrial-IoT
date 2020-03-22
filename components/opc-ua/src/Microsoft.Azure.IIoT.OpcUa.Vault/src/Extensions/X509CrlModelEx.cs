// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;

    /// <summary>
    /// A X509 certificate revocation list extensions
    /// </summary>
    public static class X509CrlModelEx {

        /// <summary>
        /// Create crl
        /// </summary>
        /// <param name="crl"></param>
        public static X509CrlModel ToServiceModel(this Crl crl) {
            return new X509CrlModel {
                Crl = crl.RawData,
                Issuer = crl.Issuer
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static Crl ToStackModel(this X509CrlModel model) {
            return CrlEx.ToCrl(model.ToRawData());
        }

        /// <summary>
        /// Get Raw data
        /// </summary>
        /// <returns></returns>
        public static byte[] ToRawData(this X509CrlModel model) {
            const string certPemHeader = "-----BEGIN X509 CRL-----";
            const string certPemFooter = "-----END X509 CRL-----";
            if (model.Crl == null) {
                throw new ArgumentNullException(nameof(model.Crl));
            }
            if (model.Crl.IsBytes) {
                return (byte[])model.Crl;
            }
            if (model.Crl.IsString) {
                var request = (string)model.Crl;
                if (request.Contains(certPemHeader,
                    StringComparison.OrdinalIgnoreCase)) {
                    var strippedCertificateRequest = request.Replace(
                        certPemHeader, "", StringComparison.OrdinalIgnoreCase);
                    strippedCertificateRequest = strippedCertificateRequest.Replace(
                        certPemFooter, "", StringComparison.OrdinalIgnoreCase);
                    return Convert.FromBase64String(strippedCertificateRequest);
                }
                return Convert.FromBase64String(request);
            }
            throw new ArgumentException("Bad crl data.", nameof(model.Crl));
        }
    }
}
