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

            _messageTrigger.OnMessage += MessageTriggerMessageReceived;

            if (_config.BatchSize.HasValue && _config.BatchSize.Value > 1) {
                _dataSetMessageBufferSize = _config.BatchSize.Value;
            }
            if (_config.MaxMessageSize.HasValue && _config.MaxMessageSize.Value > 0) {
                _maxEncodedMessageSize = _config.MaxMessageSize.Value;
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            _messageTrigger.OnMessage -= MessageTriggerMessageReceived;
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
                if (_config.DiagnosticsInterval.HasValue && _config.DiagnosticsInterval > TimeSpan.Zero){
                    _diagnosticsOutputTimer = new Timer(DiagnosticsOutputTimer_Elapsed, null,
                        _config.DiagnosticsInterval.Value,
                        _config.DiagnosticsInterval.Value);
                }

                if (_config.BatchTriggerInterval.HasValue && _config.BatchTriggerInterval > TimeSpan.Zero){
                    _batchTriggerIntervalTimer = new Timer(BatchTriggerIntervalTimer_Elapsed, null,
                        _config.BatchTriggerInterval.Value,
                        _config.BatchTriggerInterval.Value);
                }
                _encodingBlock = new TransformManyBlock<DataSetMessageModel[], NetworkMessageModel>(
                    async input => {
                        try {
                            if (_dataSetMessageBufferSize == 1) {
                                return await _messageEncoder.EncodeAsync(input, _maxEncodedMessageSize);
                            }
                            else {
                                return await _messageEncoder.EncodeBatchAsync(input, _maxEncodedMessageSize);
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
                    async input => await _messageSink.SendAsync(input),
                    new ExecutionDataflowBlockOptions {
                        CancellationToken = cancellationToken
                    });
                _batchDataSetMessageBlock.LinkTo(_encodingBlock);
                _encodingBlock.LinkTo(_batchNetworkMessageBlock);
                _batchNetworkMessageBlock.LinkTo(_sinkBlock);

                await _messageTrigger.RunAsync(cancellationToken);
            }
            finally {
                IsRunning = false;
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
            var totalDuration = (DateTime.UtcNow - _diagnosticStart).TotalSeconds;
            _logger.Information("Identity {deviceId}; {moduleId}", _identity.DeviceId, _identity.ModuleId);
            _logger.Information("\n   DIAGNOSTICS INFORMATION for Engine : {Name}\n" +
                "   # Ingress DataChanges (from OPC)   : {dataChangesCount} - {dataChangesAverage}/second\n" +
                "   # Ingress ValueChanges (from OPC)  : {valueChangesCount} - {valueChangesAverage}/second\n" +
                "   # Ingress BatchBlock buffer size   : {batchDataSetMessageBlockOutputCount}\n" +
                "   # Encoding Block input/output size : {encodingBlockInputCount}/{encodingBlockOutputCount}\n" +
                "   # Encoder Notifications processed  : {notificationsProcessedCount}\n" +
                "   # Encoder Notifications dropped    : {notificationsDroppedCount}\n" +
                "   # Encoder IoT Messages processed   : {messagesProcessedCount}\n" +
                "   # Encoder avg Notifications/Message: {notificationsPerMessage}\n" +
                "   # Encoder avg IoT Message body size: {messageSizeAverage}\n" +
                "   # Outgress Batch Block buffer size : {batchNetworkMessageBlockOutputCount}\n" +
                "   # Outgress input buffer count      : {sinkBlockInputCount}\n" +
                "   # Outgress IoT message count       : {messageSinkSentMessagesCount} - {sentMessagesAverage}/second\n" +
                "   # Connection retries               : {connectionRetries}\n",
                Name,
                _messageTrigger.DataChangesCount, _messageTrigger.DataChangesCount / totalDuration,
                _messageTrigger.ValueChangesCount, _messageTrigger.ValueChangesCount / totalDuration,
                _batchDataSetMessageBlock.OutputCount,
                _encodingBlock.InputCount, _encodingBlock.OutputCount,
                _messageEncoder.NotificationsProcessedCount,
                _messageEncoder.NotificationsDroppedCount,
                _messageEncoder.MessagesProcessedCount,
                _messageEncoder.AvgNotificationsPerMessage,
                _messageEncoder.AvgMessageSize,
                _batchNetworkMessageBlock.OutputCount,
                _sinkBlock.InputCount,
                _messageSink.SentMessagesCount,
                _messageSink.SentMessagesCount / totalDuration,
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
            _batchDataSetMessageBlock.TriggerBatch();
        }

        /// <summary>
        /// Message received handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void MessageTriggerMessageReceived(object sender, DataSetMessageModel args) {
            if (_diagnosticStart == DateTime.MinValue) {
                _diagnosticStart = DateTime.UtcNow;
            }
            _batchDataSetMessageBlock.Post(args);
        }

        private readonly int _dataSetMessageBufferSize = 1;
        private readonly int _networkMessageBufferSize = 1;
        private Timer _batchTriggerIntervalTimer;
        private readonly int _maxEncodedMessageSize = 256 * 1024;

        private readonly IEngineConfiguration _config;
        private readonly IMessageSink _messageSink;
        private readonly IMessageEncoder _messageEncoder;
        private readonly IMessageTrigger _messageTrigger;
        private readonly ILogger _logger;
        private readonly IIdentity _identity;

        private BatchBlock<DataSetMessageModel> _batchDataSetMessageBlock;
        private BatchBlock<NetworkMessageModel> _batchNetworkMessageBlock;

        private Timer _diagnosticsOutputTimer;
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
