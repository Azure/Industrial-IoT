// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;

    /// <summary>
    /// Request node history read
    /// </summary>
    [DataContract]
    public class HistoryReadRequestApiModel<T> {

        /// <summary>
        /// Node to read from (mandatory)
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
        /// The HistoryReadDetailsType extension object
        /// encoded in json and containing the tunneled
        /// Historian reader request.
        /// </summary>
        [DataMember(Name = "details", Order = 2)]
        public T Details { get; set; }

        /// <summary>
        /// Index range to read, e.g. 1:2,0:1 for 2 slices
        /// out of a matrix or 0:1 for the first item in
        /// an array, string or bytestring.
        /// See 7.22 of part 4: NumericRange.
        /// </summary>
        [DataMember(Name = "indexRange", Order = 3,
            EmitDefaultValue = false)]
        public string IndexRange { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 4,
            EmitDefaultValue = false)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
