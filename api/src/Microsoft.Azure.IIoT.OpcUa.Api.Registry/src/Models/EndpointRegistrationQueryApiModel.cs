// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Endpoint query
    /// </summary>
    public class EndpointRegistrationQueryApiModel {

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        [JsonProperty(PropertyName = "url",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }

        /// <summary>
        /// Type of credential selected for authentication
        /// </summary>
        [JsonProperty(PropertyName = "userAuthentication",
            NullValueHandling = NullValueHandling.Ignore)]
        public CredentialType? UserAuthentication { get; set; }

        /// <summary>
        /// Certificate of the endpoint
        /// </summary>
        [JsonProperty(PropertyName = "certificate",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] Certificate { get; set; }

        /// <summary>
        /// Security Mode
        /// </summary>
        [JsonProperty(PropertyName = "securityMode",
            NullValueHandling = NullValueHandling.Ignore)]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Security policy uri
        /// </summary>
        [JsonProperty(PropertyName = "securityPolicy",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Whether the endpoint was activated
        /// </summary>
        [JsonProperty(PropertyName = "activated",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Activated { get; set; }

        /// <summary>
        /// Whether the endpoint is connected on supervisor.
        /// </summary>
        [JsonProperty(PropertyName = "connected",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Connected { get; set; }

        /// <summary>
        /// The last state of the the activated endpoint
        /// </summary>
        [JsonProperty(PropertyName = "endpointState",
            NullValueHandling = NullValueHandling.Ignore)]
        public EndpointConnectivityState? EndpointState { get; set; }

        /// <summary>
        /// Whether to include endpoints that were soft deleted
        /// </summary>
        [JsonProperty(PropertyName = "includeNotSeenSince",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? IncludeNotSeenSince { get; set; }
    }
}

