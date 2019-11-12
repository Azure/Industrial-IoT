// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// A monitored and published item
    /// </summary>
    public class PublishedItemApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishedItemApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishedItemApiModel(PublishedItemModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            NodeId = model.NodeId;
            SamplingInterval = model.SamplingInterval;
            PublishingInterval = model.PublishingInterval;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishedItemModel ToServiceModel() {
            return new PublishedItemModel {
                NodeId = NodeId,
                SamplingInterval = SamplingInterval,
                PublishingInterval = PublishingInterval
            };
        }

        /// <summary>
        /// Node to monitor
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        [Required]
        public string NodeId { get; set; }

        /// <summary>
        /// Publishing interval to use
        /// </summary>
        [JsonProperty(PropertyName = "publishingInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? PublishingInterval { get; set; }

        /// <summary>
        /// Sampling interval to use
        /// </summary>
        [JsonProperty(PropertyName = "samplingInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? SamplingInterval { get; set; }
    }
}
