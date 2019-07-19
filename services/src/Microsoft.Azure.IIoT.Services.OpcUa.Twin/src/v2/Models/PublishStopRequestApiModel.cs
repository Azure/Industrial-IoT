// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Unpublish request
    /// </summary>
    public class PublishStopRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishStopRequestApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishStopRequestApiModel(PublishStopRequestModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            NodeId = model.NodeId;
            BrowsePath = model.BrowsePath;
            NodeAttribute = model.NodeAttribute;
            Diagnostics = model.Diagnostics == null ? null :
                new DiagnosticsApiModel(model.Diagnostics);
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishStopRequestModel ToServiceModel() {
            return new PublishStopRequestModel {
                NodeId = NodeId,
                BrowsePath = BrowsePath,
                NodeAttribute = NodeAttribute,
                Diagnostics = Diagnostics?.ToServiceModel()
            };
        }

        /// <summary>
        /// Node of published item to unpublish
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        [Required]
        public string NodeId { get; set; }

        /// <summary>
        /// An optional path from NodeId instance to
        /// the actual node.
        /// </summary>
        [JsonProperty(PropertyName = "browsePath",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Attribute of item to unpublish
        /// </summary>
        [JsonProperty(PropertyName = "nodeAttribute",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeAttribute? NodeAttribute { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        [JsonProperty(PropertyName = "diagnostics",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public DiagnosticsApiModel Diagnostics { get; set; }
    }
}
