// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {

    /// <summary>
    /// Certificate request query model
    /// </summary>
    public sealed class CertificateRequestQueryRequestModel {

        /// <summary>
        /// The entity id to filter with
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        /// The certificate request state
        /// </summary>
        public CertificateRequestState? State { get; set; }
    }
}
