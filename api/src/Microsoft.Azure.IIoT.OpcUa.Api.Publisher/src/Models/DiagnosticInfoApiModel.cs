// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Model for a diagnostic info response.
    /// </summary>
    [DataContract]
    public class DiagnosticInfoApiModel {

        /// <summary> EndpointInfo /// </summary>
        [DataMember(Name = "endpoint", Order = 0,
            EmitDefaultValue = true)]
        public PublishNodesEndpointApiModel Endpoint { get; set; }

        /// <summary> SentMessagesPerSec </summary>
        [DataMember(Name = "sentMessagesPerSec", Order = 1,
            EmitDefaultValue = true)]
        public double SentMessagesPerSec { get; set; }

        /// <summary> IngestionDuration /// </summary>
        [DataMember(Name = "ingestionDuration", Order = 2,
            EmitDefaultValue = true)]
        public TimeSpan IngestionDuration { get; set; }

        /// <summary> IngressDataChanges </summary>
        [DataMember(Name = "ingressDataChanges", Order = 3,
            EmitDefaultValue = true)]
        public ulong IngressDataChanges { get; set; }

        /// <summary> IngressValueChanges </summary>
        [DataMember(Name = "ingressValueChanges", Order = 4,
            EmitDefaultValue = true)]
        public ulong IngressValueChanges { get; set; }

        /// <summary> IngressBatchBlockBufferSize </summary>
        [DataMember(Name = "ingressBatchBlockBufferSize", Order = 5,
            EmitDefaultValue = true)]
        public int IngressBatchBlockBufferSize { get; set; }

        /// <summary> EncodingBlockInputSize </summary>
        [DataMember(Name = "encodingBlockInputSize", Order = 6,
            EmitDefaultValue = true)]
        public int EncodingBlockInputSize { get; set; }

        /// <summary> EncodingBlockOutputSize </summary>
        [DataMember(Name = "encodingBlockOutputSize", Order = 7,
            EmitDefaultValue = true)]
        public int EncodingBlockOutputSize { get; set; }

        /// <summary> EncoderNotificationsProcessed </summary>
        [DataMember(Name = "encoderNotificationsProcessed", Order = 8,
            EmitDefaultValue = true)]
        public uint EncoderNotificationsProcessed { get; set; }

        /// <summary> EncoderNotificationsDropped </summary>
        [DataMember(Name = "encoderNotificationsDropped", Order = 9,
            EmitDefaultValue = true)]
        public uint EncoderNotificationsDropped { get; set; }

        /// <summary> EncoderIoTMessagesProcessed </summary>
        [DataMember(Name = "encoderIoTMessagesProcessed", Order = 10,
            EmitDefaultValue = true)]
        public uint EncoderIoTMessagesProcessed { get; set; }

        /// <summary> EncoderAvgNotificationsMessage </summary>
        [DataMember(Name = "encoderAvgNotificationsMessage", Order = 11,
            EmitDefaultValue = true)]
        public double EncoderAvgNotificationsMessage { get; set; }

        /// <summary> EncoderAvgIoTMessageBodySize </summary>
        [DataMember(Name = "encoderAvgIoTMessageBodySize", Order = 12,
            EmitDefaultValue = true)]
        public double EncoderAvgIoTMessageBodySize { get; set; }

        /// <summary> EncoderAvgIoTChunkUsage </summary>
        [DataMember(Name = "encoderAvgIoTChunkUsage", Order = 13,
            EmitDefaultValue = true)]
        public double EncoderAvgIoTChunkUsage { get; set; }

        /// <summary> EstimatedIoTChunksPerDay </summary>
        [DataMember(Name = "estimatedIoTChunksPerDay", Order = 14,
            EmitDefaultValue = true)]
        public double EstimatedIoTChunksPerDay { get; set; }

        /// <summary> OutgressBatchBlockBufferSize </summary>
        [DataMember(Name = "outgressBatchBlockBufferSize", Order = 15,
            EmitDefaultValue = true)]
        public int OutgressBatchBlockBufferSize { get; set; }

        /// <summary> OutgressInputBufferCount </summary>
        [DataMember(Name = "outgressInputBufferCount", Order = 16,
            EmitDefaultValue = true)]
        public int OutgressInputBufferCount { get; set; }

        /// <summary> OutgressInputBufferDropped </summary>
        [DataMember(Name = "outgressInputBufferDropped", Order = 17,
            EmitDefaultValue = true)]
        public ulong OutgressInputBufferDropped { get; set; }

        /// <summary> OutgressIoTMessageCount </summary>
        [DataMember(Name = "outgressIoTMessageCount", Order = 18,
            EmitDefaultValue = true)]
        public long OutgressIoTMessageCount { get; set; }

        /// <summary> ConnectionRetries </summary>
        [DataMember(Name = "connectionRetries", Order = 19,
            EmitDefaultValue = true)]
        public int ConnectionRetries { get; set; }

        /// <summary> OpcEndpointConnected </summary>
        [DataMember(Name = "opcEndpointConnected", Order = 20,
            EmitDefaultValue = true)]
        public bool OpcEndpointConnected { get; set; }

        /// <summary> MonitoredOpcNodesSucceededCount </summary>
        [DataMember(Name = "monitoredOpcNodesSucceededCount", Order = 21,
            EmitDefaultValue = true)]
        public int MonitoredOpcNodesSucceededCount { get; set; }

        /// <summary> MonitoredOpcNodesFailedCount </summary>
        [DataMember(Name = "monitoredOpcNodesFailedCount", Order = 22,
            EmitDefaultValue = true)]
        public int MonitoredOpcNodesFailedCount { get; set; }

        /// <summary> Number of incoming event notifications </summary>
        [DataMember(Name = "ingressEventNotifications", Order = 23,
            EmitDefaultValue = true)]
        public ulong IngressEventNotifications { get; set; }

        /// <summary> Total incoming events so far. </summary>
        [DataMember(Name = "ingressEvents", Order = 24,
            EmitDefaultValue = true)]
        public ulong IngressEvents { get; set; }

        /// <summary> Encoder max message split ratio </summary>
        [DataMember(Name = "encoderMaxMessageSplitRatio", Order = 25,
            EmitDefaultValue = true)]
        public double EncoderMaxMessageSplitRatio { get; set; }
    }
}
