// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Application info model
    /// </summary>
    public class ApplicationInfoApiModel {

        /// <summary>
        /// Unique application id
        /// </summary>
        [JsonProperty(PropertyName = "applicationId")]
        public string ApplicationId { get; set; }

        /// <summary>
        /// Type of application
        /// </summary>
        /// <example>Server</example>
        [JsonProperty(PropertyName = "applicationType")]
        public ApplicationType ApplicationType { get; set; }

        /// <summary>
        /// Unique application uri
        /// </summary>
        [JsonProperty(PropertyName = "applicationUri")]
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        [JsonProperty(PropertyName = "productUri",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ProductUri { get; set; }

        /// <summary>
        /// Default name of application
        /// </summary>
        [JsonProperty(PropertyName = "applicationName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Locale of default name - defaults to "en"
        /// </summary>
        [JsonProperty(PropertyName = "locale",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Locale { get; set; }

        /// <summary>
        /// Localized Names of application keyed on locale
        /// </summary>
        [JsonProperty(PropertyName = "localizedNames",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> LocalizedNames { get; set; }

        /// <summary>
        /// The capabilities advertised by the server.
        /// </summary>
        /// <example>LDS</example>
        /// <example>DA</example>
        [JsonProperty(PropertyName = "capabilities",
            NullValueHandling = NullValueHandling.Ignore)]
        public HashSet<string> Capabilities { get; set; }

        /// <summary>
        /// Discovery urls of the server
        /// </summary>
        [JsonProperty(PropertyName = "discoveryUrls",
            NullValueHandling = NullValueHandling.Ignore)]
        public HashSet<string> DiscoveryUrls { get; set; }

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
        /// Host addresses of server application or null
        /// </summary>
        [JsonProperty(PropertyName = "hostAddresses",
            NullValueHandling = NullValueHandling.Ignore)]
        public HashSet<string> HostAddresses { get; set; }

        /// <summary>
        /// Site of the application
        /// </summary>
        /// <example>productionlineA</example>
        /// <example>cellB</example>
        [JsonProperty(PropertyName = "siteId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SiteId { get; set; }

        /// <summary>
        /// Discoverer that registered the application
        /// </summary>
        [JsonProperty(PropertyName = "discovererId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DiscovererId { get; set; }

        /// <summary>
        /// Last time application was seen
        /// </summary>
        [JsonProperty(PropertyName = "notSeenSince",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? NotSeenSince { get; set; }

        /// <summary>
        /// Created
        /// </summary>
        [JsonProperty(PropertyName = "created",
            NullValueHandling = NullValueHandling.Ignore)]
        public RegistryOperationApiModel Created { get; set; }

        /// <summary>
        /// Updated
        /// </summary>
        [JsonProperty(PropertyName = "updated",
            NullValueHandling = NullValueHandling.Ignore)]
        public RegistryOperationApiModel Updated { get; set; }
    }
}
