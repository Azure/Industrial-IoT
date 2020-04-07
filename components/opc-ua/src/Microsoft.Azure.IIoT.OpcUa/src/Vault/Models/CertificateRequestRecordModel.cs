// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.Serializers;

    /// <summary>
    /// Certificate request record model
    /// </summary>
    public sealed class CertificateRequestRecordModel {

        /// <summary>
        /// Request id
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Application id
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        /// Trust group
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// Request state
        /// </summary>
        public CertificateRequestState State { get; set; }

        /// <summary>
        /// Request type
        /// </summary>
        public CertificateRequestType Type { get; set; }

        /// <summary>
        /// Error diagnostics
        /// </summary>
        public VariantValue ErrorInfo { get; set; }

        /// <summary>
        /// Request time
        /// </summary>
        public VaultOperationContextModel Submitted { get; set; }

        /// <summary>
        /// Approved or rejected
        /// </summary>
        public VaultOperationContextModel Approved { get; set; }

        /// <summary>
        /// Accepted
        /// </summary>
        public VaultOperationContextModel Accepted { get; set; }

        /// <summary>
        /// Deleted
        /// </summary>
        public VaultOperationContextModel Deleted { get; set; }
    }
}
