// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Model for a diagnostic info.
    /// </summary>
    [DataContract]
    public record class WriterGroupDiagnosticModel
    {
        /// <summary>
        /// Publisher version string
        /// </summary>
        [DataMember(Name = "PublisherVersion", Order = 99,
            EmitDefaultValue = true)]
        public string? PublisherVersion { get; set; }

        /// <summary>
        /// Writer group name
        /// </summary>
        [DataMember(Name = "WriterGroupName", Order = 98,
            EmitDefaultValue = true)]
        public string? WriterGroupName { get; set; }

        /// <summary>
        /// Timestamp for this diagnostics information
        /// </summary>
        [DataMember(Name = "Timestamp", Order = 0,
            EmitDefaultValue = true)]
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Ingestion start
        /// </summary>
        [DataMember(Name = "IngestionStart", Order = 1,
            EmitDefaultValue = true)]
        public DateTimeOffset IngestionStart { get; set; }

        /// <summary>
        /// Ingestion duration
        /// </summary>
        [DataMember(Name = "IngestionDuration", Order = 2,
            EmitDefaultValue = true)]
        public TimeSpan IngestionDuration { get; set; }

        /// <summary>
        /// SentMessagesPerSec
        /// </summary>
        [DataMember(Name = "SentMessagesPerSec", Order = 3,
            EmitDefaultValue = true)]
        public double SentMessagesPerSec { get; set; }

        /// <summary>
        /// IngressDataChanges
        /// </summary>
        [DataMember(Name = "IngressDataChanges", Order = 4,
            EmitDefaultValue = true)]
        public long IngressDataChanges { get; set; }

        /// <summary>
        /// IngressValueChanges
        /// </summary>
        [DataMember(Name = "IngressValueChanges", Order = 5,
            EmitDefaultValue = true)]
        public long IngressValueChanges { get; set; }

        /// <summary>
        /// Number of heartbeats of the total values
        /// </summary>
        [DataMember(Name = "IngressHeartbeats", Order = 6,
            EmitDefaultValue = true)]
        public long IngressHeartbeats { get; set; }

        /// <summary>
        /// Number of cyclic reads of all sampled values
        /// </summary>
        [DataMember(Name = "IngressCyclicReads", Order = 7,
            EmitDefaultValue = true)]
        public long IngressCyclicReads { get; set; }

        /// <summary>
        /// Data changes received in the last minute
        /// </summary>
        [DataMember(Name = "IngressDataChangesInLastMinute", Order = 8,
            EmitDefaultValue = true)]
        public long IngressDataChangesInLastMinute { get; set; }

        /// <summary>
        /// Value changes received in the last minute
        /// </summary>
        [DataMember(Name = "IngressValueChangesInLastMinute", Order = 9,
            EmitDefaultValue = true)]
        public long IngressValueChangesInLastMinute { get; set; }

        /// <summary>
        /// IngressBatchBlockBufferSize
        /// </summary>
        [DataMember(Name = "IngressBatchBlockBufferSize", Order = 10,
            EmitDefaultValue = true)]
        public long IngressBatchBlockBufferSize { get; set; }

        /// <summary>
        /// EncodingBlockInputSize
        /// </summary>
        [DataMember(Name = "EncodingBlockInputSize", Order = 11,
            EmitDefaultValue = true)]
        public long EncodingBlockInputSize { get; set; }

        /// <summary>
        /// EncodingBlockOutputSize
        /// </summary>
        [DataMember(Name = "EncodingBlockOutputSize", Order = 12,
            EmitDefaultValue = true)]
        public long EncodingBlockOutputSize { get; set; }

        /// <summary>
        /// EncoderNotificationsProcessed
        /// </summary>
        [DataMember(Name = "EncoderNotificationsProcessed", Order = 13,
            EmitDefaultValue = true)]
        public long EncoderNotificationsProcessed { get; set; }

        /// <summary>
        /// EncoderNotificationsDropped
        /// </summary>
        [DataMember(Name = "EncoderNotificationsDropped", Order = 14,
            EmitDefaultValue = true)]
        public long EncoderNotificationsDropped { get; set; }

        /// <summary>
        /// EncoderIoTMessagesProcessed
        /// </summary>
        [DataMember(Name = "EncoderIoTMessagesProcessed", Order = 15,
            EmitDefaultValue = true)]
        public long EncoderIoTMessagesProcessed { get; set; }

        /// <summary>
        /// EncoderAvgNotificationsMessage
        /// </summary>
        [DataMember(Name = "EncoderAvgNotificationsMessage", Order = 16,
            EmitDefaultValue = true)]
        public double EncoderAvgNotificationsMessage { get; set; }

        /// <summary>
        /// EncoderAvgIoTMessageBodySize
        /// </summary>
        [DataMember(Name = "EncoderAvgIoTMessageBodySize", Order = 17,
            EmitDefaultValue = true)]
        public double EncoderAvgIoTMessageBodySize { get; set; }

        /// <summary>
        /// EncoderAvgIoTChunkUsage
        /// </summary>
        [DataMember(Name = "EncoderAvgIoTChunkUsage", Order = 18,
            EmitDefaultValue = true)]
        public double EncoderAvgIoTChunkUsage { get; set; }

        /// <summary>
        /// EstimatedIoTChunksPerDay
        /// </summary>
        [DataMember(Name = "EstimatedIoTChunksPerDay", Order = 19,
            EmitDefaultValue = true)]
        public double EstimatedIoTChunksPerDay { get; set; }

        /// <summary>
        /// OutgressInputBufferCount
        /// </summary>
        [DataMember(Name = "OutgressInputBufferCount", Order = 20,
            EmitDefaultValue = true)]
        public long OutgressInputBufferCount { get; set; }

        /// <summary>
        /// OutgressInputBufferDropped
        /// </summary>
        [DataMember(Name = "OutgressInputBufferDropped", Order = 21,
            EmitDefaultValue = true)]
        public long OutgressInputBufferDropped { get; set; }

        /// <summary>
        /// OutgressIoTMessageCount
        /// </summary>
        [DataMember(Name = "OutgressIoTMessageCount", Order = 22,
            EmitDefaultValue = true)]
        public long OutgressIoTMessageCount { get; set; }

        /// <summary>
        /// Connection Retries
        /// </summary>
        [DataMember(Name = "ConnectionRetries", Order = 23,
            EmitDefaultValue = true)]
        public long ConnectionRetries { get; set; }

        /// <summary>
        /// OpcEndpointConnected
        /// </summary>
        [DataMember(Name = "OpcEndpointConnected", Order = 24,
            EmitDefaultValue = true)]
        public bool OpcEndpointConnected { get; set; }

        /// <summary>
        /// MonitoredOpcNodesSucceededCount
        /// </summary>
        [DataMember(Name = "MonitoredOpcNodesSucceededCount", Order = 25,
            EmitDefaultValue = true)]
        public long MonitoredOpcNodesSucceededCount { get; set; }

        /// <summary>
        /// MonitoredOpcNodesFailedCount
        /// </summary>
        [DataMember(Name = "MonitoredOpcNodesFailedCount", Order = 26,
            EmitDefaultValue = true)]
        public long MonitoredOpcNodesFailedCount { get; set; }

        /// <summary>
        /// Number of incoming event notifications
        /// </summary>
        [DataMember(Name = "IngressEventNotifications", Order = 27,
            EmitDefaultValue = true)]
        public long IngressEventNotifications { get; set; }

        /// <summary>
        /// Total incoming events so far.
        /// </summary>
        [DataMember(Name = "IngressEvents", Order = 28,
            EmitDefaultValue = true)]
        public long IngressEvents { get; set; }

        /// <summary>
        /// Encoder max message split ratio
        /// </summary>
        [DataMember(Name = "EncoderMaxMessageSplitRatio", Order = 29,
            EmitDefaultValue = true)]
        public double EncoderMaxMessageSplitRatio { get; set; }

        /// <summary>
        /// Number of incoming keep alive notifications
        /// </summary>
        [DataMember(Name = "IngressKeepAliveNotifications", Order = 30,
            EmitDefaultValue = true)]
        public long IngressKeepAliveNotifications { get; set; }

        /// <summary>
        /// Number of endpoints connected
        /// </summary>
        [DataMember(Name = "NumberOfConnectedEndpoints", Order = 36,
            EmitDefaultValue = true)]
        public int NumberOfConnectedEndpoints { get; set; }

        /// <summary>
        /// Number of endpoints disconnected
        /// </summary>
        [DataMember(Name = "NumberOfDisconnectedEndpoints", Order = 37,
            EmitDefaultValue = true)]
        public int NumberOfDisconnectedEndpoints { get; set; }

        /// <summary>
        /// Number of model changes generated
        /// </summary>
        [DataMember(Name = "IngressModelChanges", Order = 39,
            EmitDefaultValue = true)]
        public long IngressModelChanges { get; set; }

        /// <summary>
        /// Events in the last minute
        /// </summary>
        [DataMember(Name = "IngressEventsInLastMinute", Order = 40,
            EmitDefaultValue = true)]
        public long IngressEventsInLastMinute { get; set; }

        /// <summary>
        /// Heartbeats in the last minute
        /// </summary>
        [DataMember(Name = "IngressHeartbeatsInLastMinute", Order = 41,
            EmitDefaultValue = true)]
        public long IngressHeartbeatsInLastMinute { get; set; }

        /// <summary>
        /// Cyclic reads last minute
        /// </summary>
        [DataMember(Name = "IngressCyclicReadsInLastMinute", Order = 42,
            EmitDefaultValue = true)]
        public long IngressCyclicReadsInLastMinute { get; set; }

        /// <summary>
        /// Number of model changes generated
        /// </summary>
        [DataMember(Name = "IngressModelChangesInLastMinute", Order = 43,
            EmitDefaultValue = true)]
        public long IngressModelChangesInLastMinute { get; set; }

        /// <summary>
        /// Event list notifications in the last minute
        /// </summary>
        [DataMember(Name = "IngressEventNotificationsInLastMinute", Order = 44,
            EmitDefaultValue = true)]
        public long IngressEventNotificationsInLastMinute { get; set; }

        /// <summary>
        /// Messages failed sending
        /// </summary>
        [DataMember(Name = "OutgressIoTMessageFailedCount", Order = 45,
            EmitDefaultValue = true)]
        public long OutgressIoTMessageFailedCount { get; set; }

        /// <summary>
        /// Total server queue overflows
        /// </summary>
        [DataMember(Name = "ServerQueueOverflows", Order = 46,
            EmitDefaultValue = true)]
        public long ServerQueueOverflows { get; set; }

        /// <summary>
        /// Queue server queue overflows in the last minute
        /// </summary>
        [DataMember(Name = "ServerQueueOverflowsInLastMinute", Order = 47,
            EmitDefaultValue = true)]
        public long ServerQueueOverflowsInLastMinute { get; set; }

        /// <summary>
        /// Sampled values in the completed cyclic reads
        /// </summary>
        [DataMember(Name = "IngressSampledValues", Order = 48,
            EmitDefaultValue = true)]
        public long IngressSampledValues { get; set; }

        /// <summary>
        /// Sampled values in the last minute
        /// </summary>
        [DataMember(Name = "IngressSampledValuesInLastMinute", Order = 49,
            EmitDefaultValue = true)]
        public long IngressSampledValuesInLastMinute { get; set; }

        /// <summary>
        /// Notifications dropped before pushing to publish queue
        /// </summary>
        [DataMember(Name = "IngressNotificationsDropped", Order = 50,
            EmitDefaultValue = true)]
        public long IngressNotificationsDropped { get; set; }

        /// <summary>
        /// Total partitions
        /// </summary>
        [DataMember(Name = "TotalPublishQueuePartitions", Order = 51,
            EmitDefaultValue = true)]
        public int TotalPublishQueuePartitions { get; set; }

        /// <summary>
        /// Active partitions
        /// </summary>
        [DataMember(Name = "ActivePublishQueuePartitions", Order = 52,
            EmitDefaultValue = true)]
        public int ActivePublishQueuePartitions { get; set; }

        /// <summary>
        /// Gets the CPU utilization percentage.
        /// </summary>
        [DataMember(Name = "CpuUsedPercentage", Order = 60,
            EmitDefaultValue = true)]
        public double CpuUsedPercentage { get; set; }

        /// <summary>
        /// Gets the CPU units available in the system.
        /// </summary>
        [DataMember(Name = "GuaranteedCpuUnits", Order = 61,
            EmitDefaultValue = true)]
        public double GuaranteedCpuUnits { get; set; }

        /// <summary>
        /// Gets the maximum CPU units available in the system.
        /// </summary>
        [DataMember(Name = "MaximumCpuUnits", Order = 62,
            EmitDefaultValue = true)]
        public double MaximumCpuUnits { get; set; }

        /// <summary>
        /// Gets the memory utilization percentage.
        /// </summary>
        [DataMember(Name = "MemoryUsedPercentage", Order = 63,
            EmitDefaultValue = true)]
        public double MemoryUsedPercentage { get; set; }

        /// <summary>
        /// Gets the memory used in bytes.
        /// </summary>
        [DataMember(Name = "MemoryUsedInBytes", Order = 64,
            EmitDefaultValue = true)]
        public ulong MemoryUsedInBytes { get; set; }

        /// <summary>
        /// Gets the memory allocated to the system in bytes.
        /// </summary>
        [DataMember(Name = "GuaranteedMemoryInBytes", Order = 65,
            EmitDefaultValue = true)]
        public ulong GuaranteedMemoryInBytes { get; set; }

        /// <summary>
        /// Gets the Request Memory Limit or the Maximum allocated for the VM.
        /// </summary>
        [DataMember(Name = "MaximumMemoryInBytes", Order = 66,
            EmitDefaultValue = true)]
        public ulong MaximumMemoryInBytes { get; set; }

        /// <summary>
        /// Nodes that are late reporting if watchdog is enabled
        /// </summary>
        [DataMember(Name = "MonitoredOpcNodesLateCount", Order = 67,
            EmitDefaultValue = true)]
        public long MonitoredOpcNodesLateCount { get; set; }

        /// <summary>
        /// Nodes with active heartbeat timer
        /// </summary>
        [DataMember(Name = "ActiveHeartbeatCount", Order = 68,
            EmitDefaultValue = true)]
        public long ActiveHeartbeatCount { get; set; }

        /// <summary>
        /// Nodes with active condition snapshot timer
        /// </summary>
        [DataMember(Name = "ActiveConditionCount", Order = 69,
            EmitDefaultValue = true)]
        public long ActiveConditionCount { get; set; }

        /// <summary>
        /// ConnectionCount
        /// </summary>
        [DataMember(Name = "ConnectionCount", Order = 70,
            EmitDefaultValue = true)]
        public long ConnectionCount { get; set; }

        /// <summary>
        /// Number of writers in the writer group
        /// </summary>
        [DataMember(Name = "NumberOfWriters", Order = 71,
            EmitDefaultValue = true)]
        public int NumberOfWriters { get; set; }

        /// <summary>
        /// Total Publish requests of all clients assigned to
        /// the group.
        /// </summary>
        [DataMember(Name = "TotalPublishRequests", Order = 72,
            EmitDefaultValue = true)]
        public int TotalPublishRequests { get; set; }

        /// <summary>
        /// Total  Good publish requests of all clients assigned to
        /// the group. They might not apply to the subscriptions
        /// assigned to the group.
        /// </summary>
        [DataMember(Name = "TotalGoodPublishRequests", Order = 73,
            EmitDefaultValue = true)]
        public int TotalGoodPublishRequests { get; set; }

        /// <summary>
        /// Total bad publish requests of all clients assigned to
        /// the group. They might not apply to the subscriptions
        /// assigned to the group.
        /// </summary>
        [DataMember(Name = "TotalBadPublishRequests", Order = 74,
            EmitDefaultValue = true)]
        public int TotalBadPublishRequests { get; set; }

        /// <summary>
        /// Total min publish requests of all clients assigned to
        /// the group.
        /// </summary>
        [DataMember(Name = "TotalMinPublishRequests", Order = 75,
            EmitDefaultValue = true)]
        public int TotalMinPublishRequests { get; set; }

        /// <summary>
        /// Total number of monitored nodes in the writer group.
        /// </summary>
        [DataMember(Name = "MonitoredOpcNodesCount", Order = 76,
            EmitDefaultValue = true)]
        public int MonitoredOpcNodesCount { get; set; }
    }
}
