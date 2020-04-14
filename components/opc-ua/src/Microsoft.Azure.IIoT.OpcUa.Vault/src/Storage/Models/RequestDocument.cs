// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Storage.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;

    /// <summary>
    /// Certificate request document in cosmos db
    /// </summary>
    [DataContract]
    public class RequestDocument {

        /// <summary>
        /// Certificate Group id
        /// </summary>
        [DataMember]
        public string GroupId { get; set; }

        /// <summary>
        /// Request id
        /// </summary>
        [DataMember(Name = "id")]
        public string RequestId { get; set; }

        /// <summary>
        /// Etag
        /// </summary>
        [DataMember(Name = "_etag")]
        public string ETag { get; set; }

        /// <summary>
        /// Document type
        /// </summary>
        [DataMember]
        public string ClassType { get; set; } = ClassTypeName;

        /// <summary>
        /// Numeric index
        /// </summary>
        [DataMember]
        public uint Index { get; set; }

        /// <summary>
        /// Request state
        /// </summary>
        [DataMember]
        public CertificateRequestState State { get; set; }

        /// <summary>
        /// Request type
        /// </summary>
        [DataMember]
        public CertificateRequestType Type { get; set; }

        /// <summary>
        /// Signing request
        /// </summary>
        [DataMember]
        public byte[] SigningRequest { get; set; }

        /// <summary>
        /// Entity
        /// </summary>
        [DataMember]
        public EntityInfoModel Entity { get; set; }

        /// <summary>
        /// Subject name
        /// </summary>
        [DataMember]
        public string SubjectName { get; set; }

        /// <summary>
        /// Certificate
        /// </summary>
        [DataMember]
        public X509CertificateModel Certificate { get; set; }

        /// <summary>
        /// Private key handle
        /// </summary>
        [DataMember]
        public byte[] KeyHandle { get; set; }

        /// <summary>
        /// Error information
        /// </summary>
        [DataMember]
        public VariantValue ErrorInfo { get; set; }

        /// <summary>
        /// Request time
        /// </summary>
        [DataMember]
        public VaultOperationContextModel Submitted { get; set; }

        /// <summary>
        /// Approve or reject time
        /// </summary>
        [DataMember]
        public VaultOperationContextModel Approved { get; set; }

        /// <summary>
        /// Complete time
        /// </summary>
        [DataMember]
        public VaultOperationContextModel Completed { get; set; }

        /// <summary>
        /// Delete time
        /// </summary>
        [DataMember]
        public VaultOperationContextModel Deleted { get; set; }

        /// <inheritdoc/>

        public static readonly string ClassTypeName = "Request";
    }
}
