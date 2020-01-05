// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Security.Cryptography.X509Certificates {
    using Microsoft.Azure.IIoT.Crypto.BouncyCastle;
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Certificate request
    /// </summary>
    public static class CertificationRequestEx {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static CertificationRequest ToCertificationRequest(
            this CertificateRequest request) {
            return new CertificationRequest {
                Subject = request.SubjectName,
                Extensions = request.CertificateExtensions.ToList(),
                PublicKey = request.GetPublicKey(),
                RawData = request.CreateSigningRequest()
            };
        }

        /// <summary>
        /// Convert to .net type
        /// </summary>
        /// <param name="request"></param>
        /// <param name="signatureType"></param>
        /// <returns></returns>
        public static CertificateRequest ToCertificateRequest(this CertificationRequest request,
            SignatureType signatureType = SignatureType.RS256) {
            return request.PublicKey.CreateCertificateRequest(request.Subject, signatureType);
        }

        /// <summary>
        /// Convert buffer to certificate request
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static CertificationRequest ToCertificationRequest(this byte[] buffer) {
            var csr = buffer.ToCertificationRequestInfo();
            var key = csr.GetPublicKey();
            var extensions = new List<X509Extension>();
            foreach (var extension in csr.GetX509Extensions().ToX509Extensions()) {
                extensions.Add(extension);
            }
            return new CertificationRequest {
                RawData = buffer,
                PublicKey = key,
                Extensions = extensions,
                Subject = X500DistinguishedNameEx.Create(csr.Subject.ToString())
            };
        }
    }
}
