// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Discovery progress
    /// </summary>
    public class DiscoveryProgressModel {

        /// <summary>
        /// Message source
        /// </summary>
        public string SupervisorId { get; set; }

        /// <summary>
        /// Original request
        /// </summary>
        public DiscoveryRequestModel Request { get; set; }

        /// <summary>
        /// Additional request information as per event
        /// </summary>
        public JToken RequestDetails { get; set; }

        /// <summary>
        /// Timestamp of progress event
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Type of event
        /// </summary>
        public DiscoveryProgressType EventType { get; set; }

        /// <summary>
        /// Number of workers running
        /// </summary>
        public int? Workers { get; set; }

        /// <summary>
        /// Total
        /// </summary>
        public int? Total { get; set; }

        /// <summary>
        /// Progress
        /// </summary>
        public int? Progress { get; set; }

        /// <summary>
        /// Number of items found
        /// </summary>
        public int? Discovered { get; set; }

        /// <summary>
        /// Discovery result
        /// </summary>
        public JToken Result { get; set; }
    }
}
