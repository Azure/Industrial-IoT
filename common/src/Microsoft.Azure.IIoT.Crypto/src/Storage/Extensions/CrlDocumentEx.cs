// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Storage.Models {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Crl document extensions
    /// </summary>
    public static class CrlDocumentEx {

        /// <summary>
        /// Clone document
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static Crl ToModel(this CrlDocument document) {
            if (document?.RawData == null) {
                return null;
            }
            return CrlEx.ToCrl(document.RawData);
        }

        /// <summary>
        /// Create document
        /// </summary>
        /// <param name="crl"></param>
        /// <param name="certificateSerial"></param>
        /// <param name="issuerSerial"></param>
        /// <param name="defaultTtl"></param>
        /// <returns></returns>
        public static CrlDocument ToDocument(this Crl crl, string certificateSerial,
            string issuerSerial, TimeSpan? defaultTtl = null) {

            if (crl.ThisUpdate == DateTime.MinValue ||
                crl.ThisUpdate == DateTime.MaxValue) {
                throw new ArgumentException(nameof(crl));
            }

            var ttl = (defaultTtl ?? TimeSpan.FromMinutes(5)).TotalSeconds;
            if (crl.NextUpdate != null &&
                crl.ThisUpdate < crl.NextUpdate) {
                ttl = (crl.ThisUpdate - crl.NextUpdate.Value).TotalSeconds;
            }

            return new CrlDocument {
                CertificateSerialNumber = certificateSerial,
                SerialNumber = new SerialNumber(crl.SerialNumber).ToString(),
                IssuerSerialNumber = issuerSerial,
                ThisUpdate = crl.ThisUpdate,
                NextUpdate = crl.NextUpdate,
                Ttl = (int)ttl,
                RawData = crl.RawData
            };
        }
    }
}

