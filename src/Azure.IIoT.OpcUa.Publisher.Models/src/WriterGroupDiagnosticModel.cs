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
        /// Timestamp for this diagnostics information
        /// </summary>
        [DataMember(Name = "Timestamp", Order = 0,
            EmitDefaultValue = true)]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Ingestion start
        /// </summary>
        [DataMember(Name = "IngestionStart", Order = 1,
            EmitDefaultValue = true)]
        public DateTime IngestionStart { get; set; }

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
        /// Number of cyclic reads of the total values
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
        /// ConnectionRetries
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
        /// Number Of Subscriptions in the writer group
        /// </summary>
        [DataMember(Name = "NumberOfSubscriptions", Order = 31,
            EmitDefaultValue = true)]
        public long NumberOfSubscriptions { get; set; }

        /// <summary>
        /// Publish requests ratio per group
        /// </summary>
        [DataMember(Name = "PublishRequestsRatio", Order = 32,
            EmitDefaultValue = true)]
        public double PublishRequestsRatio { get; set; }

        /// <summary>
        /// Good publish requests ratio per group
        /// </summary>
        [DataMember(Name = "GoodPublishRequestsRatio", Order = 33,
            EmitDefaultValue = true)]
        public double GoodPublishRequestsRatio { get; set; }

        /// <summary>
        /// Bad publish requests ratio per group
        /// </summary>
        [DataMember(Name = "BadPublishRequestsRatio", Order = 34,
            EmitDefaultValue = true)]
        public double BadPublishRequestsRatio { get; set; }

        /// <summary>
        /// Min publish requests assigned to the group
        /// </summary>
        [DataMember(Name = "MinPublishRequestsRatio", Order = 35,
            EmitDefaultValue = true)]
        public double MinPublishRequestsRatio { get; set; }

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
        /// Number values or events that were not assignable to
        /// the items in the subscription.
        /// </summary>
        [DataMember(Name = "IngressUnassignedChanges", Order = 38,
            EmitDefaultValue = true)]
        public long IngressUnassignedChanges { get; set; }

        /// <summary>
        /// Publisher version
        /// </summary>
        [DataMember(Name = "PublisherVersion", Order = 99,
            EmitDefaultValue = true)]
        public string? PublisherVersion { get; set; }
    }
}
