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
    public sealed record class PublishDiagnosticInfoModel
    {
        /// <summary>
        /// Endpoint Information
        /// </summary>
        [DataMember(Name = "endpoint", Order = 0,
            EmitDefaultValue = true)]
        public PublishedNodesEntryModel? Endpoint { get; set; }

        /// <summary>
		/// SentMessagesPerSec
		/// </summary>
        [DataMember(Name = "sentMessagesPerSec", Order = 1,
            EmitDefaultValue = true)]
        public double SentMessagesPerSec { get; set; }

        /// <summary>
		/// IngestionDuration
		/// </summary>
        [DataMember(Name = "ingestionDuration", Order = 2,
            EmitDefaultValue = true)]
        public TimeSpan IngestionDuration { get; set; }

        /// <summary>
		/// IngressDataChanges
		/// </summary>
        [DataMember(Name = "ingressDataChanges", Order = 3,
            EmitDefaultValue = true)]
        public long IngressDataChanges { get; set; }

        /// <summary>
        /// IngressValueChanges
        /// </summary>
        [DataMember(Name = "ingressValueChanges", Order = 4,
            EmitDefaultValue = true)]
        public long IngressValueChanges { get; set; }

        /// <summary>
        /// IngressBatchBlockBufferSize
        /// </summary>
        [DataMember(Name = "ingressBatchBlockBufferSize", Order = 5,
            EmitDefaultValue = true)]
        public long IngressBatchBlockBufferSize { get; set; }

        /// <summary>
        /// EncodingBlockInputSize
        /// </summary>
        [DataMember(Name = "encodingBlockInputSize", Order = 6,
            EmitDefaultValue = true)]
        public long EncodingBlockInputSize { get; set; }

        /// <summary>
        /// EncodingBlockOutputSize
        /// </summary>
        [DataMember(Name = "encodingBlockOutputSize", Order = 7,
            EmitDefaultValue = true)]
        public long EncodingBlockOutputSize { get; set; }

        /// <summary>
        /// EncoderNotificationsProcessed
        /// </summary>
        [DataMember(Name = "encoderNotificationsProcessed", Order = 8,
            EmitDefaultValue = true)]
        public long EncoderNotificationsProcessed { get; set; }

        /// <summary>
        /// EncoderNotificationsDropped
        /// </summary>
        [DataMember(Name = "encoderNotificationsDropped", Order = 9,
            EmitDefaultValue = true)]
        public long EncoderNotificationsDropped { get; set; }

        /// <summary>
        /// EncoderIoTMessagesProcessed
        /// </summary>
        [DataMember(Name = "encoderIoTMessagesProcessed", Order = 10,
            EmitDefaultValue = true)]
        public long EncoderIoTMessagesProcessed { get; set; }

        /// <summary>
        /// EncoderAvgNotificationsMessage
        /// </summary>
        [DataMember(Name = "encoderAvgNotificationsMessage", Order = 11,
            EmitDefaultValue = true)]
        public double EncoderAvgNotificationsMessage { get; set; }

        /// <summary>
        /// EncoderAvgIoTMessageBodySize
        /// </summary>
        [DataMember(Name = "encoderAvgIoTMessageBodySize", Order = 12,
            EmitDefaultValue = true)]
        public double EncoderAvgIoTMessageBodySize { get; set; }

        /// <summary>
        /// EncoderAvgIoTChunkUsage
        /// </summary>
        [DataMember(Name = "encoderAvgIoTChunkUsage", Order = 13,
            EmitDefaultValue = true)]
        public double EncoderAvgIoTChunkUsage { get; set; }

        /// <summary>
        /// EstimatedIoTChunksPerDay
        /// </summary>
        [DataMember(Name = "estimatedIoTChunksPerDay", Order = 14,
            EmitDefaultValue = true)]
        public double EstimatedIoTChunksPerDay { get; set; }

        /// <summary>
        /// OutgressInputBufferCount
        /// </summary>
        [DataMember(Name = "outgressInputBufferCount", Order = 16,
            EmitDefaultValue = true)]
        public long OutgressInputBufferCount { get; set; }

        /// <summary>
        /// OutgressInputBufferDropped
        /// </summary>
        [DataMember(Name = "outgressInputBufferDropped", Order = 17,
            EmitDefaultValue = true)]
        public long OutgressInputBufferDropped { get; set; }

        /// <summary>
        /// OutgressIoTMessageCount
        /// </summary>
        [DataMember(Name = "outgressIoTMessageCount", Order = 18,
            EmitDefaultValue = true)]
        public long OutgressIoTMessageCount { get; set; }

        /// <summary>
        /// ConnectionRetries
        /// </summary>
        [DataMember(Name = "connectionRetries", Order = 19,
            EmitDefaultValue = true)]
        public long ConnectionRetries { get; set; }

        /// <summary>
        /// OpcEndpointConnected
        /// </summary>
        [DataMember(Name = "opcEndpointConnected", Order = 20,
            EmitDefaultValue = true)]
        public bool OpcEndpointConnected { get; set; }

        /// <summary>
        /// MonitoredOpcNodesSucceededCount
        /// </summary>
        [DataMember(Name = "monitoredOpcNodesSucceededCount", Order = 21,
            EmitDefaultValue = true)]
        public long MonitoredOpcNodesSucceededCount { get; set; }

        /// <summary>
        /// MonitoredOpcNodesFailedCount
        /// </summary>
        [DataMember(Name = "monitoredOpcNodesFailedCount", Order = 22,
            EmitDefaultValue = true)]
        public long MonitoredOpcNodesFailedCount { get; set; }

        /// <summary>
        /// Number of incoming event notifications
        /// </summary>
        [DataMember(Name = "ingressEventNotifications", Order = 23,
            EmitDefaultValue = true)]
        public long IngressEventNotifications { get; set; }

        /// <summary>
        /// Total incoming events so far.
        /// </summary>
        [DataMember(Name = "ingressEvents", Order = 24,
            EmitDefaultValue = true)]
        public long IngressEvents { get; set; }

        /// <summary>
        /// Encoder max message split ratio
        /// </summary>
        [DataMember(Name = "encoderMaxMessageSplitRatio", Order = 25,
            EmitDefaultValue = true)]
        public double EncoderMaxMessageSplitRatio { get; set; }

        /// <summary>
        /// Data changes received in the last minute
        /// </summary>
        [DataMember(Name = "ingressDataChangesInLastMinute", Order = 26,
            EmitDefaultValue = true)]
        public long IngressDataChangesInLastMinute { get; set; }

        /// <summary>
        /// Value changes received in the last minute
        /// </summary>
        [DataMember(Name = "ingressValueChangesInLastMinute", Order = 27,
            EmitDefaultValue = true)]
        public long IngressValueChangesInLastMinute { get; set; }

        /// <summary>
        /// Number of heartbeats of the total value changes
        /// </summary>
        [DataMember(Name = "ingressHeartbeats", Order = 28,
            EmitDefaultValue = true)]
        public long IngressHeartbeats { get; set; }

        /// <summary>
        /// Number of cyclic reads of the total value changes
        /// </summary>
        [DataMember(Name = "ingressCyclicReads", Order = 29,
            EmitDefaultValue = true)]
        public long IngressCyclicReads { get; set; }
    }
}
