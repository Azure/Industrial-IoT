// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Certificate request record model
    /// </summary>
    public sealed class CertificateRequestRecordApiModel {

        /// <summary>
        /// Request id
        /// </summary>
        [JsonProperty(PropertyName = "requestId")]
        public string RequestId { get; set; }

        /// <summary>
        /// Application id
        /// </summary>
        [JsonProperty(PropertyName = "entityId")]
        public string EntityId { get; set; }

        /// <summary>
        /// Trust group
        /// </summary>
        [JsonProperty(PropertyName = "groupId")]
        public string GroupId { get; set; }

        /// <summary>
        /// Request state
        /// </summary>
        [JsonProperty(PropertyName = "state")]
        public CertificateRequestState State { get; set; }

        /// <summary>
        /// Request type
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public CertificateRequestType Type { get; set; }

        /// <summary>
        /// Error diagnostics
        /// </summary>
        [JsonProperty(PropertyName = "errorInfo",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken ErrorInfo { get; set; }

        /// <summary>
        /// Request time
        /// </summary>
        [JsonProperty(PropertyName = "submitted",
            NullValueHandling = NullValueHandling.Ignore)]
        public VaultOperationContextApiModel Submitted { get; set; }

        /// <summary>
        /// Approved or rejected
        /// </summary>
        [JsonProperty(PropertyName = "approved",
            NullValueHandling = NullValueHandling.Ignore)]
        public VaultOperationContextApiModel Approved { get; set; }

        /// <summary>
        /// Finished
        /// </summary>
        [JsonProperty(PropertyName = "accepted",
            NullValueHandling = NullValueHandling.Ignore)]
        public VaultOperationContextApiModel Accepted { get; set; }
    }
}
