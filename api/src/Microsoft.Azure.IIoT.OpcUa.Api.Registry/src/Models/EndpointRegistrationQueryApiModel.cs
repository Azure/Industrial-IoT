// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using System.ComponentModel;

    /// <summary>
    /// Endpoint query
    /// </summary>
    public class EndpointRegistrationQueryApiModel {

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        [JsonProperty(PropertyName = "url",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string Url { get; set; }

        /// <summary>
        /// Endpoint certificate thumbprint
        /// </summary>
        [JsonProperty(PropertyName = "certificate",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string Certificate { get; set; }

        /// <summary>
        /// Security Mode
        /// </summary>
        [JsonProperty(PropertyName = "securityMode",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Security policy uri
        /// </summary>
        [JsonProperty(PropertyName = "securityPolicy",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Whether the endpoint was activated
        /// </summary>
        [JsonProperty(PropertyName = "activated",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? Activated { get; set; }

        /// <summary>
        /// Whether the endpoint is connected on supervisor.
        /// </summary>
        [JsonProperty(PropertyName = "connected",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? Connected { get; set; }

        /// <summary>
        /// The last state of the the activated endpoint
        /// </summary>
        [JsonProperty(PropertyName = "endpointState",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(EndpointConnectivityState.Disconnected)]
        public EndpointConnectivityState? EndpointState { get; set; }

        /// <summary>
        /// Whether to include endpoints that were soft deleted
        /// </summary>
        [JsonProperty(PropertyName = "includeNotSeenSince",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? IncludeNotSeenSince { get; set; }

        /// <summary>
        /// Discoverer id to filter with
        /// </summary>
        [JsonProperty(PropertyName = "discovererId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string DiscovererId { get; set; }

        /// <summary>
        /// Application id to filter
        /// </summary>
        [JsonProperty(PropertyName = "applicationId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string ApplicationId { get; set; }

        /// <summary>
        /// Supervisor id to filter with
        /// </summary>
        [JsonProperty(PropertyName = "supervisorId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SupervisorId { get; set; }

        /// <summary>
        /// Site or gateway id to filter with
        /// </summary>
        [JsonProperty(PropertyName = "siteOrGatewayId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SiteOrGatewayId { get; set; }
    }
}

