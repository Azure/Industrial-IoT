// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;

    /// <summary>
    /// Application information
    /// </summary>
    public class ApplicationRegistrationQueryApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationRegistrationQueryApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ApplicationRegistrationQueryApiModel(ApplicationRegistrationQueryModel model) {
            ApplicationType = model.ApplicationType;
            ApplicationUri = model.ApplicationUri;
            ProductUri = model.ProductUri;
            ApplicationName = model.ApplicationName;
            Locale = model.Locale;
            Capability = model.Capability;
            SiteOrSupervisorId = model.SiteOrSupervisorId;
            IncludeNotSeenSince = model.IncludeNotSeenSince;
            DiscoveryProfileUri = model.DiscoveryProfileUri;
            GatewayServerUri = model.GatewayServerUri;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public ApplicationRegistrationQueryModel ToServiceModel() {
            return new ApplicationRegistrationQueryModel {
                ApplicationType = ApplicationType,
                ApplicationUri = ApplicationUri,
                ProductUri = ProductUri,
                ApplicationName = ApplicationName,
                Locale = Locale,
                Capability = Capability,
                SiteOrSupervisorId = SiteOrSupervisorId,
                IncludeNotSeenSince = IncludeNotSeenSince,
                GatewayServerUri = GatewayServerUri,
                DiscoveryProfileUri = DiscoveryProfileUri
            };
        }

        /// <summary>
        /// Type of application
        /// </summary>
        [JsonProperty(PropertyName = "applicationType",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public ApplicationType? ApplicationType { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        [JsonProperty(PropertyName = "applicationUri",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        [JsonProperty(PropertyName = "productUri",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string ProductUri { get; set; }

        /// <summary>
        /// Name of application
        /// </summary>
        [JsonProperty(PropertyName = "applicationName",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Locale of application name - default is "en"
        /// </summary>
        [JsonProperty(PropertyName = "locale",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string Locale { get; set; }

        /// <summary>
        /// Application capability to query with
        /// </summary>
        [JsonProperty(PropertyName = "capability",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string Capability { get; set; }

        /// <summary>
        /// Discovery profile uri
        /// </summary>
        [JsonProperty(PropertyName = "discoveryProfileUri",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string DiscoveryProfileUri { get; set; }

        /// <summary>
        /// Gateway server uri
        /// </summary>
        [JsonProperty(PropertyName = "gatewayServerUri",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string GatewayServerUri { get; set; }

        /// <summary>
        /// Supervisor or site the application belongs to.
        /// </summary>
        [JsonProperty(PropertyName = "siteOrSupervisorId",
           NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SiteOrSupervisorId { get; set; }

        /// <summary>
        /// Whether to include apps that were soft deleted
        /// </summary>
        [JsonProperty(PropertyName = "includeNotSeenSince",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? IncludeNotSeenSince { get; set; }
    }
}

