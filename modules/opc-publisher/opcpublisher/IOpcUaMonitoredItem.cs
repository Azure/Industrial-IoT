using Opc.Ua.Client;

namespace OpcPublisher
{
    using Opc.Ua;

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
