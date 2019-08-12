// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Application information
    /// </summary>
    public class ApplicationRegistrationQueryApiModel {

        /// <summary>
        /// Type of application
        /// </summary>
        [JsonProperty(PropertyName = "applicationType",
            NullValueHandling = NullValueHandling.Ignore)]
        public ApplicationType? ApplicationType { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        [JsonProperty(PropertyName = "applicationUri",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        [JsonProperty(PropertyName = "productUri",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ProductUri { get; set; }

        /// <summary>
        /// Name of application
        /// </summary>
        [JsonProperty(PropertyName = "applicationName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Locale of application name - default is "en"
        /// </summary>
        [JsonProperty(PropertyName = "locale",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Locale { get; set; }

        /// <summary>
        /// Application capability filter.
        /// </summary>
        [JsonProperty(PropertyName = "capability",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Capability { get; set; }

        /// <summary>
        /// Discovery profile uri
        /// </summary>
        [JsonProperty(PropertyName = "discoveryProfileUri",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DiscoveryProfileUri { get; set; }

        /// <summary>
        /// Gateway server uri
        /// </summary>
        [JsonProperty(PropertyName = "gatewayServerUri",
            NullValueHandling = NullValueHandling.Ignore)]
        public string GatewayServerUri { get; set; }

        /// <summary>
        /// Supervisor or site the application belongs to.
        /// </summary>
        [JsonProperty(PropertyName = "siteOrSupervisorId",
           NullValueHandling = NullValueHandling.Ignore)]
        public string SiteOrSupervisorId { get; set; }

        /// <summary>
        /// Whether to include applications that were soft deleted
        /// </summary>
        [JsonProperty(PropertyName = "includeNotSeenSince",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? IncludeNotSeenSince { get; set; }
    }
}

