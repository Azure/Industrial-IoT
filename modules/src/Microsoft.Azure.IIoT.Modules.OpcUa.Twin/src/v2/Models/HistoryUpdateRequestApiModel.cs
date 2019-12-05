// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Request node history update
    /// </summary>
    public class HistoryUpdateRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public HistoryUpdateRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public HistoryUpdateRequestApiModel(HistoryUpdateRequestModel<JToken> model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Details = model.Details;
            NodeId = model.NodeId;
            BrowsePath = model.BrowsePath;
            Header = model.Header == null ? null :
                new RequestHeaderApiModel(model.Header);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public HistoryUpdateRequestModel<JToken> ToServiceModel() {
            return new HistoryUpdateRequestModel<JToken> {
                Details = Details,
                NodeId = NodeId,
                BrowsePath = BrowsePath,
                Header = Header?.ToServiceModel()
            };
        }

        /// <summary>
        /// Node to update
        /// </summary>
        [JsonProperty(PropertyName = "NodeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string NodeId { get; set; }

        /// <summary>
        /// An optional path from NodeId instance to
        /// an actual node.
        /// </summary>
        [JsonProperty(PropertyName = "BrowsePath",
            NullValueHandling = NullValueHandling.Ignore)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// The HistoryUpdateDetailsType extension object
        /// encoded as json Variant and containing the tunneled
        /// update request for the Historian server. The value
        /// is updated at edge using above node address.
        /// </summary>
        [JsonProperty(PropertyName = "Details")]
        public JToken Details { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "Header",
            NullValueHandling = NullValueHandling.Ignore)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
