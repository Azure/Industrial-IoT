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
        /// Build the certificates trust list
        /// </summary>
        public static CertificateTrustList BuildCertificateTrustList(
            this CertificateStore certificateStore,
            CertificateTrustList certificateTrustList) {
            if (certificateStore == null) {
                throw new ArgumentNullException(nameof(certificateStore));
            }

            if (certificateTrustList == null) {
                throw new ArgumentNullException(nameof(certificateTrustList));
            }

            if (certificateTrustList.StorePath != certificateStore.StorePath) {
                certificateTrustList.StoreType = certificateStore.StoreType;
                certificateTrustList.StorePath = certificateStore.StorePath;
            }

            return certificateTrustList;
        }

        /// <summary>
        /// Build the certificate identifier for the own app
        /// </summary>
        /// <param name="certificateStore"></param>
        /// <param name="certificateIdentifier"></param>
        /// <returns></returns>
        public static CertificateIdentifier BuildCertificateIdentifier(
            this CertificateInfo certificateStore,
            CertificateIdentifier certificateIdentifier) {
            if (certificateStore == null) {
                throw new ArgumentNullException(nameof(certificateStore));
            }

            if (certificateIdentifier == null) {
                throw new ArgumentNullException(nameof(certificateIdentifier));
            }

            if (certificateIdentifier.StorePath != certificateStore.StorePath) {
                certificateIdentifier.StoreType = certificateStore.StoreType;
                certificateIdentifier.StorePath = certificateStore.StorePath;
            }

            return certificateIdentifier;
        }

        /// <summary>
        /// Build a certificate identifier store. Particularily for rejected certificates
        /// </summary>
        public static CertificateStoreIdentifier BuildCertificateIdentifierStore(
            this CertificateStore certificateStore,
            CertificateStoreIdentifier certificateStoreIdentifier) {
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

            return certificateStoreIdentifier;
        }
    }
}
