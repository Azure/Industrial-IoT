// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Request node history update
    /// </summary>
    public class HistoryUpdateRequestApiModel<T> {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="convert"></param>
        /// <returns></returns>
        public HistoryUpdateRequestModel<S> ToServiceModel<S>(Func<T, S> convert) {
            return new HistoryUpdateRequestModel<S> {
                Details = convert(Details),
                BrowsePath = BrowsePath,
                NodeId = NodeId,
                Header = Header?.ToServiceModel()
            };
        }

        /// <summary>
        /// Node to update
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
        /// The HistoryUpdateDetailsType extension object
        /// encoded as json Variant and containing the tunneled
        /// update request for the Historian server. The value
        /// is updated at edge using above node address.
        /// </summary>
        [JsonProperty(PropertyName = "details")]
        [Required]
        public T Details { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "header",
            NullValueHandling = NullValueHandling.Ignore)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
