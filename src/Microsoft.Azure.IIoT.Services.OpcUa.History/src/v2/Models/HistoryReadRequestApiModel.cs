// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Request node history read
    /// </summary>
    public class HistoryReadRequestApiModel<T> {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="convert"></param>
        /// <returns></returns>
        public HistoryReadRequestModel<S> ToServiceModel<S>(Func<T, S> convert) {
            return new HistoryReadRequestModel<S> {
                Details = convert(Details),
                BrowsePath = BrowsePath,
                NodeId = NodeId,
                IndexRange = IndexRange,
                Header = Header?.ToServiceModel()
            };
        }

        /// <summary>
        /// Node to read from (mandatory)
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// An optional path from NodeId instance to
        /// the actual node.
        /// </summary>
        [JsonProperty(PropertyName = "browsePath",
            NullValueHandling = NullValueHandling.Ignore)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// The HistoryReadDetailsType extension object
        /// encoded in json and containing the tunneled
        /// Historian reader request.
        /// </summary>
        [JsonProperty(PropertyName = "details")]
        public T Details { get; set; }

        /// <summary>
        /// Index range to read, e.g. 1:2,0:1 for 2 slices
        /// out of a matrix or 0:1 for the first item in
        /// an array, string or bytestring.
        /// See 7.22 of part 4: NumericRange.
        /// </summary>
        [JsonProperty(PropertyName = "indexRange",
            NullValueHandling = NullValueHandling.Ignore)]
        public string IndexRange { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "header",
            NullValueHandling = NullValueHandling.Ignore)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
