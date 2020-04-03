// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Request node history update
    /// </summary>
    [DataContract]
    public class HistoryUpdateRequestApiModel<T> {

        /// <summary>
        /// Node to update
        /// </summary>
        [DataMember(Name = "nodeId", Order = 0)]
        public string NodeId { get; set; }

        /// <summary>
        /// An optional path from NodeId instance to
        /// the actual node.
        /// </summary>
        [DataMember(Name = "browsePath", Order = 1,
            EmitDefaultValue = false)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// The HistoryUpdateDetailsType extension object
        /// encoded as json Variant and containing the tunneled
        /// update request for the Historian server. The value
        /// is updated at edge using above node address.
        /// </summary>
        [DataMember(Name = "details", Order = 2)]
        [Required]
        public T Details { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 3,
            EmitDefaultValue = false)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
