// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models
{
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Model for a diagnostic info response.
    /// </summary>
    public class DiagnosticInfoMethodResponseModel
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTime PublisherStartTime { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfOpcSessionsConfigured { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfOpcSessionsConnected { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfOpcSubscriptionsConfigured { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfOpcSubscriptionsConnected { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfOpcMonitoredItemsConfigured { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfOpcMonitoredItemsMonitored { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int NumberOfOpcMonitoredItemsToRemove { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int MonitoredItemsQueueCapacity { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long MonitoredItemsQueueCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long EnqueueCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long EnqueueFailureCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long NumberOfEvents { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long SentMessages { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTime SentLastTime { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long SentBytes { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long FailedMessages { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long TooLargeCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long MissedSendIntervalCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public long WorkingSetMB { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int DefaultSendIntervalSeconds { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public uint HubMessageSize { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public TransportType HubProtocol { get; set; }
    }
}
