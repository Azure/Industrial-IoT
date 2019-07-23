// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Certificate request record model
    /// </summary>
    public sealed class CertificateRequestRecordApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public CertificateRequestRecordApiModel() {
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="request"></param>
        public CertificateRequestRecordApiModel(CertificateRequestRecordModel request) {
            RequestId = request.RequestId;
            EntityId = request.EntityId;
            Type = request.Type;
            State = request.State;
            GroupId = request.GroupId;
            Submitted = request.Submitted == null ? null :
                new VaultOperationContextApiModel(request.Submitted);
            Accepted = request.Accepted == null ? null :
                new VaultOperationContextApiModel(request.Accepted);
            Approved = request.Approved == null ? null :
                new VaultOperationContextApiModel(request.Approved);
            ErrorInfo = request.ErrorInfo;
        }

        /// <summary>
        /// To service model
        /// </summary>
        /// <returns></returns>
        public CertificateRequestRecordModel ToServiceModel() {
            return new CertificateRequestRecordModel {
                RequestId = RequestId,
                EntityId = EntityId,
                Type = Type,
                State = State,
                GroupId = GroupId,
                Submitted = Submitted?.ToServiceModel(),
                Accepted = Accepted?.ToServiceModel(),
                Approved = Approved?.ToServiceModel(),
                ErrorInfo = ErrorInfo
            };
        }

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
