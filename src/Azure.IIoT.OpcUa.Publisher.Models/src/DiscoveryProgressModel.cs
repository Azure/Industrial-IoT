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
    /// Discovery progress
    /// </summary>
    [DataContract]
    public sealed record class DiscoveryProgressModel
    {
        /// <summary>
        /// Id of discovery request
        /// </summary>
        [DataMember(Name = "requestId", Order = 0,
            EmitDefaultValue = false)]
        public string? RequestId { get; set; }

        /// <summary>
        /// Event type
        /// </summary>
        [DataMember(Name = "eventType", Order = 1)]
        public DiscoveryProgressType EventType { get; set; }

        /// <summary>
        /// Discoverer that registered the application
        /// </summary>
        [DataMember(Name = "discovererId", Order = 2,
            EmitDefaultValue = false)]
        public string? DiscovererId { get; set; }

        /// <summary>
        /// Additional request information as per event
        /// </summary>
        [DataMember(Name = "requestDetails", Order = 3,
            EmitDefaultValue = false)]
        public IReadOnlyDictionary<string, string>? RequestDetails { get; set; }

        /// <summary>
        /// Timestamp of the message
        /// </summary>
        [DataMember(Name = "timeStamp", Order = 4)]
        public DateTimeOffset TimeStamp { get; set; }

        /// <summary>
        /// Number of workers running
        /// </summary>
        [DataMember(Name = "workers", Order = 5,
            EmitDefaultValue = false)]
        public int? Workers { get; set; }

        /// <summary>
        /// Progress
        /// </summary>
        [DataMember(Name = "progress", Order = 6,
            EmitDefaultValue = false)]
        public int? Progress { get; set; }

        /// <summary>
        /// Total
        /// </summary>
        [DataMember(Name = "total", Order = 7,
            EmitDefaultValue = false)]
        public int? Total { get; set; }

        /// <summary>
        /// Number of items discovered
        /// </summary>
        [DataMember(Name = "discovered", Order = 8,
            EmitDefaultValue = false)]
        public int? Discovered { get; set; }

        /// <summary>
        /// Discovery result
        /// </summary>
        [DataMember(Name = "result", Order = 9,
            EmitDefaultValue = false)]
        public string? Result { get; set; }

        /// <summary>
        /// Discovery result details
        /// </summary>
        [DataMember(Name = "resultDetails", Order = 10,
            EmitDefaultValue = false)]
        public IReadOnlyDictionary<string, string>? ResultDetails { get; set; }

        /// <summary>
        /// Discovery request
        /// </summary>
        [DataMember(Name = "request", Order = 11,
            EmitDefaultValue = false)]
        public required DiscoveryRequestModel Request { get; set; }
    }
}
