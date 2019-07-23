// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate store extensions
    /// </summary>
    public static class CertificateStoreEx {

        /// <summary>
        /// Get all issued certificates
        /// </summary>
        /// <param name="store"></param>
        /// <param name="issuerCert"></param>
        /// <param name="isIssuer"></param>
        /// <param name="disabled"></param>
        /// <param name="stillValid"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<Certificate>> GetIssuedCertificatesAsync(
            this ICertificateStore store, Certificate issuerCert, bool? isIssuer = null,
            bool disabled = false, bool stillValid = true, CancellationToken ct = default) {
            var ski = issuerCert.GetSubjectKeyIdentifierExtension();
            return await store.QueryAllCertificatesAsync(
                new CertificateFilter {
                    Issuer = issuerCert.Subject,
                    IssuerKeyId = ski.SubjectKeyIdentifier,
                    IssuerSerialNumber = issuerCert.SerialNumber,
                    NotAfter = stillValid ? DateTime.UtcNow : (DateTime?)null,
                    NotBefore = stillValid ? DateTime.UtcNow : (DateTime?)null,
                    IsIssuer = isIssuer,
                    IncludeDisabled = disabled,
                    ExcludeEnabled = disabled
                }, ct);
        }
    }
}

