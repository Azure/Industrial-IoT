// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System;

    /// <summary>
    /// A monitored and published item
    /// </summary>
    public class PublishedItemModel {

        /// <summary>
        /// Variable node monitored
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Publishing interval to use
        /// </summary>
        public TimeSpan? PublishingInterval { get; set; }

        /// <summary>
        /// Sampling interval to use
        /// </summary>
        public TimeSpan? SamplingInterval { get; set; }
    }
}
