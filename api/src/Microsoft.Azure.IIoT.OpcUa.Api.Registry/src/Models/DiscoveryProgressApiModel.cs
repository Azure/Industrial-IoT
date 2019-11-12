// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Discovery progress event type
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DiscoveryProgressType {

        /// <summary>
        /// Discovery Pending
        /// </summary>
        Pending,

        /// <summary>
        /// Discovery run started
        /// </summary>
        Started,

        /// <summary>
        /// Discovery was cancelled
        /// </summary>
        Cancelled,

        /// <summary>
        /// Discovery resulted in error
        /// </summary>
        Error,

        /// <summary>
        /// Discovery finished
        /// </summary>
        Finished,

        /// <summary>
        /// Network scanning started
        /// </summary>
        NetworkScanStarted,

        /// <summary>
        /// Network scanning result
        /// </summary>
        NetworkScanResult,

        /// <summary>
        /// Network scan progress
        /// </summary>
        NetworkScanProgress,

        /// <summary>
        /// Network scan finished
        /// </summary>
        NetworkScanFinished,

        /// <summary>
        /// Port scan started
        /// </summary>
        PortScanStarted,

        /// <summary>
        /// Port scan result
        /// </summary>
        PortScanResult,

        /// <summary>
        /// Port scan progress
        /// </summary>
        PortScanProgress,

        /// <summary>
        /// Port scan finished
        /// </summary>
        PortScanFinished,

        /// <summary>
        /// Server discovery started
        /// </summary>
        ServerDiscoveryStarted,

        /// <summary>
        /// Endpoint discovery started
        /// </summary>
        EndpointsDiscoveryStarted,

        /// <summary>
        /// Endpoint discovery finished
        /// </summary>
        EndpointsDiscoveryFinished,

        /// <summary>
        /// Server discovery finished
        /// </summary>
        ServerDiscoveryFinished,
    }

    /// <summary>
    /// Discovery progress
    /// </summary>
    public class DiscoveryProgressApiModel {

        /// <summary>
        /// Id of discovery request
        /// </summary>
        [JsonProperty(PropertyName = "requestId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string RequestId { get; set; }

        /// <summary>
        /// Event type
        /// </summary>
        [JsonProperty(PropertyName = "eventType")]
        public DiscoveryProgressType EventType { get; set; }

        /// <summary>
        /// Source of message
        /// </summary>
        [JsonProperty(PropertyName = "supervisorId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SupervisorId { get; set; }

        /// <summary>
        /// Additional request information as per event
        /// </summary>
        [JsonProperty(PropertyName = "requestDetails",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken RequestDetails { get; set; }

        /// <summary>
        /// Timestamp of the message
        /// </summary>
        [JsonProperty(PropertyName = "timeStamp")]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Number of workers running
        /// </summary>
        [JsonProperty(PropertyName = "workers",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? Workers { get; set; }

        /// <summary>
        /// Progress
        /// </summary>
        [JsonProperty(PropertyName = "progress",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? Progress { get; set; }

        /// <summary>
        /// Total
        /// </summary>
        [JsonProperty(PropertyName = "total",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? Total { get; set; }

        /// <summary>
        /// Number of items discovered
        /// </summary>
        [JsonProperty(PropertyName = "discovered",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? Discovered { get; set; }

        /// <summary>
        /// Discovery result
        /// </summary>
        [JsonProperty(PropertyName = "result",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Result { get; set; }
    }
}
