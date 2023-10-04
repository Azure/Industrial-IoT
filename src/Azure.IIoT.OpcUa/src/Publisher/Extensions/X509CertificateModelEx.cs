// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Certificate extension
    /// </summary>
    public static class X509CertificateModelEx
    {
        /// <summary>
        /// To service model
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        public static X509CertificateModel ToServiceModel(this X509Certificate2 cert)
        {
            return new X509CertificateModel
            {
                Pfx = cert.Export(X509ContentType.Pfx),
                NotAfterUtc = cert.NotAfter,
                NotBeforeUtc = cert.NotBefore,
                SerialNumber = cert.GetSerialNumberString(),
                Subject = cert.Subject,
                HasPrivateKey = cert.HasPrivateKey,
                Thumbprint = cert.Thumbprint,
                SelfSigned = IsSelfIssued(cert) ? true : null
            };
        }

        /// <summary>
        /// Test self issued - no validation is done on signature.
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        private static bool IsSelfIssued(this X509Certificate2 cert)
        {
            return cert.IssuerName.RawData
                .SequenceEqualsSafe(cert.SubjectName.RawData);
        }
    }
}
