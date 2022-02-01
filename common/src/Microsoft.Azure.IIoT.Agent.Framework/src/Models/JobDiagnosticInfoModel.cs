namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Class to enable output to the console.
    /// </summary>
    public class JobDiagnosticInfoModel {
        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public TimeSpan IngestionDuration { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public ulong IngressDataChanges { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public ulong IngressValueChanges { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public int IngressBatchBlockBufferSize { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public uint EncodingBlockInputOutputSize { get; set; }

        /// <summary>
        /// Seq  uence number of the event
        /// </summary>
        public uint EncoderNotificationsProcessed { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public uint EncoderNotificationsDropped { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public uint EncoderIoTMessagesProcessed { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public double EncoderAvgNotificationsMessage { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public double EncoderAvgIoTMessageBodySize { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public double EncoderAvgIoTChunkUsage { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public double EstimatedIoTChunksPerDay { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public int OutgressBatchBlockBufferSize { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public int OutgressInputBufferCount { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public ulong OutgressInputBufferDropped { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public long OutgressIoTMessageCount { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public int ConnectionRetries { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public bool OpcEndpointConnected { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public int MonitoredOpcNodesSucceededCount { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public int MonitoredOpcNodesFailedCount { get; set; }
    }
}

