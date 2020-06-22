﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using Prometheus;
    using System.Text;

    /// <summary>
    /// Dataflow engine
    /// </summary>
    public class DataFlowProcessingEngine : IProcessingEngine, IDisposable {

        /// <inheritdoc/>
        public bool IsRunning { get; private set; }

        /// <inheritdoc/>
        public string Name => _messageTrigger.Id;

        /// <summary>
        /// Create engine
        /// </summary>
        /// <param name="messageTrigger"></param>
        /// <param name="encoder"></param>
        /// <param name="messageSink"></param>
        /// <param name="engineConfiguration"></param>
        /// <param name="logger"></param>
        /// <param name="identity"></param>
        public DataFlowProcessingEngine(IMessageTrigger messageTrigger, IMessageEncoder encoder,
            IMessageSink messageSink, IEngineConfiguration engineConfiguration, ILogger logger,
            IIdentity identity) {
            _config = engineConfiguration;
            _messageTrigger = messageTrigger;
            _messageSink = messageSink;
            _messageEncoder = encoder;
            _logger = logger;
            _identity = identity;

            if (_config.BatchSize.HasValue && _config.BatchSize.Value > 1) {
                _dataSetMessageBufferSize = _config.BatchSize.Value;
            }
            if (_config.MaxMessageSize.HasValue && _config.MaxMessageSize.Value > 0) {
                _maxEncodedMessageSize = _config.MaxMessageSize.Value;
            }

            _diagnosticInterval = _config.DiagnosticsInterval.GetValueOrDefault(TimeSpan.Zero);
            _batchTriggerInterval = _config.BatchTriggerInterval.GetValueOrDefault(TimeSpan.Zero);
            _diagnosticsOutputTimer = new Timer(DiagnosticsOutputTimer_Elapsed);
            _batchTriggerIntervalTimer = new Timer(BatchTriggerIntervalTimer_Elapsed);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _logger.Debug("Disposing {name}", Name);
            _diagnosticsOutputTimer?.Dispose();
            _batchTriggerIntervalTimer?.Dispose();
        }

        /// <inheritdoc/>
        public Task<VariantValue> GetCurrentJobState() {
            return Task.FromResult<VariantValue>(null);
        }

        /// <inheritdoc/>
        public async Task RunAsync(ProcessMode processMode, CancellationToken cancellationToken) {
            if (_messageEncoder == null) {
                throw new NotInitializedException();
            }
            try {
                if (IsRunning) {
                    return;
                }
                IsRunning = true;
                _encodingBlock = new TransformManyBlock<DataSetMessageModel[], NetworkMessageModel>(
                    async input => {
                        if (_batchTriggerInterval > TimeSpan.Zero) {
                            _batchTriggerIntervalTimer.Change(_batchTriggerInterval, Timeout.InfiniteTimeSpan);
                        }
                        try {
                            if (_dataSetMessageBufferSize == 1) {
                                return await _messageEncoder.EncodeAsync(input, _maxEncodedMessageSize).ConfigureAwait(false);
                            }
                            else {
                                return await _messageEncoder.EncodeBatchAsync(input, _maxEncodedMessageSize).ConfigureAwait(false);
                            }
                        }
                        catch (Exception e) {
                            _logger.Error(e, "Encoding failure");
                            return Enumerable.Empty<NetworkMessageModel>();
                        }
                    },
                    new ExecutionDataflowBlockOptions {
                        CancellationToken = cancellationToken
                    });

                _batchDataSetMessageBlock = new BatchBlock<DataSetMessageModel>(
                    _dataSetMessageBufferSize,
                    new GroupingDataflowBlockOptions {
                        CancellationToken = cancellationToken
                    });

                _batchNetworkMessageBlock = new BatchBlock<NetworkMessageModel>(
                    _networkMessageBufferSize,
                    new GroupingDataflowBlockOptions {
                        CancellationToken = cancellationToken
                    });

                _sinkBlock = new ActionBlock<NetworkMessageModel[]>(
                    async input => {
                        if (input != null && input.Any()) {
                            await _messageSink.SendAsync(input).ConfigureAwait(false);
                        }
                        else {
                            _logger.Information("Sink block in engine {Name} triggered with empty input",
                                Name);
                        }
                    },
                    new ExecutionDataflowBlockOptions {
                        CancellationToken = cancellationToken
                    });
                _batchDataSetMessageBlock.LinkTo(_encodingBlock);
                _encodingBlock.LinkTo(_batchNetworkMessageBlock);
                _batchNetworkMessageBlock.LinkTo(_sinkBlock);

                _messageTrigger.OnMessage += MessageTriggerMessageReceived;
                if (_diagnosticInterval > TimeSpan.Zero) {
                    _diagnosticsOutputTimer.Change(_diagnosticInterval, _diagnosticInterval);
                }

                await _messageTrigger.RunAsync(cancellationToken).ConfigureAwait(false);

            }
            finally {
                IsRunning = false;
                _messageTrigger.OnMessage -= MessageTriggerMessageReceived;
                _diagnosticsOutputTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _batchTriggerIntervalTimer.Change(Timeout.Infinite, Timeout.Infinite);
                await _sinkBlock.Completion;
            }
        }

        /// <inheritdoc/>
        public Task SwitchProcessMode(ProcessMode processMode, DateTime? timestamp) {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Diagnostics timer
        /// </summary>
        /// <param name="state"></param>
        private void DiagnosticsOutputTimer_Elapsed(object state) {
            var totalDuration = _diagnosticStart != DateTime.MinValue ? (DateTime.UtcNow - _diagnosticStart).TotalSeconds : 0;

            _logger.Debug("Identity {deviceId}; {moduleId}", _identity.DeviceId, _identity.ModuleId);

            var diagInfo = new StringBuilder();
            diagInfo.Append("\n   DIAGNOSTICS INFORMATION for      : {host}\n");
            diagInfo.Append("   # Ingestion duration               : {duration,14:dd\\:hh\\:mm\\:ss} (dd:hh:mm:ss)\n");
            string dataChangesAverage = _messageTrigger.DataChangesCount > 0 && totalDuration > 0 ? $" ({_messageTrigger.DataChangesCount / totalDuration:0.##}/s)" : "";
            diagInfo.Append("   # Ingress DataChanges (from OPC)   : {dataChangesCount,14:0}{dataChangesAverage}\n");
            string valueChangesAverage = _messageTrigger.ValueChangesCount > 0 && totalDuration > 0 ? $" ({_messageTrigger.ValueChangesCount / totalDuration:0.##}/s)" : "";
            diagInfo.Append("   # Ingress ValueChanges (from OPC)  : {valueChangesCount,14:0}{valueChangesAverage}\n");

            diagInfo.Append("   # Ingress BatchBlock buffer size   : {batchDataSetMessageBlockOutputCount,14:0}\n");
            diagInfo.Append("   # Encoding Block input/output size : {encodingBlockInputCount,14:0} | {encodingBlockOutputCount:0}\n");
            diagInfo.Append("   # Encoder Notifications processed  : {notificationsProcessedCount,14:0}\n");
            diagInfo.Append("   # Encoder Notifications dropped    : {notificationsDroppedCount,14:0}\n");
            diagInfo.Append("   # Encoder IoT Messages processed   : {messagesProcessedCount,14:0}\n");
            diagInfo.Append("   # Encoder avg Notifications/Message: {notificationsPerMessage,14:0}\n");
            diagInfo.Append("   # Encoder avg IoT Message body size: {messageSizeAverage,14:0}\n");
            diagInfo.Append("   # Outgress Batch Block buffer size : {batchNetworkMessageBlockOutputCount,14:0}\n");
            diagInfo.Append("   # Outgress input buffer count      : {sinkBlockInputCount,14:0}\n");

            string sentMessagesAverage = _messageSink.SentMessagesCount > 0 && totalDuration > 0 ? $" ({_messageSink.SentMessagesCount / totalDuration:0.##}/s)" : "";
            diagInfo.Append("   # Outgress IoT message count       : {messageSinkSentMessagesCount,14:0}{sentMessagesAverage}\n");
            diagInfo.Append("   # Connection retries               : {connectionRetries,14:0}\n");

            _logger.Information(diagInfo.ToString(),
                Name,
                TimeSpan.FromSeconds(totalDuration),
                _messageTrigger.DataChangesCount, dataChangesAverage,
                _messageTrigger.ValueChangesCount, valueChangesAverage,
                _batchDataSetMessageBlock.OutputCount,
                _encodingBlock.InputCount, _encodingBlock.OutputCount,
                _messageEncoder.NotificationsProcessedCount,
                _messageEncoder.NotificationsDroppedCount,
                _messageEncoder.MessagesProcessedCount,
                _messageEncoder.AvgNotificationsPerMessage,
                _messageEncoder.AvgMessageSize,
                _batchNetworkMessageBlock.OutputCount,
                _sinkBlock.InputCount,
                _messageSink.SentMessagesCount, sentMessagesAverage,
                _messageTrigger.NumberOfConnectionRetries);

            kDataChangesCount.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_messageTrigger.DataChangesCount);
            kDataChangesPerSecond.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_messageTrigger.DataChangesCount / totalDuration);
            kValueChangesCount.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_messageTrigger.ValueChangesCount);
            kValueChangesPerSecond.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_messageTrigger.ValueChangesCount / totalDuration);
            kNotificationsProcessedCount.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_messageEncoder.NotificationsProcessedCount);
            kNotificationsDroppedCount.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_messageEncoder.NotificationsDroppedCount);
            kMessagesProcessedCount.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_messageEncoder.MessagesProcessedCount);
            kNotificationsPerMessageAvg.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_messageEncoder.AvgNotificationsPerMessage);
            kMesageSizeAvg.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_messageEncoder.AvgMessageSize);
            kIoTHubQueueBuffer.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_sinkBlock.InputCount);
            kSentMessagesCount.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_messageSink.SentMessagesCount);
            kNumberOfConnectionRetries.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_messageTrigger.NumberOfConnectionRetries);
        }

        /// <summary>
        /// Batch trigger interval
        /// </summary>
        /// <param name="state"></param>
        private void BatchTriggerIntervalTimer_Elapsed(object state) {
            _batchDataSetMessageBlock?.TriggerBatch();
        }

        /// <summary>
        /// Message received handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void MessageTriggerMessageReceived(object sender, DataSetMessageModel args) {
            if (_diagnosticStart == DateTime.MinValue) {
                _diagnosticStart = DateTime.UtcNow;

                if (_batchTriggerInterval > TimeSpan.Zero) {
                    _batchTriggerIntervalTimer.Change(_batchTriggerInterval, Timeout.InfiniteTimeSpan);
                }
            }
            _batchDataSetMessageBlock.Post(args);
        }

        private readonly int _dataSetMessageBufferSize = 1;
        private readonly int _networkMessageBufferSize = 1;
        private readonly Timer _batchTriggerIntervalTimer;
        private readonly TimeSpan _batchTriggerInterval;

        private readonly int _maxEncodedMessageSize = 256 * 1024;

        private readonly IEngineConfiguration _config;
        private readonly IMessageSink _messageSink;
        private readonly IMessageEncoder _messageEncoder;
        private readonly IMessageTrigger _messageTrigger;
        private readonly ILogger _logger;
        private readonly IIdentity _identity;

        private BatchBlock<DataSetMessageModel> _batchDataSetMessageBlock;
        private BatchBlock<NetworkMessageModel> _batchNetworkMessageBlock;

        private readonly Timer _diagnosticsOutputTimer;
        private readonly TimeSpan _diagnosticInterval;
        private DateTime _diagnosticStart = DateTime.MinValue;

        private TransformManyBlock<DataSetMessageModel[], NetworkMessageModel> _encodingBlock;
        private ActionBlock<NetworkMessageModel[]> _sinkBlock;

        private static readonly GaugeConfiguration kGaugeConfig = new GaugeConfiguration {
            LabelNames = new[] { "deviceid", "module", "triggerid" }
        };
        private static readonly Gauge kValueChangesCount = Metrics.CreateGauge(
            "iiot_edge_publisher_value_changes", "Opc ValuesChanges delivered for processing", kGaugeConfig);
        private static readonly Gauge kValueChangesPerSecond = Metrics.CreateGauge(
            "iiot_edge_publisher_value_changes_per_second", "Opc ValuesChanges/second delivered for processing", kGaugeConfig);
        private static readonly Gauge kDataChangesCount = Metrics.CreateGauge(
            "iiot_edge_publisher_data_changes", "Opc DataChanges delivered for processing", kGaugeConfig);
        private static readonly Gauge kDataChangesPerSecond = Metrics.CreateGauge(
            "iiot_edge_publisher_data_changes_per_second", "Opc DataChanges/second delivered for processing", kGaugeConfig);
        private static readonly Gauge kIoTHubQueueBuffer = Metrics.CreateGauge(
            "iiot_edge_publisher_iothub_queue_size", "IoT messages queued sending", kGaugeConfig);
        private static readonly Gauge kSentMessagesCount = Metrics.CreateGauge(
            "iiot_edge_publisher_sent_iot_messages", "IoT messages sent to hub", kGaugeConfig);
        private static readonly Gauge kNumberOfConnectionRetries = Metrics.CreateGauge(
            "iiot_edge_publisher_connection_retries", "OPC UA connect retries", kGaugeConfig);

        private static readonly Gauge kNotificationsProcessedCount = Metrics.CreateGauge(
            "iiot_edge_publisher_encoded_notifications", "publisher engine encoded opc notifications count", kGaugeConfig);
        private static readonly Gauge kNotificationsDroppedCount = Metrics.CreateGauge(
            "iiot_edge_publisher_dropped_notifications", "publisher engine dropped opc notifications count", kGaugeConfig);
        private static readonly Gauge kMessagesProcessedCount = Metrics.CreateGauge(
            "iiot_edge_publisher_processed_messages", "publisher engine processed iot messages count", kGaugeConfig);
        private static readonly Gauge kNotificationsPerMessageAvg = Metrics.CreateGauge(
            "iiot_edge_publisher_notifications_per_message_average",
            "publisher engine opc notifications per iot message average", kGaugeConfig);
        private static readonly Gauge kMesageSizeAvg = Metrics.CreateGauge(
            "iiot_edge_publisher_encoded_message_size_average",
            "publisher engine iot message encoded body size average", kGaugeConfig);
    }
}
