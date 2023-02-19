// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services {
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Collects metrics from the writer groups inside the publisher using the .net Meter listener
    /// API. This must be kept in sync with the values we aim to track. This class should be seen
    /// as optional, i.e., it is possible to disable any of the legacy diagnostics api and rely
    /// solely on modern OTEL based telemetry collection.
    /// </summary>
    public sealed class PublisherDiagnosticCollector : IPublisherDiagnosticCollector,
        IStartable, IDisposable {

        /// <summary>
        /// Create collector
        /// </summary>
        public PublisherDiagnosticCollector(ILogger logger, IEngineConfiguration config = null) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _meterListener = new MeterListener();
            _meterListener.InstrumentPublished = OnInstrumentPublished;
            _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
            _meterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
            _meterListener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);
            _meterListener.SetMeasurementEventCallback<byte>(OnMeasurementRecorded);
            _meterListener.SetMeasurementEventCallback<float>(OnMeasurementRecorded);
            _meterListener.SetMeasurementEventCallback<decimal>(OnMeasurementRecorded);
            _meterListener.SetMeasurementEventCallback<short>(OnMeasurementRecorded);

            _diagnosticInterval = config?.DiagnosticsInterval ?? TimeSpan.Zero;
            if (_diagnosticInterval == TimeSpan.Zero) {
                _diagnosticInterval = Timeout.InfiniteTimeSpan;
            }
            _diagnosticsOutputTimer = new Timer(DiagnosticsOutputTimer_Elapsed);
        }

        /// <inheritdoc/>
        public void ResetWriterGroup(string writerGroupId) {
            var diag = new WriterGroupDiagnosticModel { IngestionStart = DateTime.UtcNow };
            writerGroupId ??= Constants.DefaultWriterGroupId;
            _diagnostics.AddOrUpdate(writerGroupId, _ => diag, (_, _) => diag);
            _logger.Information("Tracking diagnostics for {WriterGroup} was (re-)started.",
                writerGroupId);
        }

        /// <inheritdoc/>
        public bool TryGetDiagnosticsForWriterGroup(string writerGroupId,
            out WriterGroupDiagnosticModel diagnostic) {
            writerGroupId ??= Constants.DefaultWriterGroupId;
            if (_diagnostics.TryGetValue(writerGroupId, out var value)) {
                //
                // Ensure we collect all observable instruments into this model and
                // then clone to take a snapshot
                //
                _meterListener.RecordObservableInstruments();
                diagnostic = value.Clone();
                return true;
            }
            diagnostic = default;
            return false;
        }

        /// <inheritdoc/>
        public bool RemoveWriterGroup(string writerGroupId) {
            writerGroupId ??= Constants.DefaultWriterGroupId;
            if (_diagnostics.TryRemove(writerGroupId, out _)) {
                _logger.Information("Stop tracking diagnostics for {WriterGroup}.",
                    writerGroupId);
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public void Start() {
            _meterListener.Start();
            _diagnosticsOutputTimer.Change(_diagnosticInterval, _diagnosticInterval);
        }

        /// <inheritdoc/>
        public void Dispose() {
            try {
                _diagnosticsOutputTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            finally {
                _meterListener.Dispose();
                _diagnosticsOutputTimer.Dispose();
            }
        }

        /// <summary>
        /// Enable instrument if we need to hook
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="listener"></param>
        private void OnInstrumentPublished(Instrument instrument, MeterListener listener) {
            if (_bindings.ContainsKey(instrument.Name)) {
                listener.EnableMeasurementEvents(instrument, this);
            }
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
            if (_bindings.TryGetValue(instrument.Name, out var binding)) {
                for (var index = 0; index < tags.Length; index++) {
                    if (tags[index].Key == Constants.WriterGroupIdTag) {
                        _diagnostics.AddOrUpdate((string)tags[index].Value,
                            writerGroupId => Update(new WriterGroupDiagnosticModel()),
                            (writerGroupId, existing) => Update(existing));
                        // Apply value through binding
                        WriterGroupDiagnosticModel Update(WriterGroupDiagnosticModel diag) {
                            binding(diag, measurement);
                            return diag;
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Diagnostics timer
        /// </summary>
        private void DiagnosticsOutputTimer_Elapsed(object state) {
            var now = DateTime.UtcNow;
            _meterListener.RecordObservableInstruments();
            foreach (var (writerGroupId, info) in _diagnostics) {
                LogDiagnosticInfo(writerGroupId, info, now - info.IngestionStart);
            }
            void LogDiagnosticInfo(string writerGroupId, WriterGroupDiagnosticModel info, TimeSpan ingestionDuration) {
                var totalSeconds = (DateTime.UtcNow - _diagnosticStart).TotalSeconds;
                double valueChangesPerSec = info.IngressValueChanges / ingestionDuration.TotalSeconds;
                double dataChangesPerSec = info.IngressDataChanges / ingestionDuration.TotalSeconds;
                var dataChangesLastMin = info.IngressDataChangesInLastMinute;
                double valueChangesPerSecLastMin = info.IngressValueChangesInLastMinute / Math.Min(totalSeconds, 60d);
                double dataChangesPerSecLastMin = info.IngressDataChangesInLastMinute / Math.Min(totalSeconds, 60d);

                var diagInfo = new StringBuilder();
                diagInfo.AppendLine("\n  DIAGNOSTICS INFORMATION for          : {host}");
                diagInfo.AppendLine("  # Ingestion duration                 : {duration,14:dd\\:hh\\:mm\\:ss} (dd:hh:mm:ss)");
                string dataChangesPerSecFormatted = info.IngressDataChanges > 0 && ingestionDuration.TotalSeconds > 0
                    ? $"(All time ~{dataChangesPerSec:0.##}/s; {dataChangesLastMin.ToString("D2")} in last 60s ~{dataChangesPerSecLastMin:0.##}/s)"
                    : "";
                diagInfo.AppendLine("  # Ingress DataChanges (from OPC)     : {dataChangesCount,14:n0} {dataChangesPerSecFormatted}");
                diagInfo.AppendLine("  # Ingress EventData (from OPC)       : {eventNotificationCount,14:n0}");
                string valueChangesPerSecFormatted = info.IngressValueChanges > 0 && ingestionDuration.TotalSeconds > 0
                    ? $"(All time ~{valueChangesPerSec:0.##}/s; {dataChangesLastMin.ToString("D2")} in last 60s ~{valueChangesPerSecLastMin:0.##}/s)"
                    : "";
                diagInfo.AppendLine("  # Ingress ValueChanges (from OPC)    : {valueChangesCount,14:n0} {valueChangesPerSecFormatted}");
                diagInfo.AppendLine("  # Ingress Events (from OPC)          : {eventCount,14:n0}");

                diagInfo.AppendLine("  # Ingress BatchBlock buffer size     : {batchDataSetMessageBlockOutputCount,14:0}");
                diagInfo.AppendLine("  # Encoding Block input/output size   : {encodingBlockInputCount,14:0} | {encodingBlockOutputCount:0}");
                diagInfo.AppendLine("  # Encoder Notifications processed    : {notificationsProcessedCount,14:n0}");
                diagInfo.AppendLine("  # Encoder Notifications dropped      : {notificationsDroppedCount,14:n0}");
                diagInfo.AppendLine("  # Encoder IoT Messages processed     : {messagesProcessedCount,14:n0}");
                diagInfo.AppendLine("  # Encoder avg Notifications/Message  : {notificationsPerMessage,14:0}");
                diagInfo.AppendLine("  # Encoder worst Message split ratio  : {encoderMaxMessageSplitRatio,14:0.#}");
                diagInfo.AppendLine("  # Encoder avg IoT Message body size  : {messageSizeAverage,14:n0}");
                diagInfo.AppendLine("  # Encoder avg IoT Chunk (4 KB) usage : {chunkSizeAverage,14:0.#}");
                diagInfo.AppendLine("  # Estimated IoT Chunks (4 KB) per day: {estimatedMsgChunksPerDay,14:n0}");
                diagInfo.AppendLine("  # Outgress input buffer count        : {sinkBlockInputCount,14:n0}");
                diagInfo.AppendLine("  # Outgress input buffer dropped      : {sinkBlockInputDroppedCount,14:n0}");

                string sentMessagesPerSecFormatted = info.OutgressIoTMessageCount > 0 && ingestionDuration.TotalSeconds > 0
                    ? $"({info.SentMessagesPerSec:0.##}/s)" : "";
                diagInfo.AppendLine("  # Outgress IoT message count         : {messageSinkSentMessagesCount,14:n0} {sentMessagesPerSecFormatted}");
                diagInfo.AppendLine("  # Connection retries                 : {connectionRetries,14:0}");
                diagInfo.AppendLine("  # Opc endpoint connected?            : {isConnectionOk,14:0}");
                diagInfo.AppendLine("  # Monitored Opc nodes succeeded count: {goodNodes,14:0}");
                diagInfo.AppendLine("  # Monitored Opc nodes failed count   : {badNodes,14:0}");

                _logger.Information(diagInfo.ToString(),
                    writerGroupId,
                    ingestionDuration,
                    info.IngressDataChanges, dataChangesPerSecFormatted,
                    info.IngressEventNotifications,
                    info.IngressValueChanges, valueChangesPerSecFormatted,
                    info.IngressEvents,
                    info.IngressBatchBlockBufferSize,
                    info.EncodingBlockInputSize, info.EncodingBlockOutputSize,
                    info.EncoderNotificationsProcessed,
                    info.EncoderNotificationsDropped,
                    info.EncoderIoTMessagesProcessed,
                    info.EncoderAvgNotificationsMessage,
                    info.EncoderMaxMessageSplitRatio,
                    info.EncoderAvgIoTMessageBodySize,
                    info.EncoderAvgIoTChunkUsage,
                    info.EstimatedIoTChunksPerDay,
                    info.OutgressInputBufferCount,
                    info.OutgressInputBufferDropped,
                    info.OutgressIoTMessageCount, sentMessagesPerSecFormatted,
                    info.ConnectionRetries,
                    info.OpcEndpointConnected,
                    info.MonitoredOpcNodesSucceededCount,
                    info.MonitoredOpcNodesFailedCount);
            }
        }

        private readonly Timer _diagnosticsOutputTimer;
        private readonly MeterListener _meterListener;
        private readonly ILogger _logger;
        private readonly TimeSpan _diagnosticInterval;
        private DateTime _diagnosticStart = DateTime.UtcNow;
        private readonly ConcurrentDictionary<string, WriterGroupDiagnosticModel> _diagnostics
            = new ConcurrentDictionary<string, WriterGroupDiagnosticModel>();
        // TODO: Split this per measurement type to avoid boxing
        private readonly ConcurrentDictionary<string, Action<WriterGroupDiagnosticModel, object>> _bindings
            = new ConcurrentDictionary<string, Action<WriterGroupDiagnosticModel, object>> {
                ["iiot_edge_publisher_sent_iot_messages"] =
                    (d, i) => d.OutgressIoTMessageCount = (long)i,
                ["iiot_edge_publisher_data_changes"] =
                    (d, i) => d.IngressDataChanges = (long)i,
                ["iiot_edge_publisher_value_changes"] =
                    (d, i) => d.IngressValueChanges = (long)i,
                ["iiot_edge_publisher_data_changes_per_second_last_min"] =
                    (d, i) => d.IngressDataChangesInLastMinute = (long)i,
                ["iiot_edge_publisher_value_changes_per_second_last_min"] =
                    (d, i) => d.IngressValueChangesInLastMinute = (long)i,
                ["iiot_edge_publisher_events"] =
                    (d, i) => d.IngressEvents = (long)i,
                ["iiot_edge_publisher_event_notifications"] =
                    (d, i) => d.IngressEventNotifications = (long)i,
                ["iiot_edge_publisher_estimated_message_chunks_per_day"] =
                    (d, i) => d.EstimatedIoTChunksPerDay = (double)i,
                ["iiot_edge_publisher_sent_iot_messages_per_second"] =
                    (d, i) => d.SentMessagesPerSec = (double)i,
                ["iiot_edge_publisher_chunk_size_average"] =
                    (d, i) => d.EncoderAvgIoTChunkUsage = (double)i,
                ["iiot_edge_publisher_iothub_queue_size"] =
                    (d, i) => d.OutgressInputBufferCount = (long)i,
                ["iiot_edge_publisher_iothub_queue_dropped_count"] =
                    (d, i) => d.OutgressInputBufferDropped = (long)i,
                ["iiot_edge_publisher_batch_input_queue_size"] =
                    (d, i) => d.IngressBatchBlockBufferSize = (long)i,
                ["iiot_edge_publisher_encoding_input_queue_size"] =
                    (d, i) => d.EncodingBlockInputSize = (long)i,
                ["iiot_edge_publisher_encoding_output_queue_size"] =
                    (d, i) => d.EncodingBlockOutputSize = (long)i,
                ["iiot_edge_publisher_encoded_notifications"] =
                    (d, i) => d.EncoderNotificationsProcessed = (long)i,
                ["iiot_edge_publisher_message_split_ratio_max"] =
                    (d, i) => d.EncoderMaxMessageSplitRatio = (double)i,
                ["iiot_edge_publisher_dropped_notifications"] =
                    (d, i) => d.EncoderNotificationsDropped = (long)i,
                ["iiot_edge_publisher_processed_messages"] =
                    (d, i) => d.EncoderIoTMessagesProcessed = (long)i,
                ["iiot_edge_publisher_notifications_per_message_average"] =
                    (d, i) => d.EncoderAvgNotificationsMessage = (double)i,
                ["iiot_edge_publisher_encoded_message_size_average"] =
                    (d, i) => d.EncoderAvgIoTMessageBodySize = (double)i,
                ["iiot_edge_publisher_good_nodes"] =
                    (d, i) => d.MonitoredOpcNodesSucceededCount = (long)i,
                ["iiot_edge_publisher_bad_nodes"] =
                    (d, i) => d.MonitoredOpcNodesFailedCount = (long)i,
                ["iiot_edge_publisher_is_connection_ok"] =
                    (d, i) => d.OpcEndpointConnected = ((int)i) != 0,
                ["iiot_edge_publisher_connection_retries"] =
                    (d, i) => d.ConnectionRetries = (long)i,
                // ... Add here more items if needed
            };
    }
}
