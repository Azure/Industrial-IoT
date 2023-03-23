// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Request node value read
    /// </summary>
    [DataContract]
    public sealed record class ValueReadRequestModel
    {
        /// <summary>
        /// Node to read from (mandatory)
        /// </summary>
        [DataMember(Name = "nodeId", Order = 0,
            EmitDefaultValue = false)]
        public string? NodeId { get; set; }

        /// <summary>
        /// An optional path from NodeId instance to
        /// an actual node.
        /// </summary>
        [DataMember(Name = "browsePath", Order = 1,
            EmitDefaultValue = false)]
        public IReadOnlyList<string>? BrowsePath { get; set; }

        /// <summary>
        /// Index range to read, e.g. 1:2,0:1 for 2 slices
        /// out of a matrix or 0:1 for the first item in
        /// an array, string or bytestring.
        /// See 7.22 of part 4: NumericRange.
        /// </summary>
        [DataMember(Name = "indexRange", Order = 2,
            EmitDefaultValue = false)]
        public string? IndexRange { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 3,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; set; }

        /// <summary>
        /// Maximum age of the value to be read in milliseconds.
        /// The age of the value is based on the difference
        /// between the ServerTimestamp and the time when
        /// the Server starts processing the request.
        /// If not supplied, the Server shall attempt to read
        /// a new value from the data source.
        /// </summary>
        [DataMember(Name = "maxAge", Order = 4,
            EmitDefaultValue = false)]
        public TimeSpan? MaxAge { get; set; }

        /// <summary>
        /// Decide what timestamps to return.
        /// </summary>
        [DataMember(Name = "timestampsToReturn", Order = 5,
            EmitDefaultValue = false)]
        public TimestampsToReturn? TimestampsToReturn { get; set; }
    }
}
