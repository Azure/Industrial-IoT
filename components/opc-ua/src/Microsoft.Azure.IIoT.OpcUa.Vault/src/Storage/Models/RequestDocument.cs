// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Storage.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Certificate request document in cosmos db
    /// </summary>
    [Serializable]
    public class RequestDocument {

        /// <summary>
        /// Certificate Group id
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// Request id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string RequestId { get; set; }

        /// <summary>
        /// Etag
        /// </summary>
        [JsonProperty(PropertyName = "_etag")]
        public string ETag { get; set; }

        /// <summary>
        /// Document type
        /// </summary>
        public string ClassType { get; set; } = ClassTypeName;

        /// <summary>
        /// Numeric index
        /// </summary>
        public uint Index { get; set; }

        /// <summary>
        /// Request state
        /// </summary>
        public CertificateRequestState State { get; set; }

        /// <summary>
        /// Request type
        /// </summary>
        public CertificateRequestType Type { get; set; }

        /// <summary>
        /// Signing request
        /// </summary>
        public byte[] SigningRequest { get; set; }

        /// <summary>
        /// Entity
        /// </summary>
        public EntityInfoModel Entity { get; set; }

        /// <summary>
        /// Subject name
        /// </summary>
        public string SubjectName { get; set; }

        /// <summary>
        /// Certificate
        /// </summary>
        public X509CertificateModel Certificate { get; set; }

        /// <summary>
        /// Private key handle
        /// </summary>
        public JToken KeyHandle { get; set; }

        /// <summary>
        /// Error information
        /// </summary>
        public JToken ErrorInfo { get; set; }

        /// <summary>
        /// Request time
        /// </summary>
        public VaultOperationContextModel Submitted { get; set; }

        /// <summary>
        /// Approve or reject time
        /// </summary>
        public VaultOperationContextModel Approved { get; set; }

        /// <summary>
        /// Complete time
        /// </summary>
        public VaultOperationContextModel Completed { get; set; }

        /// <summary>
        /// Delete time
        /// </summary>
        public VaultOperationContextModel Deleted { get; set; }

        /// <inheritdoc/>

        public static readonly string ClassTypeName = "Request";
    }
}
