// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
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
            Header = model.Header == null ? null :
                new RequestHeaderApiModel(model.Header);
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishStopRequestModel ToServiceModel() {
            return new PublishStopRequestModel {
                NodeId = NodeId,
                Header = Header?.ToServiceModel()
            };
        }

        /// <summary>
        /// Node of published item to unpublish
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        [Required]
        public string NodeId { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "header",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
