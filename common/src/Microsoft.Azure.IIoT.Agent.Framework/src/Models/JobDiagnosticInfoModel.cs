// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using System;

    /// <summary>
    /// Model for a diagnostic info response.
    /// </summary>
    public class JobDiagnosticInfoModel {

        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Endpoint
        /// </summary>
        public EndpointDiagnosticModel Endpoint { get; set; }

        /// <summary>
        /// SentMessagesPerSec
        /// </summary>
        public double SentMessagesPerSec { get; set; }

        /// <summary>
        /// IngestionDuration
        /// </summary>
        public TimeSpan IngestionDuration { get; set; }

        /// <summary>
        /// IngressDataChanges
        /// </summary>
        public ulong IngressDataChanges { get; set; }

        /// <summary>
        /// IngressValueChanges
        /// </summary>
        public ulong IngressValueChanges { get; set; }

        /// <summary>
        /// IngressBatchBlockBufferSize
        /// </summary>
        public int IngressBatchBlockBufferSize { get; set; }

        /// <summary>
        /// EncodingBlockInputSize
        /// </summary>
        public int EncodingBlockInputSize { get; set; }

        /// <summary>
        /// Number of incoming event notifications containing
        /// many events
        /// </summary>
        public ulong IngressEventNotifications { get; set; }

        /// <summary>
        /// Total incoming events so far.
        /// </summary>
        public ulong IngressEvents { get; set; }

        /// <summary>
        /// EncodingBlockOutputSize
        /// </summary>
        public int EncodingBlockOutputSize { get; set; }

        /// <summary>
        /// EncoderNotificationsProcessed
        /// </summary>
        public uint EncoderNotificationsProcessed { get; set; }

        /// <summary>
        /// EncoderNotificationsDropped
        /// </summary>
        public uint EncoderNotificationsDropped { get; set; }

        /// <summary>
        /// EncoderIoTMessagesProcessed
        /// </summary>
        public uint EncoderIoTMessagesProcessed { get; set; }

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
        /// OutgressBatchBlockBufferSize
        /// </summary>
        public int OutgressBatchBlockBufferSize { get; set; }

        /// <summary>
        /// OutgressInputBufferCount
        /// </summary>
        public int OutgressInputBufferCount { get; set; }

        /// <summary>
        /// OutgressInputBufferDropped
        /// </summary>
        public ulong OutgressInputBufferDropped { get; set; }

        /// <summary>
        /// OutgressIoTMessageCount
        /// </summary>
        public long OutgressIoTMessageCount { get; set; }

        /// <summary>
        /// ConnectionRetries
        /// </summary>
        public int ConnectionRetries { get; set; }

        /// <summary>
        /// OpcEndpointConnected
        /// </summary>
        public bool OpcEndpointConnected { get; set; }

        /// <summary>
        /// MonitoredOpcNodesSucceededCount
        /// </summary>
        public int MonitoredOpcNodesSucceededCount { get; set; }

        /// <summary>
        /// MonitoredOpcNodesFailedCount
        /// </summary>
        public int MonitoredOpcNodesFailedCount { get; set; }

        /// <summary>
        /// Encoder max message split ratio
        /// </summary>
        public double EncoderMaxMessageSplitRatio { get; set; }
    }
}

