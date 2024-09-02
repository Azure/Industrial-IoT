// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Autofac;
    using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Metrics;

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
        /// <param name="resources"></param>
        /// <param name="timeProvider"></param>
        public PublisherDiagnosticCollector(ILogger<PublisherDiagnosticCollector> logger,
            IResourceMonitor? resources = null, TimeProvider? timeProvider = null)
        {
            _resources = resources;
            _logger = logger;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _meterListener = new MeterListener
            {
                InstrumentPublished = OnInstrumentPublished
            };
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
            var diag = new WriterGroupDiagnosticModel
            {
                PublisherVersion = PublisherConfig.Version,
                IngestionStart = _timeProvider.GetUtcNow()
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
                var duration = _timeProvider.GetUtcNow() - value.IngestionStart;

                if (_resources != null)
                {
                    var resources = _resources.GetUtilization(TimeSpan.FromSeconds(5));

                    diagnostic = value with
                    {
                        IngestionDuration = duration,
                        OpcEndpointConnected = value.NumberOfConnectedEndpoints != 0,

                        MemoryUsedPercentage =
                            resources.MemoryUsedPercentage,
                        MemoryUsedInBytes =
                            resources.MemoryUsedInBytes,
                        CpuUsedPercentage =
                            resources.CpuUsedPercentage,
                        GuaranteedCpuUnits =
                            resources.SystemResources.GuaranteedCpuUnits,
                        MaximumCpuUnits =
                            resources.SystemResources.MaximumCpuUnits,
                        GuaranteedMemoryInBytes =
                            resources.SystemResources.GuaranteedMemoryInBytes,
                        MaximumMemoryInBytes =
                            resources.SystemResources.MaximumMemoryInBytes
                    };
                }
                else
                {
                    diagnostic = value with
                    {
                        IngestionDuration = duration,
                        OpcEndpointConnected = value.NumberOfConnectedEndpoints != 0,
                    };
                }
                return true;
            }
            diagnostic = default;
            return false;
        }

        /// <inheritdoc/>
        public IEnumerable<(string, WriterGroupDiagnosticModel)> EnumerateDiagnostics()
        {
            var now = _timeProvider.GetUtcNow();
            _meterListener.RecordObservableInstruments();

            foreach (var (writerGroupId, info) in _diagnostics)
            {
                var duration = now - info.IngestionStart;

                if (_resources == null)
                {
                    yield return (writerGroupId, info with
                    {
                        Timestamp = now,
                        IngestionDuration = duration,
                        OpcEndpointConnected = info.NumberOfConnectedEndpoints != 0,
                    });
                }
                else
                {
                    var resources = _resources.GetUtilization(TimeSpan.FromSeconds(5));
                    yield return (writerGroupId, info with
                    {
                        Timestamp = now,
                        IngestionDuration = duration,
                        OpcEndpointConnected = info.NumberOfConnectedEndpoints != 0,

                        MemoryUsedPercentage =
                            resources.MemoryUsedPercentage,
                        MemoryUsedInBytes =
                            resources.MemoryUsedInBytes,
                        CpuUsedPercentage =
                            resources.CpuUsedPercentage,
                        GuaranteedCpuUnits =
                            resources.SystemResources.GuaranteedCpuUnits,
                        MaximumCpuUnits =
                            resources.SystemResources.MaximumCpuUnits,
                        GuaranteedMemoryInBytes =
                            resources.SystemResources.GuaranteedMemoryInBytes,
                        MaximumMemoryInBytes =
                            resources.SystemResources.MaximumMemoryInBytes
                    });
                }
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
                TryGetIds(tags, out var writerGroupId, out var writerGroupName) &&
                _diagnostics.TryGetValue(writerGroupId, out var diag))
            {
                if (writerGroupName != null)
                {
                    diag.WriterGroupName = writerGroupName;
                }
                binding(diag, measurement!);
            }
            static bool TryGetIds(ReadOnlySpan<KeyValuePair<string, object?>> tags,
                [NotNullWhen(true)] out string? writerGroupId, out string? writerGroupName)
            {
                writerGroupId = null;
                writerGroupName = null;
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
                            case Constants.WriterGroupNameTag:
                                writerGroupName = id;
                                break;
                        }
                    }
                    if (writerGroupId != null &&
                        writerGroupName != null)
                    {
                        return true;
                    }
                }
                return writerGroupId != null;
            }
        }

        private readonly MeterListener _meterListener;
        private readonly IResourceMonitor? _resources;
        private readonly ILogger _logger;
        private readonly TimeProvider _timeProvider;
        private readonly ConcurrentDictionary<string, WriterGroupDiagnosticModel> _diagnostics = new();

        // TODO: Split this per measurement type to avoid boxing
        private readonly ConcurrentDictionary<string,
            Action<WriterGroupDiagnosticModel, object>> _bindings = new()
            {
                ["iiot_edge_publisher_writer_count"] =
                    (d, i) => d.NumberOfWriters = (int)i,
                ["iiot_edge_publisher_writer_nodes"] =
                    (d, i) => d.MonitoredOpcNodesCount = (int)i,
                ["iiot_edge_publisher_writer_good_nodes"] =
                    (d, i) => d.MonitoredOpcNodesSucceededCount = (int)i,
                ["iiot_edge_publisher_writer_bad_nodes"] =
                    (d, i) => d.MonitoredOpcNodesFailedCount = (int)i,
                ["iiot_edge_publisher_writer_late_nodes"] =
                    (d, i) => d.MonitoredOpcNodesLateCount = (int)i,
                ["iiot_edge_publisher_writer_heartbeat_enabled_nodes"] =
                    (d, i) => d.ActiveHeartbeatCount = (int)i,
                ["iiot_edge_publisher_writer_condition_enabled_nodes"] =
                    (d, i) => d.ActiveConditionCount = (int)i,

                ["iiot_edge_publisher_publish_requests_client_totals"] =
                    (d, i) => d.TotalPublishRequests = (int)i,
                ["iiot_edge_publisher_good_publish_requests_client_totals"] =
                    (d, i) => d.TotalGoodPublishRequests = (int)i,
                ["iiot_edge_publisher_bad_publish_requests_client_totals"] =
                    (d, i) => d.TotalBadPublishRequests = (int)i,
                ["iiot_edge_publisher_min_publish_requests_client_totals"] =
                    (d, i) => d.TotalMinPublishRequests = (int)i,

                ["iiot_edge_publisher_is_connection_ok"] =
                    (d, i) => d.NumberOfConnectedEndpoints = (int)i,
                ["iiot_edge_publisher_is_disconnected"] =
                    (d, i) => d.NumberOfDisconnectedEndpoints = (int)i,
                ["iiot_edge_publisher_connection_retries"] =
                    (d, i) => d.ConnectionRetries = (long)i,
                ["iiot_edge_publisher_connections"] =
                    (d, i) => d.ConnectionCount = (long)i,

                ["iiot_edge_publisher_keep_alive_notifications"] =
                    (d, i) => d.IngressKeepAliveNotifications = (long)i,
                ["iiot_edge_publisher_queue_overflows"] =
                    (d, i) => d.ServerQueueOverflows = (long)i,
                ["iiot_edge_publisher_queue_overflows_per_second_last_min"] =
                    (d, i) => d.ServerQueueOverflowsInLastMinute = (long)i,
                ["iiot_edge_publisher_data_changes"] =
                    (d, i) => d.IngressDataChanges = (long)i,
                ["iiot_edge_publisher_data_changes_per_second_last_min"] =
                    (d, i) => d.IngressDataChangesInLastMinute = (long)i,
                ["iiot_edge_publisher_value_changes"] =
                    (d, i) => d.IngressValueChanges = (long)i,
                ["iiot_edge_publisher_value_changes_per_second_last_min"] =
                    (d, i) => d.IngressValueChangesInLastMinute = (long)i,
                ["iiot_edge_publisher_sampledvalues"] =
                    (d, i) => d.IngressSampledValues = (long)i,
                ["iiot_edge_publisher_sampledvalues_per_second_last_min"] =
                    (d, i) => d.IngressSampledValuesInLastMinute = (long)i,
                ["iiot_edge_publisher_events"] =
                    (d, i) => d.IngressEvents = (long)i,
                ["iiot_edge_publisher_events_per_second_last_min"] =
                    (d, i) => d.IngressEventsInLastMinute = (long)i,
                ["iiot_edge_publisher_heartbeats"] =
                    (d, i) => d.IngressHeartbeats = (long)i,
                ["iiot_edge_publisher_heartbeats_per_second_last_min"] =
                    (d, i) => d.IngressHeartbeatsInLastMinute = (long)i,
                ["iiot_edge_publisher_cyclicreads"] =
                    (d, i) => d.IngressCyclicReads = (long)i,
                ["iiot_edge_publisher_cyclicreads_per_second_last_min"] =
                    (d, i) => d.IngressCyclicReadsInLastMinute = (long)i,
                ["iiot_edge_publisher_modelchanges"] =
                    (d, i) => d.IngressModelChanges = (long)i,
                ["iiot_edge_publisher_modelchanges_per_second_last_min"] =
                    (d, i) => d.IngressModelChangesInLastMinute = (long)i,
                ["iiot_edge_publisher_event_notifications"] =
                    (d, i) => d.IngressEventNotifications = (long)i,
                ["iiot_edge_publisher_event_notifications_per_second_last_min"] =
                    (d, i) => d.IngressEventNotificationsInLastMinute = (long)i,

                ["iiot_edge_publisher_publish_queue_dropped_count"] =
                    (d, i) => d.IngressNotificationsDropped = (long)i,
                ["iiot_edge_publisher_batch_input_queue_size"] =
                    (d, i) => d.IngressBatchBlockBufferSize = (long)i,
                ["iiot_edge_publisher_send_queue_size"] =
                    (d, i) => d.OutgressInputBufferCount = (long)i,
                ["iiot_edge_publisher_send_queue_dropped_count"] =
                    (d, i) => d.OutgressInputBufferDropped = (long)i,

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
                ["iiot_edge_publisher_chunk_size_average"] =
                    (d, i) => d.EncoderAvgIoTChunkUsage = (double)i,

                ["iiot_edge_publisher_partitions_count"] =
                    (d, i) => d.TotalPublishQueuePartitions = (int)i,
                ["iiot_edge_publisher_partitions_active"] =
                    (d, i) => d.ActivePublishQueuePartitions = (int)i,
                ["iiot_edge_publisher_estimated_message_chunks_per_day"] =
                    (d, i) => d.EstimatedIoTChunksPerDay = (double)i,
                ["iiot_edge_publisher_messages_per_second"] =
                    (d, i) => d.SentMessagesPerSec = (double)i,
                ["iiot_edge_publisher_messages"] =
                    (d, i) => d.OutgressIoTMessageCount = (long)i,
                ["iiot_edge_publisher_message_send_failures"] =
                    (d, i) => d.OutgressIoTMessageFailedCount = (long)i

                    // ... Add here more items if needed
            };
    }
}
