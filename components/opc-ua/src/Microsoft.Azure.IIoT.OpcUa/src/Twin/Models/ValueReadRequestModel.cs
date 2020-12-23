// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System;

    /// <summary>
    /// Request node value read
    /// </summary>
    public class ValueReadRequestModel {

        /// <summary>
        /// Node to read from (mandatory)
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// An optional path from NodeId instance to
        /// an actual node.
        /// </summary>
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Index range to read, e.g. 1:2,0:1 for 2 slices
        /// out of a matrix or 0:1 for the first item in
        /// an array, string or bytestring.
        /// See 7.22 of part 4: NumericRange.
        /// </summary>
        public string IndexRange { get; set; }

        /// <summary>
        /// Max age of value
        /// </summary>
        public TimeSpan? MaxAge { get; set; }

        /// <summary>
        /// Optional header
        /// </summary>
        public RequestHeaderModel Header { get; set; }
    }
}
