// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using System.Diagnostics.Metrics;
    using System;
    using System.Collections.Generic;
    using Serilog;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using System.Text;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Concurrent;

    /// <summary>
    /// Collect metrics
    /// </summary>
    public sealed class DiagnosticCollector : IDisposable {

        /// <summary>
        /// Create collector
        /// </summary>
        public DiagnosticCollector(IEngineConfiguration config, IIdentity identity, ILogger logger) {
            _logger = logger;
            _identity = identity;
            _diagnosticInterval = config.DiagnosticsInterval.GetValueOrDefault(TimeSpan.Zero);
            _meterListener = new MeterListener();
            _meterListener.InstrumentPublished = (instrument, listener) => {
                if (_diagnostics.ContainsKey(instrument.Meter.Name)) {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _meterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
            _meterListener.Start();
         }

         private readonly ConcurrentDictionary<string, string> _diagnostics
            = new ConcurrentDictionary<string, string> {
                ["iiot_edge_publisher_data_changes"] = "IngressDataChanges",
                ["iiot_edge_publisher_value_changes"] = "IngressValueChanges",
                ["iiot_edge_publisher_events"] = "EventCount",
                ["iiot_edge_publisher_event_notifications"] = "IngressEventNotifications",
                ["iiot_edge_publisher_estimated_message_chunks_per_day"] = "EstimatedIoTChunksPerDay",
                ["iiot_edge_publisher_sent_iot_messages_per_second"] = "SentMessagesPerSec",
                ["iiot_edge_publisher_chunk_size_average"] = "EncoderAvgIoTChunkUsage",
                ["iiot_edge_publisher_iothub_queue_size"] = "OutgressInputBufferCount",
                ["iiot_edge_publisher_iothub_queue_dropped_count"] = "OutgressInputBufferDropped",
                ["iiot_edge_publisher_batch_input_queue_size"] = "IngressBatchBlockBufferSize",
                ["iiot_edge_publisher_encoding_input_queue_size"] = "EncodingBlockInputSize",
                ["iiot_edge_publisher_encoding_output_queue_size"] = "EncodingBlockOutputSize",
                ["iiot_edge_publisher_encoded_notifications"] = "EncoderNotificationsProcessed",
                ["iiot_edge_publisher_message_split_ratio_max"] = "EncoderMaxMessageSplitRatio",
                ["iiot_edge_publisher_dropped_notifications"] = "EncoderNotificationsDropped",
                ["iiot_edge_publisher_processed_messages"] = "EncoderIoTMessagesProcessed",
                ["iiot_edge_publisher_notifications_per_message_average"] = "EncoderAvgNotificationsMessage",
                ["iiot_edge_publisher_encoded_message_size_average"] = "EncoderAvgIoTMessageBodySize",
                ["iiot_edge_publisher_good_nodes"] = "MonitoredOpcNodesSucceededCount",
                ["iiot_edge_publisher_bad_nodes"] = "MonitoredOpcNodesFailedCount",
                ["iiot_edge_publisher_is_connection_ok"] = "OpcEndpointConnected",
                ["iiot_edge_publisher_connection_retries"] = "ConnectionRetries"
            };

        /// <inheritdoc/>
        public void Dispose() {
            _meterListener.Dispose();
        }

        /// <summary>
        /// Collect measurement
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instrument"></param>
        /// <param name="measurement"></param>
        /// <param name="tags"></param>
        /// <param name="state"></param>
        private void OnMeasurementRecorded<T>(Instrument instrument, T measurement,
            ReadOnlySpan<KeyValuePair<string, object>> tags, object state) {

        }

        /// <inheritdoc/>
        public PublishDiagnosticInfoModel GetDiagnosticInfo() {

            var totalSeconds = (DateTime.UtcNow - _diagnosticStart).TotalSeconds;
            double totalDuration = _diagnosticStart != DateTime.MinValue ? totalSeconds : 0;
            //  double chunkSizeAverage = _messageEncoder.AvgMessageSize / (4 * 1024);
            //  double sentMessagesPerSec = totalDuration > 0 ? _messageSink.SentMessagesCount / totalDuration : 0;
            //   double estimatedMsgChunksPerDay = Math.Ceiling(chunkSizeAverage) * sentMessagesPerSec * 60 * 60 * 24;
            var diagnosticInfo = new PublishDiagnosticInfoModel();
            // TODO: Generate  diagnosticInfo.SentMessagesPerSec = sentMessagesPerSec;
            // TODO: Generate   diagnosticInfo.EstimatedIoTChunksPerDay = estimatedMsgChunksPerDay;

            diagnosticInfo.IngestionDuration = TimeSpan.FromSeconds(totalDuration);
            // From IMessageTrigger: diagnosticInfo.IngressDataChanges = _messageTrigger.DataChangesCount;
            // From IMessageTrigger: diagnosticInfo.IngressValueChanges = _messageTrigger.ValueChangesCount;
            // From IMessageTrigger: diagnosticInfo.IngressEvents = _messageTrigger.EventCount;
            // From IMessageTrigger: diagnosticInfo.IngressEventNotifications = _messageTrigger.EventNotificationCount;

         // FROM    diagnosticInfo.OutgressInputBufferCount = _sinkBlock.InputCount;
         // FROM    diagnosticInfo.OutgressInputBufferDropped = (ulong)_sinkBlockInputDroppedCount;
         // FROM    diagnosticInfo.IngressBatchBlockBufferSize = _batchDataSetMessageBlock.OutputCount;
         // FROM    diagnosticInfo.EncodingBlockInputSize = _encodingBlock.InputCount;
         // FROM    diagnosticInfo.EncodingBlockOutputSize = _encodingBlock.OutputCount;

            // From IMessageEncoder: diagnosticInfo.EncoderNotificationsProcessed = _messageEncoder.NotificationsProcessedCount;
            // From IMessageEncoder: diagnosticInfo.EncoderMaxMessageSplitRatio = _messageEncoder.MaxMessageSplitRatio;
            // From IMessageEncoder: diagnosticInfo.EncoderNotificationsDropped = _messageEncoder.NotificationsDroppedCount;
            // From IMessageEncoder: diagnosticInfo.EncoderIoTMessagesProcessed = _messageEncoder.MessagesProcessedCount;
            // From IMessageEncoder: diagnosticInfo.EncoderAvgNotificationsMessage = _messageEncoder.AvgNotificationsPerMessage;
            // From IMessageEncoder: diagnosticInfo.EncoderAvgIoTMessageBodySize = _messageEncoder.AvgMessageSize;
            // From IMessageEncoder: diagnosticInfo.EncoderAvgIoTChunkUsage = chunkSizeAverage;

            // From IMessageSink diagnosticInfo.OutgressIoTMessageCount = _messageSink.SentMessagesCount;
            //
            //  // From session/subscriptions
            //  diagnosticInfo.ConnectionRetries = _source.NumberOfConnectionRetries;
            //  diagnosticInfo.OpcEndpointConnected = _source.IsConnectionOk;
            //  diagnosticInfo.MonitoredOpcNodesSucceededCount = _source.NumberOfGoodNodes;
            //  diagnosticInfo.MonitoredOpcNodesFailedCount = _source.NumberOfBadNodes;
            //
            var endpointDiagnosticInfo = new PublishNodesEndpointModel();
            // endpointDiagnosticInfo.EndpointUrl = _source.EndpointUrl;
            // endpointDiagnosticInfo.DataSetWriterGroup = _source.DataSetWriterGroup;
            // endpointDiagnosticInfo.UseSecurity = _source.UseSecurity;
            // endpointDiagnosticInfo.OpcAuthenticationMode = _source.AuthenticationMode;
            // endpointDiagnosticInfo.OpcAuthenticationUsername = _source.AuthenticationUsername;
            // diagnosticInfo.Endpoint = endpointDiagnosticInfo;
            return diagnosticInfo;
        }

        /// <summary>
        /// Diagnostics timer
        /// </summary>
        private void DiagnosticsOutputTimer_Elapsed(object state) {
            var info = GetDiagnosticInfo();
            if (info == null) {
                return;
            }
            var totalSeconds = (DateTime.UtcNow - _diagnosticStart).TotalSeconds;
            double valueChangesPerSec = info.IngressValueChanges / info.IngestionDuration.TotalSeconds;
            double dataChangesPerSec = info.IngressDataChanges / info.IngestionDuration.TotalSeconds;
            // FROM Message TRigger  var dataChangesLastMin = _messageTrigger.DataChangesCountLastMinute;
            // FROM Message TRigger  double valueChangesPerSecLastMin = _messageTrigger.ValueChangesCountLastMinute / Math.Min(totalSeconds, 60d);
            // FROM Message TRigger  double dataChangesPerSecLastMin = _messageTrigger.DataChangesCountLastMinute / Math.Min(totalSeconds, 60d);
            // double messageSizeAveragePercent = Math.Round(info.EncoderAvgIoTMessageBodySize / _maxEncodedMessageSize * 100);
           // string messageSizeAveragePercentFormatted = $"({messageSizeAveragePercent}%)";

            _logger.Debug("Identity {deviceId}; {moduleId}", _identity.DeviceId, _identity.ModuleId);

            var diagInfo = new StringBuilder();
            diagInfo.AppendLine("\n  DIAGNOSTICS INFORMATION for          : {host}");
            diagInfo.AppendLine("  # Ingestion duration                 : {duration,14:dd\\:hh\\:mm\\:ss} (dd:hh:mm:ss)");
            // FROM Message TRigger           string dataChangesPerSecFormatted = info.IngressDataChanges > 0 && info.IngestionDuration.TotalSeconds > 0
            // FROM Message TRigger               ? $"(All time ~{dataChangesPerSec:0.##}/s; {dataChangesLastMin.ToString("D2")} in last 60s ~{dataChangesPerSecLastMin:0.##}/s)"
            // FROM Message TRigger               : "";
            // FROM Message TRigger           diagInfo.AppendLine("  # Ingress DataChanges (from OPC)     : {dataChangesCount,14:n0} {dataChangesPerSecFormatted}");
            // FROM Message TRigger           diagInfo.AppendLine("  # Ingress EventData (from OPC)       : {eventNotificationCount,14:n0}");
            // FROM Message TRigger           string valueChangesPerSecFormatted = info.IngressValueChanges > 0 && info.IngestionDuration.TotalSeconds > 0
            // FROM Message TRigger               ? $"(All time ~{valueChangesPerSec:0.##}/s; {dataChangesLastMin.ToString("D2")} in last 60s ~{valueChangesPerSecLastMin:0.##}/s)"
            // FROM Message TRigger               : "";
            diagInfo.AppendLine("  # Ingress ValueChanges (from OPC)    : {valueChangesCount,14:n0} {valueChangesPerSecFormatted}");
            diagInfo.AppendLine("  # Ingress Events (from OPC)          : {eventCount,14:n0}");

            diagInfo.AppendLine("  # Ingress BatchBlock buffer size     : {batchDataSetMessageBlockOutputCount,14:0}");
            diagInfo.AppendLine("  # Encoding Block input/output size   : {encodingBlockInputCount,14:0} | {encodingBlockOutputCount:0}");
            diagInfo.AppendLine("  # Encoder Notifications processed    : {notificationsProcessedCount,14:n0}");
            diagInfo.AppendLine("  # Encoder Notifications dropped      : {notificationsDroppedCount,14:n0}");
            diagInfo.AppendLine("  # Encoder IoT Messages processed     : {messagesProcessedCount,14:n0}");
            diagInfo.AppendLine("  # Encoder avg Notifications/Message  : {notificationsPerMessage,14:0}");
            diagInfo.AppendLine("  # Encoder worst Message split ratio  : {encoderMaxMessageSplitRatio,14:0.#}");
            diagInfo.AppendLine("  # Encoder avg IoT Message body size  : {messageSizeAverage,14:n0} {messageSizeAveragePercentFormatted}");
            diagInfo.AppendLine("  # Encoder avg IoT Chunk (4 KB) usage : {chunkSizeAverage,14:0.#}");
            diagInfo.AppendLine("  # Estimated IoT Chunks (4 KB) per day: {estimatedMsgChunksPerDay,14:n0}");
            diagInfo.AppendLine("  # Outgress input buffer count        : {sinkBlockInputCount,14:n0}");
            diagInfo.AppendLine("  # Outgress input buffer dropped      : {sinkBlockInputDroppedCount,14:n0}");

            string sentMessagesPerSecFormatted = info.OutgressIoTMessageCount > 0 && info.IngestionDuration.TotalSeconds > 0 ? $"({info.SentMessagesPerSec:0.##}/s)" : "";
            diagInfo.AppendLine("  # Outgress IoT message count         : {messageSinkSentMessagesCount,14:n0} {sentMessagesPerSecFormatted}");
            diagInfo.AppendLine("  # Connection retries                 : {connectionRetries,14:0}");
            diagInfo.AppendLine("  # Opc endpoint connected?            : {isConnectionOk,14:0}");
            diagInfo.AppendLine("  # Monitored Opc nodes succeeded count: {goodNodes,14:0}");
            diagInfo.AppendLine("  # Monitored Opc nodes failed count   : {badNodes,14:0}");

            _logger.Information(diagInfo.ToString(),
                info.Endpoint.DataSetWriterGroup,
                info.IngestionDuration,
                // FROM Message TRigger       info.IngressDataChanges, dataChangesPerSecFormatted,
                info.IngressEventNotifications,
                // FROM Message TRigger    info.IngressValueChanges, valueChangesPerSecFormatted,
                info.IngressEvents,
                info.IngressBatchBlockBufferSize,
                info.EncodingBlockInputSize, info.EncodingBlockOutputSize,
                info.EncoderNotificationsProcessed,
                info.EncoderNotificationsDropped,
                info.EncoderIoTMessagesProcessed,
                info.EncoderAvgNotificationsMessage,
                info.EncoderMaxMessageSplitRatio,
                //       info.EncoderAvgIoTMessageBodySize, messageSizeAveragePercentFormatted,
                info.EncoderAvgIoTChunkUsage,
                info.EstimatedIoTChunksPerDay,
                info.OutgressInputBufferCount,
                info.OutgressInputBufferDropped,
                info.OutgressIoTMessageCount, sentMessagesPerSecFormatted,
                info.ConnectionRetries,
                info.OpcEndpointConnected,
                info.MonitoredOpcNodesSucceededCount,
                info.MonitoredOpcNodesFailedCount);


            // IMessageSink  kSentMessagesCount.WithLabels(deviceId, moduleId, Name)
            // IMessageSink      .Set(info.OutgressIoTMessageCount);

        }

        private readonly IIdentity _identity;
        private readonly MeterListener _meterListener;
        private readonly ILogger _logger;
        private readonly TimeSpan _diagnosticInterval;
        private DateTime _diagnosticStart = DateTime.UtcNow;
    }
}
