// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

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
            Id = model.Id;
            ProductUri = model.ProductUri;
            ApplicationName = model.ApplicationName;
            Certificate = model.Certificate;
            Capabilities = model.Capabilities;
            DiscoveryUrls = model.DiscoveryUrls;
            DiscoveryProfileUri = model.DiscoveryProfileUri;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public ApplicationRegistrationUpdateModel ToServiceModel() {
            return new ApplicationRegistrationUpdateModel {
                Id = Id,
                ApplicationName = ApplicationName,
                ProductUri = ProductUri,
                Certificate = Certificate,
                Capabilities = Capabilities,
                DiscoveryUrls = DiscoveryUrls,
                DiscoveryProfileUri = DiscoveryProfileUri
            };
        }

        /// <summary>
        /// Identifier of the application to patch
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        [JsonProperty(PropertyName = "productUri",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string ProductUri { get; set; }

        /// <summary>
        /// Application name
        /// </summary>
        [JsonProperty(PropertyName = "applicationName",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string ApplicationName { get; set; }

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
        public List<string> DiscoveryUrls { get; set; }

        /// <summary>
        /// Discovery profile uri
        /// </summary>
        [JsonProperty(PropertyName = "discoveryProfileUri",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string DiscoveryProfileUri { get; set; }
    }
}
