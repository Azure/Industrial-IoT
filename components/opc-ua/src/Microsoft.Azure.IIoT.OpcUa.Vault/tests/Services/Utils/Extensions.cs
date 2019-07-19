// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using System;

    public static class Extensions {

        internal static void AddRange(this X509CertificateChainModel list,
            X509CertificateChainModel items) {
            if (list == null || items == null) {
                return;
            }
            foreach (var item in items.Chain) {
                list.Chain.Add(item);
            }
        }

        internal static void AddRange(this X509CrlChainModel list,
            X509CrlChainModel items) {
            if (list == null || items == null) {
                return;
            }
            foreach (var item in items.Chain) {
                list.Chain.Add(item);
            }
        }

        internal static void AddRange(this TrustListModel list, TrustListModel items) {
            if (list == null) {
                throw new ArgumentNullException(nameof(list));
            }

            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }

            list.TrustedCertificates.AddRange(items.TrustedCertificates);
            list.TrustedCrls.AddRange(items.TrustedCrls);
            list.IssuerCertificates.AddRange(items.IssuerCertificates);
            list.IssuerCrls.AddRange(items.IssuerCrls);
        }
    }
}
