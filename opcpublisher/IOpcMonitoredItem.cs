using Opc.Ua.Client;

namespace OpcPublisher
{
    using Opc.Ua;
    using static OpcMonitoredItem;

    /// <summary>
    /// Interface used to pass data from the MonitoredItem notification to the hub message processing.
    /// </summary>
    public interface IMessageData
    {
        /// <summary>
        /// The endpoint URL the monitored item belongs to.
        /// </summary>
        string EndpointUrl { get; set; }

        /// <summary>
        /// The OPC UA NodeId of the monitored item.
        /// </summary>
        string NodeId { get; set; }

        /// <summary>
        /// The Application URI of the OPC UA server the node belongs to.
        /// </summary>
        string ApplicationUri { get; set; }

        /// <summary>
        /// The display name of the node.
        /// </summary>
        string DisplayName { get; set; }

        /// <summary>
        /// The value of the node.
        /// </summary>
        string Value { get; set; }

        /// <summary>
        /// The OPC UA source timestamp the value was seen.
        /// </summary>
        string SourceTimestamp { get; set; }

        /// <summary>
        /// The OPC UA status code of the value.
        /// </summary>
        uint? StatusCode { get; set; }

        /// <summary>
        /// The OPC UA status of the value.
        /// </summary>
        string Status { get; set; }

        /// <summary>
        /// Flag if the encoding of the value should preserve quotes.
        /// </summary>
        bool PreserveValueQuotes { get; set; }

        /// <summary>
        /// Apply the patterns specified in the telemetry configuration on the message data fields.
        /// </summary>
        void ApplyPatterns(IEndpointTelemetryConfigurationModel telemetryConfiguration);
    }

    /// <summary>
    /// Interface to manage the OPC monitored items, which are the nodes we need to publish.
    /// </summary>
    public interface IOpcMonitoredItem
    {
        /// <summary>
        /// The display name to use in the telemetry event for the monitored item.
        /// </summary>
        string DisplayName { get; set; }

        /// <summary>
        /// Flag to signal that the display name was requested by the node configuration.
        /// </summary>
        bool DisplayNameFromConfiguration { get; set; }

        /// <summary>
        /// The state of the monitored item.
        /// </summary>
        OpcMonitoredItemState State { get; set; }

        /// <summary>
        /// The OPC UA attributes to use when monitoring the node.
        /// </summary>
        uint AttributeId { get; set; }

        /// <summary>
        /// The OPC UA monitoring mode to use when monitoring the node.
        /// </summary>
        MonitoringMode MonitoringMode { get; set; }

        /// <summary>
        /// The requested sampling interval to be used for the node.
        /// </summary>
        int RequestedSamplingInterval { get; set; }

        /// <summary>
        /// The actual sampling interval used for the node.
        /// </summary>
        double SamplingInterval { get; set; }

        /// <summary>
        /// Flag to signal that the sampling interval was requested by the node configuration.
        /// </summary>
        bool RequestedSamplingIntervalFromConfiguration { get; set; }

        /// <summary>
        /// The OPC UA queue size to use for the node monitoring.
        /// </summary>
        uint QueueSize { get; set; }

        /// <summary>
        /// A flag to control the queue behaviour of the OPC UA stack for the node.
        /// </summary>
        bool DiscardOldest { get; set; }

        /// <summary>
        /// The event handler of the node in case the OPC UA stack detected a change.
        /// </summary>
        MonitoredItemNotificationEventHandler Notification { get; set; }

        /// <summary>
        /// The endpoint URL of the OPC UA server this nodes is residing on.
        /// </summary>
        string EndpointUrl { get; set; }

        /// <summary>
        /// The OPC UA stacks monitored item object.
        /// </summary>
        IOpcUaMonitoredItem OpcUaClientMonitoredItem { get; set; }

        /// <summary>
        /// The OPC UA identifier of the node in NodeId ("ns=") syntax.
        /// </summary>
        NodeId ConfigNodeId { get; set; }

        /// <summary>
        /// The OPC UA identifier of the node in ExpandedNodeId ("nsu=") syntax.
        /// </summary>
        ExpandedNodeId ConfigExpandedNodeId { get; set; }

        /// <summary>
        /// The OPC UA identifier of the node as it was configured.
        /// </summary>
        string OriginalId { get; set; }

        /// <summary>
        /// Identifies the configuration type of the node.
        /// </summary>
        OpcMonitoredItemConfigurationType ConfigType { get; set; }

        /// <summary>
        /// Checks if the monitored item does monitor the node described by the given objects.
        /// </summary>
        bool IsMonitoringThisNode(NodeId nodeId, ExpandedNodeId expandedNodeId, NamespaceTable namespaceTable);

        /// <summary>
        /// The notification that the data for a monitored item has changed on an OPC UA server.
        /// </summary>
        void MonitoredItemNotificationEventHandler(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e);
    }
}
