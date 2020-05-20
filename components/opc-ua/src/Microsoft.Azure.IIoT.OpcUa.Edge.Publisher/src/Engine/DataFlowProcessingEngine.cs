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
    using System.Text;
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
                _diagnosticStart = DateTime.UtcNow;
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
                    async input =>
                        (_dataSetMessageBufferSize == 1)
                            ? await _messageEncoder.EncodeAsync(input)
                            : await _messageEncoder.EncodeBatchAsync(input, _maxEncodedMessageSize),
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
            var totalDuration = DateTime.UtcNow - _diagnosticStart;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"   DIAGNOSTICS INFORMATION for Engine: {Name}");
            sb.AppendLine("   =======================");
            sb.AppendLine($"   # Ingress data changes (from OPC)  : {_messageTrigger?.DataChangesCount}" +
                $" #/s : {_messageTrigger?.DataChangesCount / totalDuration.TotalSeconds}");
            sb.AppendLine($"   # Ingress value changes (from OPC) : {_messageTrigger?.ValueChangesCount}" +
                $" #/s : {_messageTrigger?.ValueChangesCount / totalDuration.TotalSeconds}");
            sb.AppendLine($"   # Ingress BatchBlock buffer count  : {_batchDataSetMessageBlock?.OutputCount}");
            sb.AppendLine($"   # EncodingBlock input/output count : {_encodingBlock?.InputCount}/{_encodingBlock?.OutputCount}");
            sb.AppendLine($"   # Outgress Batch Block buffer count: {_batchNetworkMessageBlock?.OutputCount}");
            sb.AppendLine($"   # Outgress Synk input buffer count : {_sinkBlock?.InputCount}");
            sb.AppendLine($"   # Outgress message count (IoT Hub) : {_messageSink.SentMessagesCount}" +
                $" #/s : {_messageSink.SentMessagesCount / totalDuration.TotalSeconds}");
            sb.AppendLine("   =======================");
            sb.AppendLine($"   # Number of connection retries since last error: {_messageTrigger.NumberOfConnectionRetries}");
            sb.AppendLine("   =======================");
            _logger.Information(sb.ToString());
            kValueChangesCount.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_messageTrigger.ValueChangesCount);
            kDataChangesCount.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_messageTrigger.DataChangesCount);
            kNumberOfConnectionRetries.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_messageTrigger.NumberOfConnectionRetries);
            kSentMessagesCount.WithLabels(_identity.DeviceId ?? "",
                _identity.ModuleId ?? "", Name).Set(_messageSink.SentMessagesCount);
            // TODO: Use structured logging!
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
        private DateTime _diagnosticStart = DateTime.UtcNow;

        private TransformManyBlock<DataSetMessageModel[], NetworkMessageModel> _encodingBlock;
        private ActionBlock<NetworkMessageModel[]> _sinkBlock;

        private static readonly GaugeConfiguration kGaugeConfig = new GaugeConfiguration {
            LabelNames = new[] { "deviceid", "module", "triggerid" }
        };
        private static readonly Gauge kValueChangesCount = Metrics.CreateGauge(
            "iiot_edge_publisher_value_changes", "invoke value changes in trigger", kGaugeConfig);
        private static readonly Gauge kDataChangesCount = Metrics.CreateGauge(
            "iiot_edge_publisher_data_changes", "invoke data changes in trigger", kGaugeConfig);
        private static readonly Gauge kSentMessagesCount = Metrics.CreateGauge(
            "iiot_edge_publisher_sent_messages", "messages sent to IoTHub", kGaugeConfig);
        private static readonly Gauge kNumberOfConnectionRetries = Metrics.CreateGauge(
            "iiot_edge_publisher_connection_retries", "OPC UA connect retries", kGaugeConfig);
    }
}
