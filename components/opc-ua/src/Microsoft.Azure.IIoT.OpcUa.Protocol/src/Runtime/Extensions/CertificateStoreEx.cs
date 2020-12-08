// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
    using Opc.Ua;


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
    }
}