// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Publisher registration model
    /// </summary>
    public class PublisherApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublisherApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public PublisherApiModel(PublisherModel model) {
            Id = model.Id;
            SiteId = model.SiteId;
            Certificate = model.Certificate;
            Configuration = model.Configuration == null ? null :
                new PublisherConfigApiModel(model.Configuration);
            LogLevel = model.LogLevel;
            OutOfSync = model.OutOfSync;
            Connected = model.Connected;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public PublisherModel ToServiceModel() {
            return new PublisherModel {
                Id = Id,
                SiteId = SiteId,
                LogLevel = LogLevel,
                Certificate = Certificate,
                Configuration = Configuration?.ToServiceModel(),
                OutOfSync = OutOfSync,
                Connected = Connected
            };
        }

        /// <summary>
        /// Publisher id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Site of the publisher
        /// </summary>
        [JsonProperty(PropertyName = "siteId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SiteId { get; set; }

        /// <summary>
        /// Publisher public client cert
        /// </summary>
        [JsonProperty(PropertyName = "certificate",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public byte[] Certificate { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        [JsonProperty(PropertyName = "logLevel",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(TraceLogLevel.Information)]
        public TraceLogLevel? LogLevel { get; set; }

        /// <summary>
        /// Publisher agent configuration
        /// </summary>
        [JsonProperty(PropertyName = "configuration",
            NullValueHandling = NullValueHandling.Ignore)]
        public PublisherConfigApiModel Configuration { get; set; }

        /// <summary>
        /// Whether the registration is out of sync between
        /// client (module) and server (service) (default: false).
        /// </summary>
        [JsonProperty(PropertyName = "outOfSync",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Whether publisher is connected on this registration
        /// </summary>
        [JsonProperty(PropertyName = "connected",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? Connected { get; set; }
    }
}
