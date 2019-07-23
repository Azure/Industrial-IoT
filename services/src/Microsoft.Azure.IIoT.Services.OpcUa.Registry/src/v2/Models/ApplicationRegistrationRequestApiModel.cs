// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Application information
    /// </summary>
    public class ApplicationRegistrationRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationRegistrationRequestApiModel() { }

        /// <summary>
        /// Create model from service model
        /// </summary>
        /// <param name="model"></param>
        public ApplicationRegistrationRequestApiModel(ApplicationRegistrationRequestModel model) {
            ApplicationType = model.ApplicationType;
            ApplicationUri = model.ApplicationUri;
            ApplicationName = model.ApplicationName;
            Locale = model.Locale;
            LocalizedNames = model.LocalizedNames;
            ProductUri = model.ProductUri;
            DiscoveryProfileUri = model.DiscoveryProfileUri;
            DiscoveryUrls = model.DiscoveryUrls;
            Capabilities = model.Capabilities;
            SiteId = model.SiteId;
            GatewayServerUri = model.GatewayServerUri;
        }

        /// <summary>
        /// Create service model from model
        /// </summary>
        public ApplicationRegistrationRequestModel ToServiceModel() {
            return new ApplicationRegistrationRequestModel {
                ApplicationType = ApplicationType,
                ApplicationUri = ApplicationUri,
                ApplicationName = ApplicationName,
                Locale = Locale,
                LocalizedNames = LocalizedNames,
                ProductUri = ProductUri,
                DiscoveryProfileUri = DiscoveryProfileUri,
                DiscoveryUrls = DiscoveryUrls,
                SiteId = SiteId,
                GatewayServerUri = GatewayServerUri,
                Capabilities = Capabilities
            };
        }

        /// <summary>
        /// Unique application uri
        /// </summary>
        [JsonProperty(PropertyName = "applicationUri")]
        [Required]
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Type of application
        /// </summary>
        /// <example>Server</example>
        [JsonProperty(PropertyName = "applicationType",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public ApplicationType? ApplicationType { get; set; }

        /// <summary>
        /// Product uri of the application.
        /// </summary>
        /// <example>http://contoso.com/fridge/1.0</example>
        [JsonProperty(PropertyName = "productUri",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string ProductUri { get; set; }

        /// <summary>
        /// Default name of the server or client.
        /// </summary>
        [JsonProperty(PropertyName = "applicationName",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Locale of default name
        /// </summary>
        [JsonProperty(PropertyName = "locale",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string Locale { get; set; }

        /// <summary>
        /// Site of the application
        /// </summary>
        [JsonProperty(PropertyName = "siteId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SiteId { get; set; }

        /// <summary>
        /// Localized names key off locale id.
        /// </summary>
        [JsonProperty(PropertyName = "localizedNames",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public Dictionary<string, string> LocalizedNames { get; set; }

        /// <summary>
        /// The OPC UA defined capabilities of the server.
        /// </summary>
        /// <example>LDS</example>
        /// <example>DA</example>
        [JsonProperty(PropertyName = "capabilities",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public HashSet<string> Capabilities { get; set; }

        /// <summary>
        /// Discovery urls of the server.
        /// </summary>
        [JsonProperty(PropertyName = "discoveryUrls",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public HashSet<string> DiscoveryUrls { get; set; }

        /// <summary>
        /// The discovery profile uri of the server.
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
    }
}
