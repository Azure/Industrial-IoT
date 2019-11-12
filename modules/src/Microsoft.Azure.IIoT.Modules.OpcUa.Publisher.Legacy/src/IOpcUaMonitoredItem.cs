// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher
{
    using Opc.Ua;
    using Opc.Ua.Client;

    /// <summary>
    /// Interface to encapsulate OPC UA monitored item API.
    /// </summary>
    public interface IOpcUaMonitoredItem
    {
        MonitoredItem MonitoredItem { get; }

        uint AttributeId { get; set; }

        bool DiscardOldest { get; set; }

        string DisplayName { get; set; }

        MonitoringMode MonitoringMode { get; set; }

        uint QueueSize { get; set; }

        NodeId StartNodeId { get; set; }

        int SamplingInterval { get; set; }

        event MonitoredItemNotificationEventHandler Notification;
    }
}
