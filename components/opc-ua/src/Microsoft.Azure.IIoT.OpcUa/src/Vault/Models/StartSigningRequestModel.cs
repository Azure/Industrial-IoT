// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.Serializers;

    /// <summary>
    /// Signing request
    /// </summary>
    public sealed class StartSigningRequestModel {

        /// <summary>
        /// Entity id
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        /// Trust group id
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// DER raw or PEM formated certificate signing request
        /// </summary>
        public VariantValue CertificateRequest { get; set; }
    }
}
