// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Prometheus;
    using Serilog;
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

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
                _notificationBufferSize = _config.BatchSize.Value;
            }
            if (_config.MaxMessageSize.HasValue && _config.MaxMessageSize.Value > 0) {
                _maxEncodedMessageSize = _config.MaxMessageSize.Value;
            }

            if (_maxEncodedMessageSize <= 0) {
                _maxEncodedMessageSize = int.MaxValue;
            }
            if (_maxEncodedMessageSize > _messageSink.MaxMessageSize) {
                _maxEncodedMessageSize = _messageSink.MaxMessageSize;
            }

            _diagnosticInterval = _config.DiagnosticsInterval.GetValueOrDefault(TimeSpan.Zero);
            _batchTriggerInterval = _config.BatchTriggerInterval.GetValueOrDefault(TimeSpan.Zero);
            _diagnosticsOutputTimer = new Timer(DiagnosticsOutputTimer_Elapsed);
            _batchTriggerIntervalTimer = new Timer(BatchTriggerIntervalTimer_Elapsed);
            _maxOutgressMessages = _config.MaxOutgressMessages.GetValueOrDefault(4096); // = 1 GB

            _encodingBlock = new TransformManyBlock<SubscriptionNotificationModel[], NetworkMessageModel>(
                input => {
                    try {
                        return _messageEncoder.Encode(input, _maxEncodedMessageSize, _notificationBufferSize != 1);
                    }
                    catch (Exception e) {
                        _logger.Error(e, "Encoding failure.");
                        return Enumerable.Empty<NetworkMessageModel>();
                    }
                },
                new ExecutionDataflowBlockOptions());

            _batchDataSetMessageBlock = new BatchBlock<SubscriptionNotificationModel>(
                _notificationBufferSize,
                new GroupingDataflowBlockOptions ());

            _batchNetworkMessageBlock = new BatchBlock<NetworkMessageModel>(
                _networkMessageBufferSize,
                new GroupingDataflowBlockOptions ());

            _sinkBlock = new ActionBlock<NetworkMessageModel[]>(
                async input => {
                    if (input != null && input.Any()) {
                        _logger.Debug("Sink block in engine {Name} triggered with {count} messages",
                            Name, input.Length);
                        await _messageSink.SendAsync(input).ConfigureAwait(false);
                    }
                    else {
                        _logger.Warning("Sink block in engine {Name} triggered with empty input",
                            Name);
                    }
                },
                new ExecutionDataflowBlockOptions ());
            _batchDataSetMessageBlock.LinkTo(_encodingBlock);
            _encodingBlock.LinkTo(_batchNetworkMessageBlock);
            _batchNetworkMessageBlock.LinkTo(_sinkBlock);

            _messageTrigger.OnMessage += MessageTriggerMessageReceived;
            _messageTrigger.OnCounterReset += MessageTriggerCounterResetReceived;

            if (_diagnosticInterval > TimeSpan.Zero) {
                _diagnosticsOutputTimer.Change(_diagnosticInterval, _diagnosticInterval);
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            _logger.Information("Disposing processing engine {Name}", Name);
            _diagnosticsOutputTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _batchTriggerIntervalTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _messageTrigger.OnCounterReset -= MessageTriggerCounterResetReceived;
            _messageTrigger.OnMessage -= MessageTriggerMessageReceived;
            _batchDataSetMessageBlock.Complete();
            _batchDataSetMessageBlock.Completion.GetAwaiter().GetResult();
            _encodingBlock.Complete();
            _encodingBlock.Completion.GetAwaiter().GetResult();
            _sinkBlock.Complete();
            _sinkBlock.Completion.GetAwaiter().GetResult();
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

            if (IsRunning) {
                return;
            }

            try {
                IsRunning = true;
                await _messageTrigger.RunAsync(cancellationToken).ConfigureAwait(false);
            }
            finally {
                IsRunning = false;
            }
        }

        /// <inheritdoc/>
        public Task SwitchProcessMode(ProcessMode processMode, DateTime? timestamp) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public JobDiagnosticInfoModel GetDiagnosticInfo() {
            if (_messageTrigger.EndpointUrl == null) {
                return null;
            }
            var totalSeconds = (DateTime.UtcNow - _diagnosticStart).TotalSeconds;
            double totalDuration = _diagnosticStart != DateTime.MinValue ? totalSeconds : 0;
            double chunkSizeAverage = _messageEncoder.AvgMessageSize / (4 * 1024);
            double sentMessagesPerSec = totalDuration > 0 ? _messageSink.SentMessagesCount / totalDuration : 0;
            double estimatedMsgChunksPerDay = Math.Ceiling(chunkSizeAverage) * sentMessagesPerSec * 60 * 60 * 24;
            var diagnosticInfo = new JobDiagnosticInfoModel();
            var endpointDiagnosticInfo = new EndpointDiagnosticModel();

            endpointDiagnosticInfo.EndpointUrl = _messageTrigger.EndpointUrl;
            endpointDiagnosticInfo.DataSetWriterGroup = _messageTrigger.DataSetWriterGroup;
            endpointDiagnosticInfo.UseSecurity = _messageTrigger.UseSecurity;
            endpointDiagnosticInfo.OpcAuthenticationMode = (AuthMode)_messageTrigger.AuthenticationMode;
            endpointDiagnosticInfo.OpcAuthenticationUsername = _messageTrigger.AuthenticationUsername;
            diagnosticInfo.Endpoint = endpointDiagnosticInfo;
            diagnosticInfo.Id = Name;
            diagnosticInfo.SentMessagesPerSec = sentMessagesPerSec;
            diagnosticInfo.IngestionDuration = TimeSpan.FromSeconds(totalDuration);
            diagnosticInfo.IngressDataChanges = _messageTrigger.DataChangesCount;
            diagnosticInfo.IngressValueChanges = _messageTrigger.ValueChangesCount;
            diagnosticInfo.IngressEvents = _messageTrigger.EventCount;
            diagnosticInfo.IngressEventNotifications = _messageTrigger.EventNotificationCount;
            diagnosticInfo.IngressBatchBlockBufferSize = _batchDataSetMessageBlock.OutputCount;
            diagnosticInfo.EncodingBlockInputSize = _encodingBlock.InputCount;
            diagnosticInfo.EncodingBlockOutputSize = _encodingBlock.OutputCount;
            diagnosticInfo.EncoderNotificationsProcessed = _messageEncoder.NotificationsProcessedCount;
            diagnosticInfo.EncoderMaxMessageSplitRatio = _messageEncoder.MaxMessageSplitRatio;
            diagnosticInfo.EncoderNotificationsDropped = _messageEncoder.NotificationsDroppedCount;
            diagnosticInfo.EncoderIoTMessagesProcessed = _messageEncoder.MessagesProcessedCount;
            diagnosticInfo.EncoderAvgNotificationsMessage = _messageEncoder.AvgNotificationsPerMessage;
            diagnosticInfo.EncoderAvgIoTMessageBodySize = _messageEncoder.AvgMessageSize;
            diagnosticInfo.EncoderAvgIoTChunkUsage = chunkSizeAverage;
            diagnosticInfo.EstimatedIoTChunksPerDay = estimatedMsgChunksPerDay;
            diagnosticInfo.OutgressBatchBlockBufferSize = _batchNetworkMessageBlock.OutputCount;
            diagnosticInfo.OutgressInputBufferCount = _sinkBlock.InputCount;
            diagnosticInfo.OutgressInputBufferDropped = _sinkBlockInputDroppedCount;
            diagnosticInfo.OutgressIoTMessageCount = _messageSink.SentMessagesCount;
            diagnosticInfo.ConnectionRetries = _messageTrigger.NumberOfConnectionRetries;
            diagnosticInfo.OpcEndpointConnected = _messageTrigger.IsConnectionOk;
            diagnosticInfo.MonitoredOpcNodesSucceededCount = _messageTrigger.NumberOfGoodNodes;
            diagnosticInfo.MonitoredOpcNodesFailedCount = _messageTrigger.NumberOfBadNodes;

            return diagnosticInfo;
        }

        /// <inheritdoc/>
        public void ReconfigureTrigger(object config) {
            _messageTrigger.Reconfigure(config);
        }

        /// <summary>
        /// Diagnostics timer
        /// </summary>
        private void DiagnosticsOutputTimer_Elapsed(object state) {
            var info = GetDiagnosticInfo();
            var totalSeconds = (DateTime.UtcNow - _diagnosticStart).TotalSeconds;
            double valueChangesPerSec = info.IngressValueChanges / info.IngestionDuration.TotalSeconds;
            double dataChangesPerSec = info.IngressDataChanges / info.IngestionDuration.TotalSeconds;
            double valueChangesPerSecLastMin = _messageTrigger.ValueChangesCountLastMinute / Math.Min(totalSeconds, 60d);
            double dataChangesPerSecLastMin = _messageTrigger.DataChangesCountLastMinute / Math.Min(totalSeconds, 60d);
            double messageSizeAveragePercent = Math.Round(info.EncoderAvgIoTMessageBodySize / _maxEncodedMessageSize * 100);
            string messageSizeAveragePercentFormatted = $"({messageSizeAveragePercent}%)";

            _logger.Debug("Identity {deviceId}; {moduleId}", _identity.DeviceId, _identity.ModuleId);

            var diagInfo = new StringBuilder();
            diagInfo.AppendLine("\n  DIAGNOSTICS INFORMATION for          : {host}");
            diagInfo.AppendLine("  # Ingestion duration                 : {duration,14:dd\\:hh\\:mm\\:ss} (dd:hh:mm:ss)");
            string dataChangesPerSecFormatted = info.IngressDataChanges > 0 && info.IngestionDuration.TotalSeconds > 0
                ? $"(All time ~{dataChangesPerSec:0.##}/s; {_messageTrigger.DataChangesCountLastMinute.ToString("D2")} in last 60s ~{dataChangesPerSecLastMin:0.##}/s)"
                : "";
            diagInfo.AppendLine("  # Ingress DataChanges (from OPC)     : {dataChangesCount,14:n0} {dataChangesPerSecFormatted}");
            diagInfo.AppendLine("  # Ingress EventData (from OPC)       : {eventNotificationCount,14:n0}");
            string valueChangesPerSecFormatted = info.IngressValueChanges > 0 && info.IngestionDuration.TotalSeconds > 0
                ? $"(All time ~{valueChangesPerSec:0.##}/s; {_messageTrigger.ValueChangesCountLastMinute.ToString("D2")} in last 60s ~{valueChangesPerSecLastMin:0.##}/s)"
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
            diagInfo.AppendLine("  # Encoder avg IoT Message body size  : {messageSizeAverage,14:n0} {messageSizeAveragePercentFormatted}");
            diagInfo.AppendLine("  # Encoder avg IoT Chunk (4 KB) usage : {chunkSizeAverage,14:0.#}");
            diagInfo.AppendLine("  # Estimated IoT Chunks (4 KB) per day: {estimatedMsgChunksPerDay,14:n0}");
            diagInfo.AppendLine("  # Outgress Batch Block buffer size   : {batchNetworkMessageBlockOutputCount,14:0}");
            diagInfo.AppendLine("  # Outgress input buffer count        : {sinkBlockInputCount,14:n0}");
            diagInfo.AppendLine("  # Outgress input buffer dropped      : {sinkBlockInputDroppedCount,14:n0}");

            string sentMessagesPerSecFormatted = info.OutgressIoTMessageCount > 0 && info.IngestionDuration.TotalSeconds > 0 ? $"({info.SentMessagesPerSec:0.##}/s)" : "";
            diagInfo.AppendLine("  # Outgress IoT message count         : {messageSinkSentMessagesCount,14:n0} {sentMessagesPerSecFormatted}");
            diagInfo.AppendLine("  # Connection retries                 : {connectionRetries,14:0}");
            diagInfo.AppendLine("  # Opc endpoint connected?            : {isConnectionOk,14:0}");
            diagInfo.AppendLine("  # Monitored Opc nodes succeeded count: {goodNodes,14:0}");
            diagInfo.AppendLine("  # Monitored Opc nodes failed count   : {badNodes,14:0}");

            _logger.Information(diagInfo.ToString(),
                info.Id,
                info.IngestionDuration,
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
                info.EncoderAvgIoTMessageBodySize, messageSizeAveragePercentFormatted,
                info.EncoderAvgIoTChunkUsage,
                info.EstimatedIoTChunksPerDay,
                info.OutgressBatchBlockBufferSize,
                info.OutgressInputBufferCount,
                info.OutgressInputBufferDropped,
                info.OutgressIoTMessageCount, sentMessagesPerSecFormatted,
                info.ConnectionRetries,
                info.OpcEndpointConnected,
                info.MonitoredOpcNodesSucceededCount,
                info.MonitoredOpcNodesFailedCount);

            string deviceId = _identity.DeviceId ?? "";
            string moduleId = _identity.ModuleId ?? "";
            kDataChangesCount.WithLabels(deviceId, moduleId, Name)
                .Set(info.IngressDataChanges);
            kDataChangesPerSecond.WithLabels(deviceId, moduleId, Name)
                .Set(dataChangesPerSec);
            kDataChangesPerSecondLastMin.WithLabels(deviceId, moduleId, Name)
                .Set(dataChangesPerSecLastMin);
            kEventNotificationsCount.WithLabels(deviceId, moduleId, Name)
                .Set(info.IngressEventNotifications);
            kValueChangesCount.WithLabels(deviceId, moduleId, Name)
                .Set(info.IngressValueChanges);
            kValueChangesPerSecond.WithLabels(deviceId, moduleId, Name)
                .Set(valueChangesPerSec);
            kValueChangesPerSecondLastMin.WithLabels(deviceId, moduleId, Name)
                .Set(valueChangesPerSecLastMin);
            kEventsCount.WithLabels(deviceId, moduleId, Name)
                .Set(info.IngressEvents);
            kNotificationsProcessedCount.WithLabels(deviceId, moduleId, Name)
                .Set(info.EncoderNotificationsProcessed);
            kNotificationsDroppedCount.WithLabels(deviceId, moduleId, Name)
                .Set(info.EncoderNotificationsDropped);
            kMaxMessageSplitRatio.WithLabels(deviceId, moduleId, Name)
               .Set(info.EncoderMaxMessageSplitRatio);
            kMessagesProcessedCount.WithLabels(deviceId, moduleId, Name)
                .Set(info.EncoderIoTMessagesProcessed);
            kNotificationsPerMessageAvg.WithLabels(deviceId, moduleId, Name)
                .Set(info.EncoderAvgNotificationsMessage);
            kMessageSizeAvg.WithLabels(deviceId, moduleId, Name)
                .Set(info.EncoderAvgIoTMessageBodySize);
            kIoTHubQueueBuffer.WithLabels(deviceId, moduleId, Name)
                .Set(info.OutgressInputBufferCount);
            kIoTHubQueueBufferDroppedCount.WithLabels(deviceId, moduleId, Name)
                .Set(info.OutgressInputBufferDropped);
            kSentMessagesCount.WithLabels(deviceId, moduleId, Name)
                .Set(info.OutgressIoTMessageCount);
            kSentMessagesPerSecond.WithLabels(deviceId, moduleId, Name)
                .Set(info.SentMessagesPerSec);
            kNumberOfConnectionRetries.WithLabels(deviceId, moduleId, Name)
                .Set(info.ConnectionRetries);
            kIsConnectionOk.WithLabels(deviceId, moduleId, Name)
                .Set(info.OpcEndpointConnected ? 1 : 0);
            kNumberOfGoodNodes.WithLabels(deviceId, moduleId, Name)
                .Set(info.MonitoredOpcNodesSucceededCount);
            kNumberOfBadNodes.WithLabels(deviceId, moduleId, Name)
                .Set(info.MonitoredOpcNodesFailedCount);
            kChunkSizeAvg.WithLabels(deviceId, moduleId, Name)
                .Set(info.EncoderAvgIoTChunkUsage);
            kEstimatedMsgChunksPerday.WithLabels(deviceId, moduleId, Name)
                .Set(info.EstimatedIoTChunksPerDay);
        }

        /// <summary>
        /// Batch trigger interval
        /// </summary>
        private void BatchTriggerIntervalTimer_Elapsed(object state) {
            if (_batchTriggerInterval > TimeSpan.Zero) {
                _batchTriggerIntervalTimer.Change(_batchTriggerInterval, Timeout.InfiniteTimeSpan);
            }
            _batchDataSetMessageBlock?.TriggerBatch();
        }

        /// <summary>
        /// Message received handler
        /// </summary>
        private void MessageTriggerMessageReceived(object sender, SubscriptionNotificationModel args) {
            _logger.Debug("Message trigger for {Name} received message with sequenceNumber {SequenceNumber}",
                    Name, args.SequenceNumber);

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

        private readonly int _notificationBufferSize = 1;
        private readonly int _networkMessageBufferSize = 1;
        private readonly Timer _batchTriggerIntervalTimer;
        private readonly TimeSpan _batchTriggerInterval;
        private readonly int _maxEncodedMessageSize;
        private readonly IEngineConfiguration _config;
        private readonly IMessageSink _messageSink;
        private readonly IMessageEncoder _messageEncoder;
        private readonly IMessageTrigger _messageTrigger;
        private readonly ILogger _logger;
        private readonly IIdentity _identity;

        private readonly BatchBlock<SubscriptionNotificationModel> _batchDataSetMessageBlock;
        private readonly BatchBlock<NetworkMessageModel> _batchNetworkMessageBlock;

        private readonly Timer _diagnosticsOutputTimer;
        private readonly TimeSpan _diagnosticInterval;
        private DateTime _diagnosticStart = DateTime.MinValue;

        private readonly TransformManyBlock<SubscriptionNotificationModel[], NetworkMessageModel> _encodingBlock;
        private readonly ActionBlock<NetworkMessageModel[]> _sinkBlock;

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
        private static readonly Gauge kEventsCount = Metrics.CreateGauge(
            "iiot_edge_publisher_events",
            "Opc Events delivered for processing", kGaugeConfig);
        private static readonly Gauge kValueChangesCount = Metrics.CreateGauge(
            "iiot_edge_publisher_value_changes",
            "Opc Value changes delivered for processing", kGaugeConfig);
        private static readonly Gauge kValueChangesPerSecond = Metrics.CreateGauge(
            "iiot_edge_publisher_value_changes_per_second",
            "Opc Value changes/second delivered for processing", kGaugeConfig);
        private static readonly Gauge kValueChangesPerSecondLastMin = Metrics.CreateGauge(
            "iiot_edge_publisher_value_changes_per_second_last_min",
            "Opc Value changes/second delivered for processing in last 60s", kGaugeConfig);
        private static readonly Gauge kEventNotificationsCount = Metrics.CreateGauge(
            "iiot_edge_publisher_event_notifications",
            "Opc Event notifications delivered for processing", kGaugeConfig);
        private static readonly Gauge kDataChangesCount = Metrics.CreateGauge(
            "iiot_edge_publisher_data_changes",
            "Opc Data notifications delivered for processing", kGaugeConfig);
        private static readonly Gauge kDataChangesPerSecond = Metrics.CreateGauge(
            "iiot_edge_publisher_data_changes_per_second",
            "Opc Data notifications/second delivered for processing", kGaugeConfig);
        private static readonly Gauge kDataChangesPerSecondLastMin = Metrics.CreateGauge(
            "iiot_edge_publisher_data_changes_per_second_last_min",
            "Opc Data notifications/second delivered for processing in last 60s", kGaugeConfig);
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
        private static readonly Gauge kMaxMessageSplitRatio = Metrics.CreateGauge(
            "iiot_edge_publisher_message_split_ratio_max",
            "publisher engine worst message split ratio", kGaugeConfig);
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
