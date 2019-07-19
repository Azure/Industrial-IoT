// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;

    /// <summary>
    /// Application registration update request
    /// </summary>
    public class ApplicationRegistrationUpdateApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationRegistrationUpdateApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ApplicationRegistrationUpdateApiModel(ApplicationRegistrationUpdateModel model) {
            ProductUri = model.ProductUri;
            ApplicationName = model.ApplicationName;
            Locale = model.Locale;
            LocalizedNames = model.LocalizedNames;
            Certificate = model.Certificate;
            Capabilities = model.Capabilities;
            DiscoveryUrls = model.DiscoveryUrls;
            DiscoveryProfileUri = model.DiscoveryProfileUri;
            GatewayServerUri = model.GatewayServerUri;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public ApplicationRegistrationUpdateModel ToServiceModel() {
            return new ApplicationRegistrationUpdateModel {
                ApplicationName = ApplicationName,
                Locale = Locale,
                LocalizedNames = LocalizedNames,
                ProductUri = ProductUri,
                Certificate = Certificate,
                Capabilities = Capabilities,
                DiscoveryUrls = DiscoveryUrls,
                GatewayServerUri = GatewayServerUri,
                DiscoveryProfileUri = DiscoveryProfileUri
            };
        }

        /// <summary>
        /// Product uri
        /// </summary>
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
        /// Locale of default name - defaults to "en"
        /// </summary>
        [JsonProperty(PropertyName = "locale",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string Locale { get; set; }

        /// <summary>
        /// Localized names keyed off locale id.
        /// To remove entry, set value for locale id to null.
        /// </summary>
        [JsonProperty(PropertyName = "localizedNames",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public Dictionary<string, string> LocalizedNames { get; set; }

        /// <summary>
        /// Application public cert
        /// </summary>
        [JsonProperty(PropertyName = "certificate",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public byte[] Certificate { get; set; }

        /// <summary>
        /// Capabilities of the application
        /// </summary>
        [JsonProperty(PropertyName = "capabilities",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public HashSet<string> Capabilities { get; set; }

        /// <summary>
        /// Discovery urls of the application
        /// </summary>
        [JsonProperty(PropertyName = "discoveryUrls",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public HashSet<string> DiscoveryUrls { get; set; }

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
    }
}
