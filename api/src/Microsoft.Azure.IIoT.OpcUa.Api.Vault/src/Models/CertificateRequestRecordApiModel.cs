// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;

    /// <summary>
    /// Certificate request record model
    /// </summary>
    [DataContract]
    public sealed class CertificateRequestRecordApiModel {

        /// <summary>
        /// Request id
        /// </summary>
        [DataMember(Name = "requestId")]
        public string RequestId { get; set; }

        /// <summary>
        /// Application id
        /// </summary>
        [DataMember(Name = "entityId")]
        public string EntityId { get; set; }

        /// <summary>
        /// Trust group
        /// </summary>
        [DataMember(Name = "groupId")]
        public string GroupId { get; set; }

        /// <summary>
        /// Request state
        /// </summary>
        [DataMember(Name = "state")]
        public CertificateRequestState State { get; set; }

        /// <summary>
        /// Request type
        /// </summary>
        [DataMember(Name = "type")]
        public CertificateRequestType Type { get; set; }

        /// <summary>
        /// Error diagnostics
        /// </summary>
        [DataMember(Name = "errorInfo",
            EmitDefaultValue = false)]
        public VariantValue ErrorInfo { get; set; }

        /// <summary>
        /// Request time
        /// </summary>
        [DataMember(Name = "submitted",
            EmitDefaultValue = false)]
        public VaultOperationContextApiModel Submitted { get; set; }

        /// <summary>
        /// Approved or rejected
        /// </summary>
        [DataMember(Name = "approved",
            EmitDefaultValue = false)]
        public VaultOperationContextApiModel Approved { get; set; }

        /// <summary>
        /// Finished
        /// </summary>
        [DataMember(Name = "accepted",
            EmitDefaultValue = false)]
        public VaultOperationContextApiModel Accepted { get; set; }
    }
}
