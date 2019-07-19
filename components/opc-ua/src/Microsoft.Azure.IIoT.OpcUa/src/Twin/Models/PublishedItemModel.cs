// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {

    /// <summary>
    /// A monitored and published item
    /// </summary>
    public class PublishedItemModel {

        /// <summary>
        /// Variable node to monitor
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// An optional path from NodeId instance to
        /// an actual node.
        /// </summary>
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Attribute to monitor
        /// </summary>
        public NodeAttribute? NodeAttribute { get; set; }

        /// <summary>
        /// Publishing interval to use
        /// </summary>
        public int? PublishingInterval { get; set; }

        /// <summary>
        /// Sampling interval to use
        /// </summary>
        public int? SamplingInterval { get; set; }
    }
}
