// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Autofac;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Metrics;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Linq;

    /// <summary>
    /// Collects metrics from the writer groups inside the publisher using the .net Meter listener
    /// API. This must be kept in sync with the values we aim to track. This class should be seen
    /// as optional, i.e., it is possible to disable any of the legacy diagnostics api and rely
    /// solely on modern OTEL based telemetry collection.
    /// </summary>
    public sealed class PublisherDiagnosticCollector : IDiagnosticCollector,
        IStartable, IDisposable
    {
        /// <summary>
        /// Create collector
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        public PublisherDiagnosticCollector(ILogger<PublisherDiagnosticCollector> logger,
            IOptions<PublisherOptions>? options = null)
        {
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

            _diagnosticInterval = options?.Value.DiagnosticsInterval ?? TimeSpan.Zero;
            if (_diagnosticInterval == TimeSpan.Zero)
            {
                _diagnosticInterval = Timeout.InfiniteTimeSpan;
            }
            _diagnosticsOutputTimer = new Timer(DiagnosticsOutputTimer_Elapsed);
        }

        /// <inheritdoc/>
        public void ResetWriterGroup(string writerGroupId)
        {
            var diag = new AggregateDiagnosticModel { IngestionStart = DateTime.UtcNow };
            _diagnostics.AddOrUpdate(writerGroupId, _ => diag, (_, _) => diag);
            _logger.LogInformation("Tracking diagnostics for {WriterGroup} was (re-)started.",
                writerGroupId);
        }

        /// <inheritdoc/>
        public bool TryGetDiagnosticsForWriterGroup(string writerGroupId,
            [NotNullWhen(true)] out WriterGroupDiagnosticModel? diagnostic)
        {
            if (_diagnostics.TryGetValue(writerGroupId, out var value))
            {
                //
                // Ensure we collect all observable instruments then
                // return the aggregate model
                //
                _meterListener.RecordObservableInstruments();
                diagnostic = value.AggregateModel;
                return true;
            }
            diagnostic = default;
            return false;
        }

        /// <inheritdoc/>
        public bool RemoveWriterGroup(string writerGroupId)
        {
            if (_diagnostics.TryRemove(writerGroupId, out _))
            {
                _logger.LogInformation("Stop tracking diagnostics for {WriterGroup}.",
                    writerGroupId);
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public void Start()
        {
            _meterListener.Start();
            _diagnosticsOutputTimer.Change(_diagnosticInterval, _diagnosticInterval);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                _diagnosticsOutputTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            finally
            {
                _meterListener.Dispose();
                _diagnosticsOutputTimer.Dispose();
            }
        }

        /// <summary>
        /// Enable instrument if we need to hook
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="listener"></param>
        private void OnInstrumentPublished(Instrument instrument, MeterListener listener)
        {
            if (_bindings.ContainsKey(instrument.Name))
            {
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
            ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            if (_bindings.TryGetValue(instrument.Name, out var binding) &&
                TryGetIds(tags, out var writerGroupId, out var dataSetWriterId) &&
                _diagnostics.TryGetValue(writerGroupId, out var diag))
            {
                binding(dataSetWriterId != null ? diag[dataSetWriterId] : diag, measurement!);
            }
            static bool TryGetIds(ReadOnlySpan<KeyValuePair<string, object?>> tags,
                [NotNullWhen(true)] out string? writerGroupId, out string? dataSetWriterId)
            {
                writerGroupId = null;
                dataSetWriterId = null;
                for (var index = tags.Length; index > 0; index--) // Identifiers are at the end
                {
                    var entry = tags[index - 1];
                    if (entry.Value is string id)
                    {
                        switch (entry.Key)
                        {
                            case Constants.WriterGroupIdTag:
                                writerGroupId = id;
                                break;
                            case Constants.DataSetWriterIdTag:
                                dataSetWriterId = id;
                                break;
                        }
                    }
                    if (writerGroupId != null &&
                        dataSetWriterId != null)
                    {
                        return true;
                    }
                }
                return writerGroupId != null;
            }
        }

        /// <summary>
        /// Diagnostics timer to dump out all diagnostics
        /// </summary>
        /// <param name="state"></param>
        private void DiagnosticsOutputTimer_Elapsed(object? state)
        {
            var now = DateTime.UtcNow;
            _meterListener.RecordObservableInstruments();

            var builder = new StringBuilder();

            // Get all writers
            var diagnostics = _diagnostics
                .Select(kv => (kv.Key, kv.Value.AggregateModel));
            foreach (var (writerGroupId, info) in diagnostics)
            {
                builder = Append(builder, writerGroupId, info, now - info.IngestionStart);
            }
            Console.Out.WriteLine(builder.ToString());

            StringBuilder Append(StringBuilder builder, string writerGroupId,
                WriterGroupDiagnosticModel info, TimeSpan ingestionDuration)
            {
                var valueChangesPerSec = info.IngressValueChanges / ingestionDuration.TotalSeconds;
                var dataChangesPerSec = info.IngressDataChanges / ingestionDuration.TotalSeconds;
                var dataChangesLastMin = info.IngressDataChangesInLastMinute
                    .ToString("D2", CultureInfo.CurrentCulture);
                var valueChangesPerSecLastMin = info.IngressValueChangesInLastMinute /
                    Math.Min(ingestionDuration.TotalSeconds, 60d);
                var dataChangesPerSecLastMin = info.IngressDataChangesInLastMinute /
                    Math.Min(ingestionDuration.TotalSeconds, 60d);
                var version = GetType().Assembly.GetReleaseVersion().ToString();

                var dataChangesPerSecFormatted = info.IngressDataChanges > 0 && ingestionDuration.TotalSeconds > 0
        ? $"(All time ~{dataChangesPerSec:0.##}/s; {dataChangesLastMin} in last 60s ~{dataChangesPerSecLastMin:0.##}/s)"
                    : string.Empty;
                var valueChangesPerSecFormatted = info.IngressValueChanges > 0 && ingestionDuration.TotalSeconds > 0
        ? $"(All time ~{valueChangesPerSec:0.##}/s; {dataChangesLastMin} in last 60s ~{valueChangesPerSecLastMin:0.##}/s)"
                    : string.Empty;
                var sentMessagesPerSecFormatted = info.OutgressIoTMessageCount > 0 && ingestionDuration.TotalSeconds > 0
        ? $"({info.SentMessagesPerSec:0.##}/s)" : "";

                return builder.AppendLine()
                    .Append("  DIAGNOSTICS INFORMATION for          : ")
                        .Append(writerGroupId).Append(" (OPC Publisher ").Append(version)
                        .AppendLine(")")
                    .Append("  # Ingestion duration                 : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:dd\\:hh\\:mm\\:ss}", ingestionDuration)
                        .AppendLine(" (dd:hh:mm:ss)")
                    .Append("  # Ingress DataChanges (from OPC)     : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressDataChanges)
                        .Append(' ').AppendLine(dataChangesPerSecFormatted)
                    .Append("  # Ingress EventData (from OPC)       : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressEventNotifications)
                        .AppendLine()
                    .Append("  # Ingress ValueChanges (from OPC)    : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressValueChanges).Append(' ')
                        .AppendLine(valueChangesPerSecFormatted)
                    .Append("  # Ingress Events (from OPC)          : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.IngressEvents)
                        .AppendLine()
                    .Append("  # Ingress BatchBlock buffer size     : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.IngressBatchBlockBufferSize)
                        .AppendLine()
                    .Append("  # Encoding Block input/output size   : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.EncodingBlockInputSize).Append(" | ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0:0}", info.EncodingBlockOutputSize).AppendLine()
                    .Append("  # Encoder Notifications processed    : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.EncoderNotificationsProcessed)
                        .AppendLine()
                    .Append("  # Encoder Notifications dropped      : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.EncoderNotificationsDropped)
                        .AppendLine()
                    .Append("  # Encoder IoT Messages processed     : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.EncoderIoTMessagesProcessed)
                        .AppendLine()
                    .Append("  # Encoder avg Notifications/Message  : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.EncoderAvgNotificationsMessage)
                        .AppendLine()
                    .Append("  # Encoder worst Message split ratio  : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0.#}", info.EncoderMaxMessageSplitRatio)
                        .AppendLine()
                    .Append("  # Encoder avg IoT Message body size  : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.EncoderAvgIoTMessageBodySize)
                        .AppendLine()
                    .Append("  # Encoder avg IoT Chunk (4 KB) usage : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0.#}", info.EncoderAvgIoTChunkUsage)
                        .AppendLine()
                    .Append("  # Estimated IoT Chunks (4 KB) per day: ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.EstimatedIoTChunksPerDay)
                        .AppendLine()
                    .Append("  # Outgress input buffer count        : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.OutgressInputBufferCount)
                        .AppendLine()
                    .Append("  # Outgress input buffer dropped      : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.OutgressInputBufferDropped)
                        .AppendLine()
                    .Append("  # Outgress IoT message count         : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:n0}", info.OutgressIoTMessageCount)
                        .Append(' ').AppendLine(sentMessagesPerSecFormatted)
                    .Append("  # Connection retries                 : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.ConnectionRetries)
                        .AppendLine()
                    .Append("  # Opc endpoint connected?            : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.OpcEndpointConnected)
                        .AppendLine()
                    .Append("  # Monitored Opc nodes succeeded count: ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.MonitoredOpcNodesSucceededCount)
                        .AppendLine()
                    .Append("  # Monitored Opc nodes failed count   : ")
                        .AppendFormat(CultureInfo.CurrentCulture, "{0,14:0}", info.MonitoredOpcNodesFailedCount)
                        .AppendLine()
                    ;
            }
        }

        /// <summary>
        /// Aggregate diagnostics
        /// </summary>
        private sealed record class AggregateDiagnosticModel : WriterGroupDiagnosticModel
        {
            /// <summary>
            /// The aggregate including information from all writers
            /// </summary>
            internal WriterGroupDiagnosticModel AggregateModel
            {
                get
                {
                    return this with
                    {
                        MonitoredOpcNodesFailedCount = MonitoredOpcNodesFailedCount +
                            _writers.Values.Sum(w => w.MonitoredOpcNodesFailedCount),
                        MonitoredOpcNodesSucceededCount = MonitoredOpcNodesSucceededCount +
                            _writers.Values.Sum(w => w.MonitoredOpcNodesSucceededCount),
                        ConnectionRetries = (int)
                            _writers.Values.Average(w => w.ConnectionRetries),
                        OpcEndpointConnected = OpcEndpointConnected ||
                            _writers.Values.Any(w => w.OpcEndpointConnected)
                    };
                }
            }

            /// <summary>
            /// Get the writer diagnostics
            /// </summary>
            /// <param name="dataSetWriterId"></param>
            /// <returns></returns>
            public WriterGroupDiagnosticModel this[string dataSetWriterId] =>
                _writers.GetOrAdd(dataSetWriterId, new WriterGroupDiagnosticModel
                {
                    IngestionStart = DateTime.UtcNow
                });

            private readonly ConcurrentDictionary<string, WriterGroupDiagnosticModel> _writers = new();
        }

        private readonly Timer _diagnosticsOutputTimer;
        private readonly MeterListener _meterListener;
        private readonly ILogger _logger;
        private readonly TimeSpan _diagnosticInterval;
        private readonly ConcurrentDictionary<string, AggregateDiagnosticModel> _diagnostics = new();

        // TODO: Split this per measurement type to avoid boxing
        private readonly ConcurrentDictionary<string,
            Action<WriterGroupDiagnosticModel, object>> _bindings = new()
            {
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
                (d, i) => d.ConnectionRetries = (long)i

                // ... Add here more items if needed
            };
    }
}
