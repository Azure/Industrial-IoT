// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using System;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Certificate identifier extensions
    /// </summary>
    public static class CertificateIdentifierEx {

        /// <summary>
        /// Remove from certificate store
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="certificate"></param>
        public static void RemoveFromStore(this CertificateIdentifier identifier,
            X509Certificate2 certificate) {
            if (certificate == null) {
                throw new ArgumentNullException(nameof(certificate));
            }
            using (var store = CertificateStoreIdentifier.CreateStore(identifier.StoreType)) {
                store.Open(identifier.StorePath);
                store.Remove(certificate.YieldReturn());
            }
        }

        /// <summary>
        /// Add to certificate store
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="certificate"></param>
        /// <param name="noCopy"></param>
        /// <returns></returns>
        public static void AddToStore(this CertificateIdentifier identifier,
            X509Certificate2 certificate, bool noCopy = false) {
            if (certificate == null) {
                throw new ArgumentNullException(nameof(certificate));
            }
            using (var store = CertificateStoreIdentifier.CreateStore(identifier.StoreType)) {
                store.Open(identifier.StorePath);
                store.Add(certificate.YieldReturn(), noCopy);
            }
        }
    }
}
