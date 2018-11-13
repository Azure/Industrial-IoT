// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {

    /// <summary>
    /// A monitored and published node
    /// </summary>
    public class PublishedNodeModel {

        /// <summary>
        /// Node to monitor
        /// </summary>
        public string NodeId { get; set; }

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
