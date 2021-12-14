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
            _maxOutgressMessages = _config.MaxOutgressMessages.GetValueOrDefault(4096); // = 1 GB
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
                _messageTrigger.OnCounterReset += MessageTriggerCounterResetReceived;

                if (_diagnosticInterval > TimeSpan.Zero) {
                    _diagnosticsOutputTimer.Change(_diagnosticInterval, _diagnosticInterval);
                }

                await _messageTrigger.RunAsync(cancellationToken).ConfigureAwait(false);

            }
            finally {
                IsRunning = false;
                _messageTrigger.OnCounterReset -= MessageTriggerCounterResetReceived;
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
            var totalSeconds = (DateTime.UtcNow - _diagnosticStart).TotalSeconds;
            double totalDuration = _diagnosticStart != DateTime.MinValue ? totalSeconds : 0;
            double valueChangesPerSec = _messageTrigger.ValueChangesCount / totalDuration;
            double dataChangesPerSec = _messageTrigger.DataChangesCount / totalDuration;
            double valueChangesPerSecLastMin = _messageTrigger.ValueChangesCountLastMinute / Math.Min(totalSeconds, 60d);
            double dataChangesPerSecLastMin = _messageTrigger.DataChangesCountLastMinute / Math.Min(totalSeconds, 60d);
            double sentMessagesPerSec = totalDuration > 0 ? _messageSink.SentMessagesCount / totalDuration : 0;
            double messageSizeAveragePercent = Math.Round(_messageEncoder.AvgMessageSize / _maxEncodedMessageSize * 100);
            string messageSizeAveragePercentFormatted = $"({messageSizeAveragePercent}%)";
            double chunkSizeAverage = _messageEncoder.AvgMessageSize / (4 * 1024);
            double estimatedMsgChunksPerDay = Math.Ceiling(chunkSizeAverage) * sentMessagesPerSec * 60 * 60 * 24;

            _logger.Debug("Identity {deviceId}; {moduleId}", _identity.DeviceId, _identity.ModuleId);

            var diagInfo = new StringBuilder();
            diagInfo.AppendLine("\n  DIAGNOSTICS INFORMATION for          : {host}");
            diagInfo.AppendLine("  # Ingestion duration                 : {duration,14:dd\\:hh\\:mm\\:ss} (dd:hh:mm:ss)");
            string dataChangesPerSecFormatted = _messageTrigger.DataChangesCount > 0 && totalDuration > 0
                ? $"(All time ~{dataChangesPerSec:0.##}/s; {_messageTrigger.DataChangesCountLastMinute.ToString("D2")} in last 60s ~{dataChangesPerSecLastMin:0.##}/s)"
                : "";
            diagInfo.AppendLine("  # Ingress DataChanges (from OPC)     : {dataChangesCount,14:n0} {dataChangesPerSecFormatted}");
            string valueChangesPerSecFormatted = _messageTrigger.ValueChangesCount > 0 && totalDuration > 0
                ? $"(All time ~{valueChangesPerSec:0.##}/s; {_messageTrigger.ValueChangesCountLastMinute.ToString("D2")} in last 60s ~{valueChangesPerSecLastMin:0.##}/s)"
                : "";
            diagInfo.AppendLine("  # Ingress ValueChanges (from OPC)    : {valueChangesCount,14:n0} {valueChangesPerSecFormatted}");

            diagInfo.AppendLine("  # Ingress BatchBlock buffer size     : {batchDataSetMessageBlockOutputCount,14:0}");
            diagInfo.AppendLine("  # Encoding Block input/output size   : {encodingBlockInputCount,14:0} | {encodingBlockOutputCount:0}");
            diagInfo.AppendLine("  # Encoder Notifications processed    : {notificationsProcessedCount,14:n0}");
            diagInfo.AppendLine("  # Encoder Notifications dropped      : {notificationsDroppedCount,14:n0}");
            diagInfo.AppendLine("  # Encoder IoT Messages processed     : {messagesProcessedCount,14:n0}");
            diagInfo.AppendLine("  # Encoder avg Notifications/Message  : {notificationsPerMessage,14:0}");
            diagInfo.AppendLine("  # Encoder avg IoT Message body size  : {messageSizeAverage,14:n0} {messageSizeAveragePercentFormatted}");
            diagInfo.AppendLine("  # Encoder avg IoT Chunk (4 KB) usage : {chunkSizeAverage,14:0.#}");
            diagInfo.AppendLine("  # Estimated IoT Chunks (4 KB) per day: {estimatedMsgChunksPerDay,14:n0}");
            diagInfo.AppendLine("  # Outgress Batch Block buffer size   : {batchNetworkMessageBlockOutputCount,14:0}");
            diagInfo.AppendLine("  # Outgress input buffer count        : {sinkBlockInputCount,14:n0}");
            diagInfo.AppendLine("  # Outgress input buffer dropped      : {sinkBlockInputDroppedCount,14:n0}");

            string sentMessagesPerSecFormatted = _messageSink.SentMessagesCount > 0 && totalDuration > 0 ? $"({sentMessagesPerSec:0.##}/s)" : "";
            diagInfo.AppendLine("  # Outgress IoT message count         : {messageSinkSentMessagesCount,14:n0} {sentMessagesPerSecFormatted}");
            diagInfo.AppendLine("  # Connection retries                 : {connectionRetries,14:0}");
            diagInfo.AppendLine("  # Opc endpoint connected?            : {isConnectionOk,14:0}");
            diagInfo.AppendLine("  # Monitored Opc nodes succeeded count: {goodNodes,14:0}");
            diagInfo.AppendLine("  # Monitored Opc nodes failed count   : {badNodes,14:0}");

            _logger.Information(diagInfo.ToString(),
                Name,
                TimeSpan.FromSeconds(totalDuration),
                _messageTrigger.DataChangesCount, dataChangesPerSecFormatted,
                _messageTrigger.ValueChangesCount, valueChangesPerSecFormatted,
                _batchDataSetMessageBlock.OutputCount,
                _encodingBlock.InputCount, _encodingBlock.OutputCount,
                _messageEncoder.NotificationsProcessedCount,
                _messageEncoder.NotificationsDroppedCount,
                _messageEncoder.MessagesProcessedCount,
                _messageEncoder.AvgNotificationsPerMessage,
                _messageEncoder.AvgMessageSize, messageSizeAveragePercentFormatted,
                chunkSizeAverage,
                estimatedMsgChunksPerDay,
                _batchNetworkMessageBlock.OutputCount,
                _sinkBlock.InputCount,
                _sinkBlockInputDroppedCount,
                _messageSink.SentMessagesCount, sentMessagesPerSecFormatted,
                _messageTrigger.NumberOfConnectionRetries,
                _messageTrigger.IsConnectionOk,
                _messageTrigger.NumberOfGoodNodes,
                _messageTrigger.NumberOfBadNodes);

            string deviceId = _identity.DeviceId ?? "";
            string moduleId = _identity.ModuleId ?? "";
            kDataChangesCount.WithLabels(deviceId, moduleId, Name)
                .Set(_messageTrigger.DataChangesCount);
            kDataChangesPerSecond.WithLabels(deviceId, moduleId, Name)
                .Set(dataChangesPerSec);
            kDataChangesPerSecondLastMin.WithLabels(deviceId, moduleId, Name)
                .Set(dataChangesPerSecLastMin);
            kValueChangesCount.WithLabels(deviceId, moduleId, Name)
                .Set(_messageTrigger.ValueChangesCount);
            kValueChangesPerSecond.WithLabels(deviceId, moduleId, Name)
                .Set(valueChangesPerSec);
            kValueChangesPerSecondLastMin.WithLabels(deviceId, moduleId, Name)
                .Set(valueChangesPerSecLastMin);
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
            kIsConnectionOk.WithLabels(deviceId, moduleId, Name)
                .Set(_messageTrigger.IsConnectionOk ? 1 : 0);
            kNumberOfGoodNodes.WithLabels(deviceId, moduleId, Name)
                .Set(_messageTrigger.NumberOfGoodNodes);
            kNumberOfBadNodes.WithLabels(deviceId, moduleId, Name)
                .Set(_messageTrigger.NumberOfBadNodes);
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
        private void MessageTriggerMessageReceived(object sender, DataSetMessageModel args) {
            if (_diagnosticStart == DateTime.MinValue) {
                if (_batchTriggerInterval > TimeSpan.Zero) {
                    _batchTriggerIntervalTimer.Change(_batchTriggerInterval, Timeout.InfiniteTimeSpan);
                }

                // reset diagnostic counter, to be aligned with publishing
                if (_diagnosticInterval > TimeSpan.Zero) {
                    _diagnosticsOutputTimer.Change(_diagnosticInterval, _diagnosticInterval);
                }
                _diagnosticStart = DateTime.UtcNow;
            }

            if(_sinkBlock.InputCount >= _maxOutgressMessages) {
                _sinkBlockInputDroppedCount++;
            }
            else {
                _batchDataSetMessageBlock.Post(args);
            }
        }

        private void MessageTriggerCounterResetReceived(object sender, EventArgs e) {
            _diagnosticStart = DateTime.MinValue;
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

        /// <summary>
        /// Define the maximum size of messages
        /// </summary>
        private readonly int _maxOutgressMessages;

        /// <summary>
        /// Counts the amount of messages that couldn't be send to IoTHub
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
        private static readonly Gauge kValueChangesPerSecondLastMin = Metrics.CreateGauge(
            "iiot_edge_publisher_value_changes_per_second_last_min",
            "Opc ValuesChanges/second delivered for processing in last 60s", kGaugeConfig);
        private static readonly Gauge kDataChangesCount = Metrics.CreateGauge(
            "iiot_edge_publisher_data_changes",
            "Opc DataChanges delivered for processing", kGaugeConfig);
        private static readonly Gauge kDataChangesPerSecond = Metrics.CreateGauge(
            "iiot_edge_publisher_data_changes_per_second",
            "Opc DataChanges/second delivered for processing", kGaugeConfig);
        private static readonly Gauge kDataChangesPerSecondLastMin = Metrics.CreateGauge(
            "iiot_edge_publisher_data_changes_per_second_last_min",
            "Opc DataChanges/second delivered for processing in last 60s", kGaugeConfig);
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
        private static readonly Gauge kIsConnectionOk = Metrics.CreateGauge(
            "iiot_edge_publisher_is_connection_ok",
            "OPC UA connection success flag", kGaugeConfig);
        private static readonly Gauge kNumberOfGoodNodes = Metrics.CreateGauge(
            "iiot_edge_publisher_good_nodes",
            "OPC UA connected nodes", kGaugeConfig);
        private static readonly Gauge kNumberOfBadNodes = Metrics.CreateGauge(
            "iiot_edge_publisher_bad_nodes",
            "OPC UA disconnected nodes", kGaugeConfig);

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
            "iiot_edge_publisher_notifications_per_message_average",
            "publisher engine opc notifications per iot message average", kGaugeConfig);
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
