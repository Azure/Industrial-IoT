// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
    using Opc.Ua;
    using System;

    /// <summary>
    /// Certificate store extensions
    /// </summary>
    public static class CertificateStoreEx {

        /// <summary>
        /// Apply the configured settings provided via a CertificateStore to a CertificateTrustList.
        /// </summary>
        public static void ApplyLocalConfig(
            this CertificateTrustList certificateTrustList,
            CertificateStore certificateStore) {
            if (certificateTrustList == null) {
                throw new ArgumentNullException(nameof(certificateTrustList));
            }

            if (certificateStore == null) {
                throw new ArgumentNullException(nameof(certificateStore));
            }

            if (certificateTrustList.StorePath != certificateStore.StorePath) {
                certificateTrustList.StoreType = certificateStore.StoreType;
                certificateTrustList.StorePath = certificateStore.StorePath;
            }
        }

        /// <summary>
        /// Applies the configuration settings to the own app certificate.
        /// </summary>
        public static void ApplyLocalConfig(
            this CertificateIdentifier certificateIdentifier,
            CertificateInfo certificateStore) {
            if (certificateIdentifier == null) {
                throw new ArgumentNullException(nameof(certificateIdentifier));
            }

            if (certificateStore == null) {
                throw new ArgumentNullException(nameof(certificateStore));
            }

            if (certificateIdentifier.StorePath != certificateStore.StorePath) {
                certificateIdentifier.StoreType = certificateStore.StoreType;
                certificateIdentifier.StorePath = certificateStore.StorePath;
            }
        }

        /// <summary>
        /// Apply the configured settings provided via a CertificateStore to a CertificateStoreIdentifier
        /// Particularily used for for rejected certificates store.
        /// </summary>
        public static void ApplyLocalConfig(
            this CertificateStoreIdentifier certificateStoreIdentifier,
            CertificateStore certificateStore) {
            if (certificateStore == null) {
                throw new ArgumentNullException(nameof(certificateStore));
            }

            if (certificateStoreIdentifier == null) {
                throw new ArgumentNullException(nameof(certificateStoreIdentifier));
            }

            if (certificateStoreIdentifier.StorePath != certificateStore.StorePath) {
                certificateStoreIdentifier.StoreType = certificateStore.StoreType;
                certificateStoreIdentifier.StorePath = certificateStore.StorePath;
            }
        }
    }
}
