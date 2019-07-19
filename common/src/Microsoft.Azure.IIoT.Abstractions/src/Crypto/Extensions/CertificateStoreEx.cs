// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate store extensions
    /// </summary>
    public static class CertificateStoreEx {

        /// <summary>
        /// Find most recent certificate with the given name
        /// from certificate store.
        /// </summary>
        /// <param name="store"></param>
        /// <param name="certificateName">Name of certificate
        /// </param>
        /// <param name="ct">CancellationToken</param>
        /// <returns></returns>
        public static async Task<Certificate> FindLatestCertificateAsync(
            this ICertificateStore store, string certificateName,
            CancellationToken ct = default) {
            try {
                return await store.GetLatestCertificateAsync(certificateName, ct);
            }
            catch (ResourceNotFoundException) {
                return null;
            }
        }

        /// <summary>
        /// Find certificate by serial number.
        /// </summary>
        /// <param name="store"></param>
        /// <param name="serialNumber">serial number of the
        /// certificate </param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<Certificate> FindCertificateAsync(
            this ICertificateStore store, byte[] serialNumber,
            CancellationToken ct = default) {
            try {
                return await store.GetCertificateAsync(serialNumber, ct);
            }
            catch (ResourceNotFoundException) {
                return null;
            }
        }

        /// <summary>
        /// Get certificate chain of trust.
        /// </summary>
        /// <param name="store"></param>
        /// <param name="certificate">certificate</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<Certificate>> ListCompleteCertificateChainAsync(
            this ICertificateStore store, Certificate certificate,
            CancellationToken ct = default) {
            var certificates = new List<Certificate>();
            var chain = await store.ListCertificateChainAsync(certificate, ct);
            if (chain != null) {
                certificates.AddRange(chain.Certificates);
            }
            while (chain?.ContinuationToken != null) {
                chain = await store.ListCertificatesAsync(
                    chain.ContinuationToken, null, ct);
                certificates.AddRange(chain.Certificates);
            }
            return certificates;
        }

        /// <summary>
        /// Get certificate chain of trust.
        /// </summary>
        /// <param name="store"></param>
        /// <param name="serialNumber">serial number of the
        /// certificate </param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<CertificateCollection> ListCertificateChainAsync(
            this ICertificateStore store, byte[] serialNumber,
            CancellationToken ct = default) {
            var cert = await store.GetCertificateAsync(serialNumber, ct);
            return await store.ListCertificateChainAsync(cert, ct);
        }

        /// <summary>
        /// Get certificate chain of trust.
        /// </summary>
        /// <param name="store"></param>
        /// <param name="serialNumber">serial number of the
        /// certificate </param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<Certificate>> ListCompleteCertificateChainAsync(
            this ICertificateStore store, byte[] serialNumber,
            CancellationToken ct = default) {
            var cert = await store.GetCertificateAsync(serialNumber, ct);
            return await store.ListCompleteCertificateChainAsync(cert, ct);
        }

        /// <summary>
        /// Query all certificates
        /// </summary>
        /// <param name="store"></param>
        /// <param name="filter"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<Certificate>> QueryAllCertificatesAsync(
            this ICertificateStore store, CertificateFilter filter,
            CancellationToken ct = default) {
            var results = await store.QueryCertificatesAsync(filter, null, ct);
            var certificates = new List<Certificate>(results.Certificates);
            while (results.ContinuationToken != null) {
                results = await store.ListCertificatesAsync(
                    results.ContinuationToken, null, ct);
                certificates.AddRange(results.Certificates);
            }
            return certificates;
        }
    }
}

