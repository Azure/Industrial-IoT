// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate trust list extensions
    /// </summary>
    public static class CertificateTrustListEx
    {
        /// <summary>
        /// Remove certficates
        /// </summary>
        /// <param name="trustList"></param>
        /// <param name="certificates"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="certificates"/> is <c>null</c>.</exception>
        public static async Task RemoveAsync(this CertificateTrustList trustList,
            IEnumerable<X509Certificate2> certificates, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(certificates);
            using var trustedStore = trustList.OpenStore(null!);
            await trustedStore.RemoveAsync(certificates, ct).ConfigureAwait(false);
            var trustedCertificates = trustList.TrustedCertificates.ToArray()?.ToList() ?? [];
            foreach (var cert in certificates)
            {
                trustedCertificates.Remove(new CertificateIdentifier { RawData = cert.RawData });
            }
            trustList.TrustedCertificates = trustedCertificates;
        }

        /// <summary>
        /// Add to trust list
        /// </summary>
        /// <param name="trustList"></param>
        /// <param name="certificates"></param>
        /// <param name="noCopy"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="certificates"/> is <c>null</c>.</exception>
        public static async Task AddAsync(this CertificateTrustList trustList,
            IEnumerable<X509Certificate2> certificates, bool noCopy = false,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(certificates);
            using var trustedStore = trustList.OpenStore(null!);
            await trustedStore.AddAsync(certificates, noCopy, ct: ct).ConfigureAwait(false);
            var trustedCertificates = trustList.TrustedCertificates.ToArray()?.ToList() ?? [];
            foreach (var cert in certificates)
            {
                trustedCertificates.Add(new CertificateIdentifier { RawData = cert.RawData });
            }
            trustList.TrustedCertificates = trustedCertificates;
        }
    }
}
