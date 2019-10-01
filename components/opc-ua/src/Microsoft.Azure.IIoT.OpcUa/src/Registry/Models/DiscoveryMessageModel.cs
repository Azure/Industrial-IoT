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
    public class DiscoveryMessageModel {

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
        /// Timestamp of the discovery sweep.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Index in the batch with same timestamp.
        /// </summary>
        public DiscoveryMessageType Event { get; set; }

        /// <summary>
        /// Number of workers running
        /// </summary>
        public int? Workers { get; set; }

        /// <summary>
        /// Progress
        /// </summary>
        public int? Progress { get; set; }

        /// <summary>
        /// Number of items discovered
        /// </summary>
        public int? Discovered { get; set; }

        /// <summary>
        /// Discovery result
        /// </summary>
        public JToken Result { get; set; }
    }
}
