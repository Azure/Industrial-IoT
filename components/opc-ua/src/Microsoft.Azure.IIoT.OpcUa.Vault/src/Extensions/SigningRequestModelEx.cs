// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using System;

    /// <summary>
    /// Signing request
    /// </summary>
    public static class SigningRequestModelEx {

        /// <summary>
        /// Convert to raw request
        /// </summary>
        /// <returns></returns>
        public static byte[] ToRawData(this StartSigningRequestModel model) {
            const string certRequestPemHeader = "-----BEGIN CERTIFICATE REQUEST-----";
            const string certRequestPemFooter = "-----END CERTIFICATE REQUEST-----";
            if (model.CertificateRequest == null) {
                throw new ArgumentNullException(nameof(model.CertificateRequest));
            }
            if (model.CertificateRequest.IsBytes) {
                return (byte[])model.CertificateRequest;
            }
            if (model.CertificateRequest.IsString) {
                var request = (string)model.CertificateRequest;
                if (request.Contains(certRequestPemHeader,
                    StringComparison.OrdinalIgnoreCase)) {
                    var strippedCertificateRequest = request.Replace(
                        certRequestPemHeader, "", StringComparison.OrdinalIgnoreCase);
                    strippedCertificateRequest = strippedCertificateRequest.Replace(
                        certRequestPemFooter, "", StringComparison.OrdinalIgnoreCase);
                    return Convert.FromBase64String(strippedCertificateRequest);
                }
                return Convert.FromBase64String(request);
            }
            throw new ArgumentException("Bad certificate request",
                nameof(model.CertificateRequest));
        }
    }
}
