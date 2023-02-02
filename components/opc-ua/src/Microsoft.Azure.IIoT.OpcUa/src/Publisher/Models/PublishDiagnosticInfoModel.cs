// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Model for a diagnostic info.
    /// </summary>
    [DataContract]
    public class PublishDiagnosticInfoModel {

        /// <summary>
        /// Endpoint Information
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public PublishNodesEndpointModel Endpoint { get; set; }

        /// <summary>
        /// SentMessagesPerSec
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public double SentMessagesPerSec { get; set; }

        /// <summary>
        /// IngestionDuration
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public TimeSpan IngestionDuration { get; set; }

        /// <summary>
        /// IngressDataChanges
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public ulong IngressDataChanges { get; set; }

        /// <summary>
        /// IngressValueChanges
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public ulong IngressValueChanges { get; set; }

        /// <summary>
        /// IngressBatchBlockBufferSize
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int IngressBatchBlockBufferSize { get; set; }

        /// <summary>
        /// EncodingBlockInputSize
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int EncodingBlockInputSize { get; set; }

        /// <summary>
        /// EncodingBlockOutputSize
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int EncodingBlockOutputSize { get; set; }

        /// <summary>
        /// EncoderNotificationsProcessed
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public uint EncoderNotificationsProcessed { get; set; }

        /// <summary>
        /// EncoderNotificationsDropped
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public uint EncoderNotificationsDropped { get; set; }

        /// <summary>
        /// EncoderIoTMessagesProcessed
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public uint EncoderIoTMessagesProcessed { get; set; }

        /// <summary>
        /// EncoderAvgNotificationsMessage
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public double EncoderAvgNotificationsMessage { get; set; }

        /// <summary>
        /// EncoderAvgIoTMessageBodySize
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public double EncoderAvgIoTMessageBodySize { get; set; }

        /// <summary>
        /// EncoderAvgIoTChunkUsage
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public double EncoderAvgIoTChunkUsage { get; set; }

        /// <summary>
        /// EstimatedIoTChunksPerDay
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public double EstimatedIoTChunksPerDay { get; set; }

        /// <summary>
        /// OutgressBatchBlockBufferSize
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int OutgressBatchBlockBufferSize { get; set; }

        /// <summary>
        /// OutgressInputBufferCount
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int OutgressInputBufferCount { get; set; }

        /// <summary>
        /// OutgressInputBufferDropped
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public ulong OutgressInputBufferDropped { get; set; }

        /// <summary>
        /// OutgressIoTMessageCount
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public long OutgressIoTMessageCount { get; set; }

        /// <summary>
        /// ConnectionRetries
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int ConnectionRetries { get; set; }

        /// <summary>
        /// OpcEndpointConnected
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public bool OpcEndpointConnected { get; set; }

        /// <summary>
        /// MonitoredOpcNodesSucceededCount
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int MonitoredOpcNodesSucceededCount { get; set; }

        /// <summary>
        /// MonitoredOpcNodesFailedCount
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int MonitoredOpcNodesFailedCount { get; set; }

        /// <summary>
        /// Number of incoming event notifications
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public ulong IngressEventNotifications { get; set; }

        /// <summary>
        /// Total incoming events so far.
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public ulong IngressEvents { get; set; }

        /// <summary>
        /// Encoder max message split ratio
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public double EncoderMaxMessageSplitRatio { get; set; }
    }
}
