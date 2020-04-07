// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Trust list api model
    /// </summary>
    public sealed class TrustListModel {

        /// <summary>
        /// Group id
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// Issuer certificates
        /// </summary>
        public X509CertificateChainModel IssuerCertificates { get; set; }

        /// <summary>
        /// Issuer crls
        /// </summary>
        public X509CrlChainModel IssuerCrls { get; set; }

        /// <summary>
        /// Trusted certificates
        /// </summary>
        public X509CertificateChainModel TrustedCertificates { get; set; }

        /// <summary>
        /// Trusted crls
        /// </summary>
        public X509CrlChainModel TrustedCrls { get; set; }

        /// <summary>
        /// Next page link
        /// </summary>
        public string NextPageLink { get; set; }
    }
}
