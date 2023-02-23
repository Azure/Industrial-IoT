// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;

    /// <summary>
    /// Model for a diagnostic info.
    /// </summary>
    public class WriterGroupDiagnosticModel
    {
        /// <summary>
        /// Ingestion start
        /// </summary>
        public DateTime IngestionStart { get; set; }

        /// <summary>
        /// SentMessagesPerSec
        /// </summary>
        public double SentMessagesPerSec { get; set; }

        /// <summary>
        /// IngressDataChanges
        /// </summary>
        public long IngressDataChanges { get; set; }

        /// <summary>
        /// IngressValueChanges
        /// </summary>
        public long IngressValueChanges { get; set; }

        /// <summary>
        /// Data changes received in the last minute
        /// </summary>
        public long IngressDataChangesInLastMinute { get; set; }

        /// <summary>
        /// Value changes received in the last minute
        /// </summary>
        public long IngressValueChangesInLastMinute { get; set; }

        /// <summary>
        /// IngressBatchBlockBufferSize
        /// </summary>
        public long IngressBatchBlockBufferSize { get; set; }

        /// <summary>
        /// EncodingBlockInputSize
        /// </summary>
        public long EncodingBlockInputSize { get; set; }

        /// <summary>
        /// EncodingBlockOutputSize
        /// </summary>
        public long EncodingBlockOutputSize { get; set; }

        /// <summary>
        /// EncoderNotificationsProcessed
        /// </summary>
        public long EncoderNotificationsProcessed { get; set; }

        /// <summary>
        /// EncoderNotificationsDropped
        /// </summary>
        public long EncoderNotificationsDropped { get; set; }

        /// <summary>
        /// EncoderIoTMessagesProcessed
        /// </summary>
        public long EncoderIoTMessagesProcessed { get; set; }

        /// <summary>
        /// EncoderAvgNotificationsMessage
        /// </summary>
        public double EncoderAvgNotificationsMessage { get; set; }

        /// <summary>
        /// EncoderAvgIoTMessageBodySize
        /// </summary>
        public double EncoderAvgIoTMessageBodySize { get; set; }

        /// <summary>
        /// EncoderAvgIoTChunkUsage
        /// </summary>
        public double EncoderAvgIoTChunkUsage { get; set; }

        /// <summary>
        /// EstimatedIoTChunksPerDay
        /// </summary>
        public double EstimatedIoTChunksPerDay { get; set; }

        /// <summary>
        /// OutgressInputBufferCount
        /// </summary>
        public long OutgressInputBufferCount { get; set; }

        /// <summary>
        /// OutgressInputBufferDropped
        /// </summary>
        public long OutgressInputBufferDropped { get; set; }

        /// <summary>
        /// OutgressIoTMessageCount
        /// </summary>
        public long OutgressIoTMessageCount { get; set; }

        /// <summary>
        /// ConnectionRetries
        /// </summary>
        public long ConnectionRetries { get; set; }

        /// <summary>
        /// OpcEndpointConnected
        /// </summary>
        public bool OpcEndpointConnected { get; set; }

        /// <summary>
        /// MonitoredOpcNodesSucceededCount
        /// </summary>
        public long MonitoredOpcNodesSucceededCount { get; set; }

        /// <summary>
        /// MonitoredOpcNodesFailedCount
        /// </summary>
        public long MonitoredOpcNodesFailedCount { get; set; }

        /// <summary>
        /// Number of incoming event notifications
        /// </summary>
        public long IngressEventNotifications { get; set; }

        /// <summary>
        /// Total incoming events so far.
        /// </summary>
        public long IngressEvents { get; set; }

        /// <summary>
        /// Encoder max message split ratio
        /// </summary>
        public double EncoderMaxMessageSplitRatio { get; set; }
    }
}
