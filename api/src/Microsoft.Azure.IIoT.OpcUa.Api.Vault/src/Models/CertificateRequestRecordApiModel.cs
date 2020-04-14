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
        [DataMember(Name = "requestId", Order = 0)]
        public string RequestId { get; set; }

        /// <summary>
        /// Application id
        /// </summary>
        [DataMember(Name = "entityId", Order = 1)]
        public string EntityId { get; set; }

        /// <summary>
        /// Trust group
        /// </summary>
        [DataMember(Name = "groupId", Order = 2)]
        public string GroupId { get; set; }

        /// <summary>
        /// Request state
        /// </summary>
        [DataMember(Name = "state", Order = 3)]
        public CertificateRequestState State { get; set; }

        /// <summary>
        /// Request type
        /// </summary>
        [DataMember(Name = "type", Order = 4)]
        public CertificateRequestType Type { get; set; }

        /// <summary>
        /// Error diagnostics
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 5,
            EmitDefaultValue = false)]
        public VariantValue ErrorInfo { get; set; }

        /// <summary>
        /// Request time
        /// </summary>
        [DataMember(Name = "submitted", Order = 6,
            EmitDefaultValue = false)]
        public VaultOperationContextApiModel Submitted { get; set; }

        /// <summary>
        /// Approved or rejected
        /// </summary>
        [DataMember(Name = "approved", Order = 7,
            EmitDefaultValue = false)]
        public VaultOperationContextApiModel Approved { get; set; }

        /// <summary>
        /// Finished
        /// </summary>
        [DataMember(Name = "accepted", Order = 8,
            EmitDefaultValue = false)]
        public VaultOperationContextApiModel Accepted { get; set; }
    }
}
