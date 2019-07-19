// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Certificate trust list extensions
    /// </summary>
    public static class CertificateTrustListEx {

        /// <summary>
        /// Remove certficates
        /// </summary>
        /// <param name="trustList"></param>
        /// <param name="certificates"></param>
        /// <returns></returns>
        public static void Remove(this CertificateTrustList trustList,
            IEnumerable<X509Certificate2> certificates) {
            if (certificates == null) {
                throw new ArgumentNullException(nameof(certificates));
            }
            using (var trustedStore = trustList.OpenStore()) {
                trustedStore.Remove(certificates);
                foreach (var cert in certificates) {
                    trustList.TrustedCertificates.Remove(new CertificateIdentifier(cert));
                }
            }
        }

        /// <summary>
        /// Try remove trust list
        /// </summary>
        /// <param name="trustList"></param>
        /// <param name="certificates"></param>
        /// <returns></returns>
        public static bool TryRemove(this CertificateTrustList trustList,
            IEnumerable<X509Certificate2> certificates) {
            try {
                Remove(trustList, certificates);
                return true;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Add to trust list
        /// </summary>
        /// <param name="trustList"></param>
        /// <param name="certificates"></param>
        /// <param name="noCopy"></param>
        /// <returns></returns>
        public static void Add(this CertificateTrustList trustList,
            IEnumerable<X509Certificate2> certificates, bool noCopy = false) {
            if (certificates == null) {
                throw new ArgumentNullException(nameof(certificates));
            }
            using (var trustedStore = trustList.OpenStore()) {
                trustedStore.Add(certificates, noCopy);
                foreach (var cert in certificates) {
                    trustList.TrustedCertificates.Add(new CertificateIdentifier(
                        noCopy ? cert : new X509Certificate2(cert)));
                }
            }
        }
    }
}
