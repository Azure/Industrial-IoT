﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Autofac;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Metrics;
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
        public PublisherDiagnosticCollector(ILogger<PublisherDiagnosticCollector> logger)
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
        }

        /// <inheritdoc/>
        public void ResetWriterGroup(string writerGroupId)
        {
            var diag = new AggregateDiagnosticModel
            {
                PublisherVersion = PublisherConfig.Version,
                IngestionStart = DateTime.UtcNow
            };
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
        public IEnumerable<(string, WriterGroupDiagnosticModel)> EnumerateDiagnostics()
        {
            var now = DateTime.UtcNow;
            _meterListener.RecordObservableInstruments();

            foreach (var (writerGroupId, info) in _diagnostics
                 .Select(kv => (kv.Key, kv.Value.AggregateModel)))
            {
                yield return (writerGroupId, info with
                {
                    Timestamp = now,
                    IngestionDuration = now - info.IngestionStart,
                });
            }
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
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _meterListener.Dispose();
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
                    var writers = _writers.Values; // Snapshot writers
                    return this with
                    {
                        MonitoredOpcNodesFailedCount = MonitoredOpcNodesFailedCount +
                            writers.Sum(w => w.MonitoredOpcNodesFailedCount),
                        MonitoredOpcNodesSucceededCount = MonitoredOpcNodesSucceededCount +
                            writers.Sum(w => w.MonitoredOpcNodesSucceededCount),
                        OpcEndpointConnected = NumberOfConnectedEndpoints != 0,
                        PublishRequestsRatio = PublishRequestsRatio +
                            writers.Sum(w => w.PublishRequestsRatio),
                        BadPublishRequestsRatio = BadPublishRequestsRatio +
                            writers.Sum(w => w.BadPublishRequestsRatio),
                        GoodPublishRequestsRatio = GoodPublishRequestsRatio +
                            writers.Sum(w => w.GoodPublishRequestsRatio),
                        MinPublishRequestsRatio = MinPublishRequestsRatio +
                            writers.Sum(w => w.MinPublishRequestsRatio),
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
                    PublisherVersion = PublisherConfig.Version,
                    IngestionStart = DateTime.UtcNow
                });

            private readonly ConcurrentDictionary<string, WriterGroupDiagnosticModel> _writers = new();
        }

        private readonly MeterListener _meterListener;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, AggregateDiagnosticModel> _diagnostics = new();

        // TODO: Split this per measurement type to avoid boxing
        private readonly ConcurrentDictionary<string,
            Action<WriterGroupDiagnosticModel, object>> _bindings = new()
            {
                ["iiot_edge_publisher_messages"] =
                (d, i) => d.OutgressIoTMessageCount = (long)i,
                ["iiot_edge_publisher_data_changes"] =
                (d, i) => d.IngressDataChanges = (long)i,
                ["iiot_edge_publisher_value_changes"] =
                (d, i) => d.IngressValueChanges = (long)i,
                ["iiot_edge_publisher_data_changes_per_second_last_min"] =
                (d, i) => d.IngressDataChangesInLastMinute = (long)i,
                ["iiot_edge_publisher_value_changes_per_second_last_min"] =
                (d, i) => d.IngressValueChangesInLastMinute = (long)i,
                ["iiot_edge_publisher_keep_alive_notifications"] =
                (d, i) => d.IngressKeepAliveNotifications = (long)i,
                ["iiot_edge_publisher_events"] =
                (d, i) => d.IngressEvents = (long)i,
                ["iiot_edge_publisher_heartbeats"] =
                (d, i) => d.IngressHeartbeats = (long)i,
                ["iiot_edge_publisher_cyclicreads"] =
                (d, i) => d.IngressCyclicReads = (long)i,
                ["iiot_edge_publisher_event_notifications"] =
                (d, i) => d.IngressEventNotifications = (long)i,
                ["iiot_edge_publisher_unassigned_notification_count"] =
                (d, i) => d.IngressUnassignedChanges = (long)i,
                ["iiot_edge_publisher_estimated_message_chunks_per_day"] =
                (d, i) => d.EstimatedIoTChunksPerDay = (double)i,
                ["iiot_edge_publisher_messages_per_second"] =
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
                (d, i) => d.NumberOfConnectedEndpoints = (int)i,
                ["iiot_edge_publisher_is_disconnected"] =
                (d, i) => d.NumberOfDisconnectedEndpoints = (int)i,
                ["iiot_edge_publisher_connection_retries"] =
                (d, i) => d.ConnectionRetries = (long)i,
                ["iiot_edge_publisher_subscriptions"] =
                (d, i) => d.NumberOfSubscriptions = (long)i,
                ["iiot_edge_publisher_publish_requests_per_subscription"] =
                (d, i) => d.PublishRequestsRatio = (double)i,
                ["iiot_edge_publisher_good_publish_requests_per_subscription"] =
                (d, i) => d.GoodPublishRequestsRatio = (double)i,
                ["iiot_edge_publisher_bad_publish_requests_per_subscription"] =
                (d, i) => d.BadPublishRequestsRatio = (double)i,
                ["iiot_edge_publisher_min_publish_requests_per_subscription"] =
                (d, i) => d.MinPublishRequestsRatio = (double)i

                // ... Add here more items if needed
            };
    }
}
