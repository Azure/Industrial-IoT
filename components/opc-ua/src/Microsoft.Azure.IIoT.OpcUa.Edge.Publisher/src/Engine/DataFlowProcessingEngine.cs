// ------------------------------------------------------------
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
            _maxEgressMessageQueue = _config.MaxEgressMessageQueue.GetValueOrDefault(4096); // = 2 GB / 2 / (256 * 1024)
            _logger.Information($"Max. egress message queue: {_maxEgressMessageQueue} messages ({_maxEgressMessageQueue * 256 / 1024} MB)");
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
                        try {
                            if (_dataSetMessageBufferSize == 1) {
                                return await _messageEncoder.EncodeAsync(input, _maxEncodedMessageSize - _encodedMessageSizeOverhead).ConfigureAwait(false);
                            }
                            else {
                                return await _messageEncoder.EncodeBatchAsync(input, _maxEncodedMessageSize - _encodedMessageSizeOverhead).ConfigureAwait(false);
                            }
                        }
                        catch (Exception e) {
                            _logger.Error(e, "Encoding failure");
                            return Enumerable.Empty<NetworkMessageModel>();
                        }
                    },
                    new ExecutionDataflowBlockOptions {
                        CancellationToken = cancellationToken,
                    });

                _batchDataSetMessageBlock = new BatchBlock<DataSetMessageModel>(
                    _dataSetMessageBufferSize,
                    new GroupingDataflowBlockOptions {
                        CancellationToken = cancellationToken,
                    });

                _batchNetworkMessageBlock = new BatchBlock<NetworkMessageModel>(
                    _networkMessageBufferSize,
                    new GroupingDataflowBlockOptions {
                        CancellationToken = cancellationToken,
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
                        CancellationToken = cancellationToken,
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
            double totalDuration = _diagnosticStart != DateTime.MinValue ? (DateTime.UtcNow - _diagnosticStart).TotalSeconds : 0;
            double valueChangesPerSec = totalDuration > 0 ? _messageTrigger.ValueChangesCount / totalDuration : 0;
            double dataChangesPerSec = totalDuration > 0 ? _messageTrigger.DataChangesCount / totalDuration : 0;
            double sentMessagesPerSec = totalDuration > 0 ? _messageSink.SentMessagesCount / totalDuration : 0;
            string messageSizeAveragePercent = $"({_messageEncoder.AvgMessageSize / _maxEncodedMessageSize:P0})";
            double chunkSizeAverage = _messageEncoder.AvgMessageSize / (4 * 1024);
            double estimatedMsgChunksPerDay = Math.Ceiling(chunkSizeAverage) * sentMessagesPerSec * 60 * 60 * 24;

            // Account for report inaccuracies due to timing.
            double estimatedNotificationsSent = Math.Min(
                _messageEncoder.AvgNotificationsPerMessage * _messageSink.SentMessagesCount,
                _messageTrigger.ValueChangesCount);

            _logger.Debug("Identity {deviceId}; {moduleId}", _identity.DeviceId, _identity.ModuleId);

            var diagInfo = new StringBuilder();
            diagInfo.AppendLine("\n  DIAGNOSTICS INFORMATION for          : {host}");
            diagInfo.AppendLine("  # Ingestion duration                 : {duration,14:dd\\:hh\\:mm\\:ss} (dd:hh:mm:ss)");
            string dataChangesPerSecFormatted = _messageTrigger.DataChangesCount > 0 ? $"({dataChangesPerSec:0.##}/s)" : "";
            diagInfo.AppendLine("  | Ingress OPC DataChanges            : {dataChangesCount,14:n0} {dataChangesPerSecFormatted}");
            string valueChangesPerSecFormatted = _messageTrigger.ValueChangesCount > 0 ? $"({valueChangesPerSec:0.##}/s)" : "";
            diagInfo.AppendLine("  | Ingress OPC ValueChanges           : {valueChangesCount,14:n0} {valueChangesPerSecFormatted}");

            diagInfo.AppendLine("  v Ingress BatchBlock queue           : {batchDataSetMessageBlockOutputCount,14:0} batches");
            string batchDataOutputValues = _encodingBlock.OutputCount > 0 ? $" (~{_encodingBlock.OutputCount * _messageEncoder.AvgNotificationsPerMessage:n0} OPC values)" : "";
            diagInfo.AppendLine("  | Encoding block input | output queue: {encodingBlockInputCount,14:0} batches | {encodingBlockOutputCount:0} messages" + batchDataOutputValues);
            diagInfo.AppendLine("  | Encoder processed                  : {notificationsProcessedCount,14:n0} OPC values");
            diagInfo.AppendLine("  | Encoder dropped                    : {notificationsDroppedCount,14:n0} OPC values");
            diagInfo.AppendLine("  | Encoder IoT Hub processed          : {messagesProcessedCount,14:n0} messages");
            diagInfo.AppendLine("  | Encoder avg IoT Hub message        : {AvgNotificationsPerMessage,14:0} OPC values/message");
            diagInfo.AppendLine("  | Encoder avg IoT Hub message body   : {AvgMessageSize,14:n0} bytes {messageSizeAveragePercent}");
            diagInfo.AppendLine("  v Encoder avg IoT Hub usage          : {chunkSizeAverage,14:0.#} 4-KB chunks");
            string batchNetworkOutputValues = _batchNetworkMessageBlock.OutputCount > 0 ? $" (~{_batchNetworkMessageBlock.OutputCount * _messageEncoder.AvgNotificationsPerMessage:n0} OPC values)" : "";
            diagInfo.AppendLine("  | Egress BatchBlock output queue     : {batchNetworkMessageBlockOutputCount,14:0} messages" + batchNetworkOutputValues);
            string sinkBlockInputValues = _sinkBlock.InputCount > 0 ? $" (~{_sinkBlock.InputCount * _messageEncoder.AvgNotificationsPerMessage:n0} OPC values)" : "";
            diagInfo.AppendLine("  | Egress IoT Hub queue               : {sinkBlockInputCount,14:n0} messages" + sinkBlockInputValues);
            string egressDroppedPercent = _sinkBlockInputDroppedCount > 0 ? $" ({GetValuePercentage(_sinkBlockInputDroppedCount)})" : "";
            diagInfo.AppendLine("  | Egress dropped                     : {sinkBlockInputDroppedCount,14:n0} OPC values" + egressDroppedPercent);

            string sentMessagesPerSecFormatted = _messageSink.SentMessagesCount > 0 ? $"({sentMessagesPerSec:0.##}/s)" : "";
            diagInfo.AppendLine("  v Egress IoT Hub sent                : {SentMessagesCount,14:n0} messages {sentMessagesPerSecFormatted}");
            diagInfo.AppendLine("  # Calculated IoT Hub egress          : {estimatedNotificationsSent,14:n0} OPC values" + $" ({GetValuePercentage(estimatedNotificationsSent)})");
            diagInfo.AppendLine("  # Estimated IoT Hub usage            : {estimatedMsgChunksPerDay,14:n0} 4-KB chunks per day");
            diagInfo.AppendLine("  # Connection retries                 : {NumberOfConnectionRetries,14:0}");

            _logger.Information(diagInfo.ToString(),
                Name, // host
                TimeSpan.FromSeconds(totalDuration), // duration
                _messageTrigger.DataChangesCount, dataChangesPerSecFormatted,
                _messageTrigger.ValueChangesCount, valueChangesPerSecFormatted,
                _batchDataSetMessageBlock.OutputCount, // batchDataSetMessageBlockOutputCount
                _encodingBlock.InputCount, _encodingBlock.OutputCount, // encodingBlockInputCount | encodingBlockOutputCount
                // Account for report inaccuracies due to timing.
                Math.Min(_messageEncoder.NotificationsProcessedCount,
                    _messageTrigger.ValueChangesCount), // NotificationsProcessedCount
                _messageEncoder.NotificationsDroppedCount,
                _messageEncoder.MessagesProcessedCount,
                _messageEncoder.AvgNotificationsPerMessage,
                _messageEncoder.AvgMessageSize, messageSizeAveragePercent,
                chunkSizeAverage,
                _batchNetworkMessageBlock.OutputCount, // batchNetworkMessageBlockOutputCount
                _sinkBlock.InputCount, // sinkBlockInputCount
                _sinkBlockInputDroppedCount,
                _messageSink.SentMessagesCount, sentMessagesPerSecFormatted,
                estimatedNotificationsSent,
                estimatedMsgChunksPerDay,
                _messageTrigger.NumberOfConnectionRetries);

            string deviceId = _identity.DeviceId ?? "";
            string moduleId = _identity.ModuleId ?? "";
            kDataChangesCount.WithLabels(deviceId, moduleId, Name)
                .Set(_messageTrigger.DataChangesCount);
            kDataChangesPerSecond.WithLabels(deviceId, moduleId, Name)
                .Set(dataChangesPerSec);
            kValueChangesCount.WithLabels(deviceId, moduleId, Name)
                .Set(_messageTrigger.ValueChangesCount);
            kValueChangesPerSecond.WithLabels(deviceId, moduleId, Name)
                .Set(valueChangesPerSec);
            kNotificationsProcessedCount.WithLabels(deviceId, moduleId, Name)
                .Set(_messageEncoder.NotificationsProcessedCount);
            kNotificationsDroppedCount.WithLabels(deviceId, moduleId, Name)
                .Set(_messageEncoder.NotificationsDroppedCount);
            kMessagesProcessedCount.WithLabels(deviceId, moduleId, Name)
                .Set(_messageEncoder.MessagesProcessedCount);
            kNotificationsPerMessageAvg.WithLabels(deviceId, moduleId, Name)
                .Set(_messageEncoder.AvgNotificationsPerMessage);
            kMessageSizeAvg.WithLabels(deviceId, moduleId, Name)
                .Set(_messageEncoder.AvgMessageSize);
            kIoTHubQueueBuffer.WithLabels(deviceId, moduleId, Name)
                .Set(_sinkBlock.InputCount);
            kIoTHubQueueBufferDroppedCount.WithLabels(deviceId, moduleId, Name)
                .Set(_sinkBlockInputDroppedCount);
            kSentMessagesCount.WithLabels(deviceId, moduleId, Name)
                .Set(_messageSink.SentMessagesCount);
            kSentMessagesPerSecond.WithLabels(deviceId, moduleId, Name)
                .Set(sentMessagesPerSec);
            kNumberOfConnectionRetries.WithLabels(deviceId, moduleId, Name)
                .Set(_messageTrigger.NumberOfConnectionRetries);
            kChunkSizeAvg.WithLabels(deviceId, moduleId, Name)
                .Set(chunkSizeAverage);
            kEstimatedMsgChunksPerday.WithLabels(deviceId, moduleId, Name)
                .Set(estimatedMsgChunksPerDay);
        }

        /// <summary>
        /// Batch trigger interval
        /// </summary>
        /// <param name="state"></param>
        private void BatchTriggerIntervalTimer_Elapsed(object state) {
            if (_batchTriggerInterval > TimeSpan.Zero) {
                _batchTriggerIntervalTimer.Change(_batchTriggerInterval, Timeout.InfiniteTimeSpan);
            }
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

            if (_sinkBlock.InputCount >= _maxEgressMessageQueue) {
                _sinkBlockInputDroppedCount += (ulong)args.Notifications.Count();
            }
            else {
                _batchDataSetMessageBlock.Post(args);
            }
        }

        /// <summary>
        /// Gets the percentage of a number of values
        /// in relation the total ValueChanges.
        /// </summary>
        /// <param name="valueCount"></param>
        private string GetValuePercentage(double valueCount) =>
            $"{(_messageTrigger.ValueChangesCount > 0 ? valueCount / _messageTrigger.ValueChangesCount : 0):P0}";

        private readonly int _dataSetMessageBufferSize = 1;
        private readonly int _networkMessageBufferSize = 1;
        private readonly Timer _batchTriggerIntervalTimer;
        private readonly TimeSpan _batchTriggerInterval;

        private readonly int _maxEncodedMessageSize = 256 * 1024;
        private readonly int _encodedMessageSizeOverhead = 2 * 1024;

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

        /// <summary>
        /// Maximum size of egress message queue.
        /// </summary>
        private readonly int _maxEgressMessageQueue;

        /// <summary>
        /// Amount of messages that couldn't be sent to IoT Hub.
        /// </summary>
        private ulong _sinkBlockInputDroppedCount;

        private static readonly GaugeConfiguration kGaugeConfig = new GaugeConfiguration {
            LabelNames = new[] { "deviceid", "module", "triggerid" }
        };
        private static readonly Gauge kValueChangesCount = Metrics.CreateGauge(
            "iiot_edge_publisher_value_changes",
            "Opc ValuesChanges delivered for processing", kGaugeConfig);
        private static readonly Gauge kValueChangesPerSecond = Metrics.CreateGauge(
            "iiot_edge_publisher_value_changes_per_second",
            "Opc ValuesChanges/second delivered for processing", kGaugeConfig);
        private static readonly Gauge kDataChangesCount = Metrics.CreateGauge(
            "iiot_edge_publisher_data_changes",
            "Opc DataChanges delivered for processing", kGaugeConfig);
        private static readonly Gauge kDataChangesPerSecond = Metrics.CreateGauge(
            "iiot_edge_publisher_data_changes_per_second",
            "Opc DataChanges/second delivered for processing", kGaugeConfig);
        private static readonly Gauge kIoTHubQueueBuffer = Metrics.CreateGauge(
            "iiot_edge_publisher_iothub_queue_size",
            "IoT messages queued sending", kGaugeConfig);
        private static readonly Gauge kIoTHubQueueBufferDroppedCount = Metrics.CreateGauge(
            "iiot_edge_publisher_iothub_queue_dropped_count",
            "IoT messages dropped", kGaugeConfig);
        private static readonly Gauge kSentMessagesCount = Metrics.CreateGauge(
            "iiot_edge_publisher_sent_iot_messages",
            "IoT messages sent to hub", kGaugeConfig);
        private static readonly Gauge kSentMessagesPerSecond = Metrics.CreateGauge(
            "iiot_edge_publisher_sent_iot_messages_per_second",
            "IoT messages/second sent to hub", kGaugeConfig);
        private static readonly Gauge kNumberOfConnectionRetries = Metrics.CreateGauge(
            "iiot_edge_publisher_connection_retries",
            "OPC UA connect retries", kGaugeConfig);

        private static readonly Gauge kNotificationsProcessedCount = Metrics.CreateGauge(
            "iiot_edge_publisher_encoded_notifications",
            "publisher engine encoded opc notifications count", kGaugeConfig);
        private static readonly Gauge kNotificationsDroppedCount = Metrics.CreateGauge(
            "iiot_edge_publisher_dropped_notifications",
            "publisher engine dropped opc notifications count", kGaugeConfig);
        private static readonly Gauge kMessagesProcessedCount = Metrics.CreateGauge(
            "iiot_edge_publisher_processed_messages",
            "publisher engine processed iot messages count", kGaugeConfig);
        private static readonly Gauge kNotificationsPerMessageAvg = Metrics.CreateGauge(
            "iiot_edge_publisher_value_changes_per_message_average",
            "publisher engine value changes per iot message average", kGaugeConfig);
        private static readonly Gauge kMessageSizeAvg = Metrics.CreateGauge(
            "iiot_edge_publisher_encoded_message_size_average",
            "publisher engine iot message encoded body size average", kGaugeConfig);

        private static readonly Gauge kChunkSizeAvg = Metrics.CreateGauge(
            "iiot_edge_publisher_chunk_size_average",
            "IoT Hub chunk size average", kGaugeConfig);
        private static readonly Gauge kEstimatedMsgChunksPerday = Metrics.CreateGauge(
            "iiot_edge_publisher_estimated_message_chunks_per_day",
            "Estimated IoT Hub messages chunks charged per day", kGaugeConfig);
    }
}
