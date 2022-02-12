// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestModels {
    using System;

    /// <summary>
    /// Model for a diagnostic info response.
    /// </summary>
    public class DiagnosticInfoLegacyModel {

        public DateTime PublisherStartTime { get; set; }

        public int NumberOfOpcSessionsConfigured { get; set; }

        public int NumberOfOpcSessionsConnected { get; set; }

        public int NumberOfOpcSubscriptionsConfigured { get; set; }

        public int NumberOfOpcSubscriptionsConnected { get; set; }

        public int NumberOfOpcMonitoredItemsConfigured { get; set; }

        public int NumberOfOpcMonitoredItemsMonitored { get; set; }

        public int NumberOfOpcMonitoredItemsToRemove { get; set; }

        public int MonitoredItemsQueueCapacity { get; set; }

        public long MonitoredItemsQueueCount { get; set; }

        public long EnqueueCount { get; set; }

        public long EnqueueFailureCount { get; set; }

        public long NumberOfEvents { get; set; }

        public long SentMessages { get; set; }

        public DateTime SentLastTime { get; set; }

        public long SentBytes { get; set; }

        public long FailedMessages { get; set; }

        public long TooLargeCount { get; set; }

        public long MissedSendIntervalCount { get; set; }

        public long WorkingSetMB { get; set; }

        public int DefaultSendIntervalSeconds { get; set; }

        public uint HubMessageSize { get; set; }

        public int HubProtocol { get; set; }
    }
}

