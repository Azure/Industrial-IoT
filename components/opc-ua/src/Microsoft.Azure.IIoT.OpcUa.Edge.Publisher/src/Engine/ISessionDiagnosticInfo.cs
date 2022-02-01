namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Class to enable output to the console.
    /// </summary>
    public interface ISessionDiagnosticInfo {

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        string SessionId { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        TimeSpan IngestionDuration { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        ulong IngressDataChanges { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        ulong IngressValueChanges { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        int IngressBatchBlockBufferSize { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        uint EncodingBlockInputOutputSize { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        uint EncoderNotificationsProcessed { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        uint EncoderNotificationsDropped { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        uint EncoderIoTMessagesProcessed { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        double EncoderAvgNotificationsMessage { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        double EncoderAvgIoTMessageBodySize { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        double EncoderAvgIoTChunkUsage { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        double EstimatedIoTChunksPerDay { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        int OutgressBatchBlockBufferSize { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        int OutgressInputBufferCount { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        ulong OutgressInputBufferDropped { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        long OutgressIoTMessageCount { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        int ConnectionRetries { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        bool OpcEndpointConnected { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        int MonitoredOpcNodesSucceededCount { get; set; }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        int MonitoredOpcNodesFailedCount { get; set; }

//        /// <summary>
//        /// Fetch diagnostic data.
//        /// </summary>
//        public PublisherDiagnosticsInfo GetDiagnosticInfo() {

        }
}
