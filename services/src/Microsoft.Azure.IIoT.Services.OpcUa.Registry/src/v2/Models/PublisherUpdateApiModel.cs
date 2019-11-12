// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;

    /// <summary>
    /// Publisher registration update request
    /// </summary>
    public class PublisherUpdateApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublisherUpdateApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public PublisherUpdateApiModel(PublisherUpdateModel model) {
            SiteId = model.SiteId;
            LogLevel = model.LogLevel;
            Configuration = model.Configuration == null ? null :
                new PublisherConfigApiModel(model.Configuration);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public PublisherUpdateModel ToServiceModel() {
            return new PublisherUpdateModel {
                SiteId = SiteId,
                LogLevel = LogLevel,
                Configuration = Configuration?.ToServiceModel()
            };
        }

        /// <summary>
        /// Site of the publisher
        /// </summary>
        [JsonProperty(PropertyName = "siteId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SiteId { get; set; }

        /// <summary>
        /// Publisher discovery configuration
        /// </summary>
        [JsonProperty(PropertyName = "configuration",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public PublisherConfigApiModel Configuration { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        [JsonProperty(PropertyName = "logLevel",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public TraceLogLevel? LogLevel { get; set; }
    }
}
