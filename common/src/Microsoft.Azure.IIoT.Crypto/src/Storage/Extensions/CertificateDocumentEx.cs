// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Storage.Models {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;
    using System.Linq;

    /// <summary>
    /// Certificate document extensions
    /// </summary>
    public static class CertificateDocumentEx {

        /// <summary>
        /// Clone document
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static CertificateDocument Clone(this CertificateDocument document) {
            return new CertificateDocument {
                DisabledSince = document.DisabledSince,
                Issuer = document.Issuer,
                NotAfter = document.NotAfter,
                NotBefore = document.NotBefore,
                RawData = document.RawData.ToArray(),
                Subject = document.Subject,
                SerialNumber = document.SerialNumber,
                Thumbprint = document.Thumbprint,
                KeyId = document.KeyId,
                Version = document.Version,
                CertificateId = document.CertificateId,
                IsserPolicies = document.IsserPolicies.Clone(),
                CertificateName = document.CertificateName,
                KeyHandle = document.KeyHandle,
                IsIssuer = document.IsIssuer,
                IssuerAltNames = document.IssuerAltNames,
                IssuerKeyId = document.IssuerKeyId,
                IssuerSerialNumber = document.IssuerSerialNumber,
                SubjectAltNames = document.SubjectAltNames
            };
        }

        /// <summary>
        /// Create document
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cert"></param>
        /// <param name="certificateName"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static CertificateDocument ToDocument(this Certificate cert,
            string certificateName, string id, IKeyHandleSerializer serializer) {

            var ski = cert.GetSubjectKeyIdentifierExtension();
            var aki = cert.GetAuthorityKeyIdentifierExtension();
            var san = cert.GetSubjectAltNameExtension();

            return new CertificateDocument {
                Issuer = cert.Issuer.Name,
                IssuerAltNames = aki?.AuthorityNames,
                IssuerKeyId = aki?.KeyId,
                IssuerSerialNumber = aki?.SerialNumber.ToString(),
                IsIssuer = cert.IsIssuer(),
                IsserPolicies = cert.IssuerPolicies,
                Subject = cert.Subject.Name,
                KeyId = ski?.SubjectKeyIdentifier,
                SubjectAltNames = san?.DomainNames
                    .Concat(san.IPAddresses)
                    .ToList(),
                NotAfter = cert.NotAfterUtc,
                NotBefore = cert.NotBeforeUtc,
                RawData = cert.RawData,
                SerialNumber = cert.GetSerialNumberAsString(),
                Thumbprint = cert.Thumbprint,
                DisabledSince = cert.Revoked?.Date,
                Version = DateTime.UtcNow.ToFileTimeUtc(),
                CertificateId = id ?? certificateName,
                CertificateName = certificateName,
                KeyHandle = cert.KeyHandle == null ? null :
                    serializer.SerializeHandle(cert.KeyHandle)
            };
        }
    }
}

