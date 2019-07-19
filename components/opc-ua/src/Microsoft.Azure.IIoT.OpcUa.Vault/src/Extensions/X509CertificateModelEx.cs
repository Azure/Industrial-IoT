// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.Crypto;
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Newtonsoft.Json.Linq;
    using Opc.Ua;
    using System;
    using System.Linq;

    /// <summary>
    /// X509 cert extensions
    /// </summary>
    public static class X509CertificateModelEx {

        /// <summary>
        /// Get file name or return default
        /// </summary>
        /// <param name="cert"></param>
        /// <param name="defaultName"></param>
        /// <returns></returns>
        public static string GetFileNameOrDefault(this X509CertificateModel cert,
            string defaultName) {
            try {
                var dn = Utils.ParseDistinguishedName(cert.Subject);
                var prefix = dn
                    .FirstOrDefault(x => x.StartsWith("CN=",
                    StringComparison.OrdinalIgnoreCase)).Substring(3);
                return prefix + " [" + cert.Thumbprint + "]";
            }
            catch {
                return defaultName;
            }
        }

        /// <summary>
        /// Create certificate from cert
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="withCertificate"></param>
        public static X509CertificateModel ToServiceModel(this Certificate certificate,
            bool withCertificate = true) {
            if (certificate == null) {
                throw new ArgumentNullException(nameof(certificate));
            }
            if (certificate.RawData == null) {
                throw new ArgumentNullException(nameof(certificate.RawData));
            }
            return new X509CertificateModel {
                Certificate = withCertificate ? certificate.RawData : null,
                Thumbprint = certificate.Thumbprint,
                SerialNumber = certificate.GetSerialNumberAsString(),
                NotBeforeUtc = certificate.NotBeforeUtc,
                NotAfterUtc = certificate.NotAfterUtc,
                Subject = certificate.Subject.Name
            };
        }

        /// <summary>
        /// Convert to framework model
        /// </summary>
        /// <returns></returns>
        public static Certificate ToStackModel(this X509CertificateModel model) {
            return CertificateEx.Create(model.ToRawData());
        }

        /// <summary>
        /// Get Raw data
        /// </summary>
        /// <returns></returns>
        public static byte[] ToRawData(this X509CertificateModel model) {
            const string certPemHeader = "-----BEGIN CERTIFICATE-----";
            const string certPemFooter = "-----END CERTIFICATE-----";
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            if (model.Certificate == null) {
                throw new ArgumentNullException(nameof(model.Certificate));
            }
            switch (model.Certificate.Type) {
                case JTokenType.Bytes:
                    return (byte[])model.Certificate;
                case JTokenType.String:
                    var request = (string)model.Certificate;
                    if (request.Contains(certPemHeader,
                        StringComparison.OrdinalIgnoreCase)) {
                        var strippedCertificateRequest = request.Replace(
                            certPemHeader, "", StringComparison.OrdinalIgnoreCase);
                        strippedCertificateRequest = strippedCertificateRequest.Replace(
                            certPemFooter, "", StringComparison.OrdinalIgnoreCase);
                        return Convert.FromBase64String(strippedCertificateRequest);
                    }
                    return Convert.FromBase64String(request);
                default:
                    throw new ArgumentException(
                        "Bad certificate data", nameof(model.Certificate));
            }
        }
    }
}
