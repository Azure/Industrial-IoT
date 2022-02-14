// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Model for a diagnostic info response.
    /// </summary>
    [DataContract]
    public class DiagnosticInfoApiModel {
 
        /// <summary> EndpointInfo /// </summary>
        [DataMember(Name = "PublishedNodesEntryModel", Order = 0,
            EmitDefaultValue = false)]
        public PublishedNodesEntryModel EndpointInfo { get; set; }

        /// <summary> SentMessagesPerSec </summary>
        [DataMember(Name = "SentMessagesPerSec", Order = 1,
            EmitDefaultValue = false)]
        public double SentMessagesPerSec { get; set; }

        /// <summary> IngestionDuration /// </summary>
        [DataMember(Name = "IngestionDuration", Order = 2,
            EmitDefaultValue = false)]
        public TimeSpan IngestionDuration { get; set; }

        /// <summary> IngressDataChanges </summary>
        [DataMember(Name = "IngressDataChanges", Order = 3,
            EmitDefaultValue = false)]
        public ulong IngressDataChanges { get; set; }

        /// <summary> IngressValueChanges </summary>
        [DataMember(Name = "IngressValueChanges", Order = 4,
            EmitDefaultValue = false)]
        public ulong IngressValueChanges { get; set; }

        /// <summary> IngressBatchBlockBufferSize </summary> 
        [DataMember(Name = "IngressBatchBlockBufferSize", Order = 5,
            EmitDefaultValue = false)]
        public int IngressBatchBlockBufferSize { get; set; }

        /// <summary> EncodingBlockInputSize </summary>
        [DataMember(Name = "EncodingBlockInputSize", Order = 6,
            EmitDefaultValue = false)]
        public int EncodingBlockInputSize { get; set; }

        /// <summary> EncodingBlockOutputSize </summary>
        [DataMember(Name = "EncodingBlockOutputSize", Order = 7,
            EmitDefaultValue = false)]
        public int EncodingBlockOutputSize { get; set; }

        /// <summary> EncoderNotificationsProcessed </summary>
        [DataMember(Name = "EncoderNotificationsProcessed", Order = 8,
            EmitDefaultValue = false)]
        public uint EncoderNotificationsProcessed { get; set; }

        /// <summary> EncoderNotificationsDropped </summary>
        [DataMember(Name = "EncoderNotificationsDropped", Order = 9,
            EmitDefaultValue = false)]
        public uint EncoderNotificationsDropped { get; set; }

        /// <summary> EncoderIoTMessagesProcessed </summary>
        [DataMember(Name = "EncoderIoTMessagesProcessed", Order = 10,
            EmitDefaultValue = false)]
        public uint EncoderIoTMessagesProcessed { get; set; }

        /// <summary> EncoderAvgNotificationsMessage </summary>
        [DataMember(Name = "EncoderAvgNotificationsMessage", Order = 11,
            EmitDefaultValue = false)]
        public double EncoderAvgNotificationsMessage { get; set; }

        /// <summary> EncoderAvgIoTMessageBodySize </summary>
        [DataMember(Name = "EncoderAvgIoTMessageBodySize", Order = 12,
            EmitDefaultValue = false)]
        public double EncoderAvgIoTMessageBodySize { get; set; }

        /// <summary> EncoderAvgIoTChunkUsage </summary>
        [DataMember(Name = "EncoderAvgIoTChunkUsage", Order = 13,
            EmitDefaultValue = false)]
        public double EncoderAvgIoTChunkUsage { get; set; }

        /// <summary> EstimatedIoTChunksPerDay </summary>
        [DataMember(Name = "EstimatedIoTChunksPerDay", Order = 14,
            EmitDefaultValue = false)]
        public double EstimatedIoTChunksPerDay { get; set; }

        /// <summary> OutgressBatchBlockBufferSize </summary>
        [DataMember(Name = "OutgressBatchBlockBufferSize", Order = 15,
            EmitDefaultValue = false)]
        public int OutgressBatchBlockBufferSize { get; set; }

        /// <summary> OutgressInputBufferCount </summary>
        [DataMember(Name = "OutgressInputBufferCount", Order = 16,
            EmitDefaultValue = false)]
        public int OutgressInputBufferCount { get; set; }

        /// <summary> OutgressInputBufferDropped </summary>
        [DataMember(Name = "OutgressInputBufferDropped", Order = 17,
            EmitDefaultValue = false)]
        public ulong OutgressInputBufferDropped { get; set; }

        /// <summary> OutgressIoTMessageCount </summary>
        [DataMember(Name = "OutgressIoTMessageCount", Order = 18,
            EmitDefaultValue = false)]
        public long OutgressIoTMessageCount { get; set; }

        /// <summary> ConnectionRetries </summary>
        [DataMember(Name = "ConnectionRetries", Order = 19,
            EmitDefaultValue = false)]
        public int ConnectionRetries { get; set; }

        /// <summary> OpcEndpointConnected </summary>
        [DataMember(Name = "OpcEndpointConnected", Order = 20,
            EmitDefaultValue = false)]
        public bool OpcEndpointConnected { get; set; }

        /// <summary> MonitoredOpcNodesSucceededCount </summary>
        [DataMember(Name = "MonitoredOpcNodesSucceededCount", Order = 21,
            EmitDefaultValue = false)]
        public int MonitoredOpcNodesSucceededCount { get; set; }

        /// <summary> MonitoredOpcNodesFailedCount </summary>
        [DataMember(Name = "MonitoredOpcNodesFailedCount", Order = 22,
            EmitDefaultValue = false)]
        public int MonitoredOpcNodesFailedCount { get; set; }
    }
}
