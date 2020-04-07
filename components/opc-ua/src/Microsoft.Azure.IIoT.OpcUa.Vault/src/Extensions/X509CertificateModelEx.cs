// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Crypto;
    using Microsoft.Azure.IIoT.Crypto.Models;
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
                Subject = certificate.Subject.Name,
                SelfSigned = certificate.IsSelfSigned() ? true : (bool?)null
            };
        }

        /// <summary>
        /// Convert to framework model
        /// </summary>
        /// <returns></returns>
        public static Certificate ToStackModel(this X509CertificateModel model) {
            return CertificateEx.Create(model.Certificate);
        }
    }
}
