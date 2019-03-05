// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Request node history read
    /// </summary>
    public class HistoryReadRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public HistoryReadRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public HistoryReadRequestApiModel(HistoryReadRequestModel<JToken> model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            NodeId = model.NodeId;
            BrowsePath = model.BrowsePath;
            IndexRange = model.IndexRange;
            Details = model.Details;
            Header = model.Header == null ? null :
                new RequestHeaderApiModel(model.Header);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public HistoryReadRequestModel<JToken> ToServiceModel() {
            return new HistoryReadRequestModel<JToken> {
                NodeId = NodeId,
                BrowsePath = BrowsePath,
                IndexRange = IndexRange,
                Details = Details,
                Header = Header?.ToServiceModel()
            };
        }

        /// <summary>
        /// Node to read from (mandatory)
        /// </summary>
        [JsonProperty(PropertyName = "NodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// An optional path from NodeId instance to
        /// an actual node.
        /// </summary>
        [JsonProperty(PropertyName = "BrowsePath",
            NullValueHandling = NullValueHandling.Ignore)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// The HistoryReadDetailsType extension object
        /// encoded in json and containing the tunneled
        /// Historian reader request.
        /// </summary>
        [JsonProperty(PropertyName = "Details")]
        public JToken Details { get; set; }

        /// <summary>
        /// Index range to read, e.g. 1:2,0:1 for 2 slices
        /// out of a matrix or 0:1 for the first item in
        /// an array, string or bytestring.
        /// See 7.22 of part 4: NumericRange.
        /// </summary>
        [JsonProperty(PropertyName = "IndexRange",
            NullValueHandling = NullValueHandling.Ignore)]
        public string IndexRange { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "Header",
            NullValueHandling = NullValueHandling.Ignore)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
