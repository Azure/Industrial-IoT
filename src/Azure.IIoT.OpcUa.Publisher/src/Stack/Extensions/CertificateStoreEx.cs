// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Certificate store extensions
    /// </summary>
    public static class CertificateStoreEx {

        /// <summary>
        /// Add to certificate store
        /// </summary>
        /// <param name="store"></param>
        /// <param name="certificates"></param>
        /// <param name="noCopy"></param>
        /// <returns></returns>
        public static void Add(this ICertificateStore store,
            IEnumerable<X509Certificate2> certificates,
            bool noCopy = false) {
            if (certificates == null) {
                throw new ArgumentNullException(nameof(certificates));
            }
            foreach (var cert in certificates) {
                Try.Op(() => store.Delete(cert.Thumbprint));
                store.Add(noCopy ? cert : new X509Certificate2(cert));
            }
        }

        /// <summary>
        /// Remove from certificate store
        /// </summary>
        /// <param name="store"></param>
        /// <param name="certificates"></param>
        /// <returns></returns>
        public static void Remove(this ICertificateStore store,
            IEnumerable<X509Certificate2> certificates) {
            if (certificates == null) {
                throw new ArgumentNullException(nameof(certificates));
            }
            foreach (var cert in certificates) {
                store.Delete(cert.Thumbprint);
            }
        }
    }
}
