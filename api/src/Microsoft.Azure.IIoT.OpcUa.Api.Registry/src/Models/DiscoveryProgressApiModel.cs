// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Discovery progress
    /// </summary>
    [DataContract]
    public class DiscoveryProgressApiModel {

        /// <summary>
        /// Id of discovery request
        /// </summary>
        [DataMember(Name = "requestId",
            EmitDefaultValue = false)]
        public string RequestId { get; set; }

        /// <summary>
        /// Event type
        /// </summary>
        [DataMember(Name = "eventType")]
        public DiscoveryProgressType EventType { get; set; }

        /// <summary>
        /// Discoverer that registered the application
        /// </summary>
        [DataMember(Name = "discovererId",
            EmitDefaultValue = false)]
        public string DiscovererId { get; set; }

        /// <summary>
        /// Additional request information as per event
        /// </summary>
        [DataMember(Name = "requestDetails",
            EmitDefaultValue = false)]
        public Dictionary<string, string> RequestDetails { get; set; }

        /// <summary>
        /// Timestamp of the message
        /// </summary>
        [DataMember(Name = "timeStamp")]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Number of workers running
        /// </summary>
        [DataMember(Name = "workers",
            EmitDefaultValue = false)]
        public int? Workers { get; set; }

        /// <summary>
        /// Progress
        /// </summary>
        [DataMember(Name = "progress",
            EmitDefaultValue = false)]
        public int? Progress { get; set; }

        /// <summary>
        /// Total
        /// </summary>
        [DataMember(Name = "total",
            EmitDefaultValue = false)]
        public int? Total { get; set; }

        /// <summary>
        /// Number of items discovered
        /// </summary>
        [DataMember(Name = "discovered",
            EmitDefaultValue = false)]
        public int? Discovered { get; set; }

        /// <summary>
        /// Discovery result
        /// </summary>
        [DataMember(Name = "result",
            EmitDefaultValue = false)]
        public string Result { get; set; }

        /// <summary>
        /// Discovery result details
        /// </summary>
        [DataMember(Name = "resultDetails",
            EmitDefaultValue = false)]
        public Dictionary<string, string> ResultDetails { get; set; }
    }
}
