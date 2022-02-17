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
        /// Create trust list
        /// </summary>
        /// <param name="certificateStore"></param>
        /// <returns></returns>
        public static CertificateTrustList ToCertificateTrustList(this CertificateStore certificateStore) {
            var certificateTrustList = new CertificateTrustList {
                StoreType = certificateStore.StoreType,
                StorePath = certificateStore.StorePath
            };

            return certificateTrustList;
        }

        /// <summary>
        /// Create identifier
        /// </summary>
        /// <param name="certificateInfo"></param>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public static CertificateIdentifier ToCertificateIdentifier(
            this CertificateInfo certificateInfo, string hostname) {
            var certificateIdentifier = new CertificateIdentifier {
                StoreType = certificateInfo.StoreType,
                StorePath = certificateInfo.StorePath,
                SubjectName = certificateInfo.SubjectName.Replace("DC=localhost", $"DC={hostname}")
            };

            return certificateIdentifier;
        }

        /// <summary>
        /// Create trust list
        /// </summary>
        /// <param name="certificateStore"></param>
        /// <param name="certificateTrustList"></param>
        /// <returns></returns>
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
        /// Create trust list
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
        /// Create trust list
        /// </summary>
        /// <param name="certificateStore"></param>
        /// <param name="certificateStoreIdentifier"></param>
        /// <returns></returns>
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
