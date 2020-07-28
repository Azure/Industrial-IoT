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
    using System.Text;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reactive.Linq;
    using System.Reactive.Concurrency;
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

            if (_config.BatchSize.HasValue && _config.BatchSize.Value > 1) {
                _dataSetMessageBufferSize = _config.BatchSize.Value;
            }
            if (_config.MaxMessageSize.HasValue && _config.MaxMessageSize.Value > 0) {
                _maxEncodedMessageSize = _config.MaxMessageSize.Value;
            }

            _diagnosticInterval = _config.DiagnosticsInterval.GetValueOrDefault(TimeSpan.Zero);
            _batchTriggerInterval = _config.BatchTriggerInterval.GetValueOrDefault(TimeSpan.Zero);
            _diagnosticsOutputTimer = new Timer(DiagnosticsOutputTimer_Elapsed);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _logger.Debug("Disposing {name}", Name);
            _diagnosticsOutputTimer?.Dispose();
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

                if (_diagnosticInterval > TimeSpan.Zero) {
                    _diagnosticStart = DateTime.UtcNow;
                    _diagnosticsOutputTimer.Change(_diagnosticInterval, _diagnosticInterval);
                }

                IsRunning = true;
                using (var subscription = Observable
                    .FromEventPattern<DataSetMessageModel>(
                        e => _messageTrigger.OnMessage += e,
                        e => _messageTrigger.OnMessage -= e)
                    .Synchronize()
                    .Select(e => e.EventArgs)
                    .Buffer(_batchTriggerInterval, _dataSetMessageBufferSize)
                    .Select(input => {
                        // Encode
                        IEnumerable<NetworkMessageModel> result;
                        try {
                            if (_dataSetMessageBufferSize == 1) {
                                result = _messageEncoder.Encode(input, _maxEncodedMessageSize);
                            }
                            else {
                                result = _messageEncoder.EncodeBatch(input, _maxEncodedMessageSize);
                            }
                        }
                        catch (Exception e) {
                            _logger.Error(e, "Encoding failure");
                            result = Enumerable.Empty<NetworkMessageModel>();
                        }
                        return result;
                    })
                    .Do(input => Interlocked.Add(ref _messageSending, input.Count()))
                    .Select(input => Observable.FromAsync(ct => _messageSink.SendAsync(input), Scheduler.Immediate))
                    .Merge(1) // single concurrency sending to keep order
                    .Subscribe()) {

                    await _messageTrigger.RunAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                IsRunning = false;
                _diagnosticsOutputTimer.Change(Timeout.Infinite, Timeout.Infinite);
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
            var valueChangesPerSec = _messageTrigger.ValueChangesCount / totalDuration;
            var dataChangesPerSec = _messageTrigger.DataChangesCount / totalDuration;
            var encoderValuesPerSec = _messageEncoder.NotificationsProcessedCount / totalDuration;
            var sentMessagesPerSec = totalDuration > 0 ? _messageSink.SentMessagesCount / totalDuration : 0;
            var messageSizeAveragePercent = Math.Round(_messageEncoder.AvgMessageSize / _maxEncodedMessageSize * 100);
            var messageSizeAveragePercentFormatted = $"({messageSizeAveragePercent}%)";
            var chunkSizeAverage = _messageEncoder.AvgMessageSize / (4 * 1024);
            var estimatedMsgChunksPerDay = Math.Ceiling(chunkSizeAverage) * sentMessagesPerSec * 60 * 60 * 24;
            var deviceId = _identity.DeviceId ?? "";
            var moduleId = _identity.ModuleId ?? "";

            ThreadPool.GetAvailableThreads(out var curWorkerThreads, out var curIoThreads);
            ThreadPool.GetMinThreads(out var minWorkerThreads, out var minIoThreads);
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxIoThreads);

            var diagInfo = new StringBuilder();
            diagInfo.AppendLine("\n  DIAGNOSTICS INFORMATION for          : {host} ({deviceId}; {moduleId})");
            diagInfo.AppendLine("  # Ingestion duration                 : {duration,14:dd\\:hh\\:mm\\:ss} (dd:hh:mm:ss)");
            var dataChangesPerSecFormatted = _messageTrigger.DataChangesCount > 0 && totalDuration > 0 ? $"({dataChangesPerSec:#,0.##}/s)" : "";
            diagInfo.AppendLine("  # Ingress DataChanges (from OPC)     : {dataChangesCount,14:n0} {dataChangesPerSecFormatted}");
            var valueChangesPerSecFormatted = _messageTrigger.ValueChangesCount > 0 && totalDuration > 0 ? $"({valueChangesPerSec:#,0.##}/s)" : "";
            diagInfo.AppendLine("  # Ingress ValueChanges (from OPC)    : {valueChangesCount,14:n0} {valueChangesPerSecFormatted}");

            diagInfo.AppendLine("  # Connection retries (OPC Server)    : {connectionRetries,14:0}");
            diagInfo.AppendLine("  # Ingress batchsize/trigger interval : {batchSize,14:0} | {batchTriggerInterval}");
            var encodervaluesPerSecFormatted = _messageEncoder.NotificationsProcessedCount > 0 && totalDuration > 0 ? $"({encoderValuesPerSec:#,0.##}/s)" : "";
            diagInfo.AppendLine("  # Encoder ValueChanges processed     : {notificationsProcessedCount,14:n0} {encodervaluesPerSecFormatted}");
            diagInfo.AppendLine("  # Encoder ValueChanges dropped       : {notificationsDroppedCount,14:n0}");
            diagInfo.AppendLine("  # Encoder IoT Messages produced      : {messagesProcessedCount,14:n0}");
            diagInfo.AppendLine("  # Encoder avg ValueChanges/Message   : {notificationsPerMessage,14:0}");
            diagInfo.AppendLine("  # Encoder avg IoT Message body size  : {messageSizeAverage,14:n0} {messageSizeAveragePercentFormatted}");
            diagInfo.AppendLine("  # Encoder avg IoT Unit (4 KB) usage  : {chunkSizeAverage,14:0.#}");
            diagInfo.AppendLine("  # Estimated IoT Units (4 KB) per day : {estimatedMsgChunksPerDay,14:n0}");

            var sentMessagesPerSecFormatted = _messageSink.SentMessagesCount > 0 && totalDuration > 0 ? $"({sentMessagesPerSec:0.##}/s)" : "";
            diagInfo.AppendLine("  # Sink message send call count       : {messageSending,14:n0}");
            diagInfo.AppendLine("  # Sent IoT message count             : {messageSinkSentMessagesCount,14:n0} {sentMessagesPerSecFormatted}");
            diagInfo.AppendLine("  # IoT message send failure count     : {messageSinkSentFailures,14:n0}");
            diagInfo.AppendLine("  # Memory (Workingset / Private)      : {workingSet,14:0} | {privateMemory} kb");
            diagInfo.AppendLine("  # Handle count                       : {handleCount,14:0}");
            diagInfo.AppendLine("  # Threadpool Work Items / completed  : {pendingWorkItems,14:0} | {completedWorkItems} {threadCount}");
            diagInfo.AppendLine("  # Threadpool Worker Threads          : {curWorkerThreads,14:0} (min: {minWorkerThreads} / max:{maxWorkerThrads})");
            diagInfo.AppendLine("  # Threadpool IO Threads              : {curIoThreads,14:0} (min: {minIoThreads} / max:{maxIoThreads})");

            _logger.Information(diagInfo.ToString(),
                Name, deviceId, moduleId,
                TimeSpan.FromSeconds(totalDuration),
                _messageTrigger.DataChangesCount, dataChangesPerSecFormatted,
                _messageTrigger.ValueChangesCount, valueChangesPerSecFormatted,
                _messageTrigger.NumberOfConnectionRetries,
                _dataSetMessageBufferSize, _batchTriggerInterval,
                _messageEncoder.NotificationsProcessedCount, encodervaluesPerSecFormatted,
                _messageEncoder.NotificationsDroppedCount,
                _messageEncoder.MessagesProcessedCount,
                _messageEncoder.AvgNotificationsPerMessage,
                _messageEncoder.AvgMessageSize, messageSizeAveragePercentFormatted,
                chunkSizeAverage,
                estimatedMsgChunksPerDay,
                _messageSending,
                _messageSink.SentMessagesCount, sentMessagesPerSecFormatted,
                _messageSink.SendErrorCount,
                Process.GetCurrentProcess().WorkingSet64 / 1024, Process.GetCurrentProcess().PrivateMemorySize64 / 1024,
                Process.GetCurrentProcess().HandleCount,
                ThreadPool.PendingWorkItemCount, ThreadPool.CompletedWorkItemCount, ThreadPool.ThreadCount,
                curWorkerThreads, minWorkerThreads, maxWorkerThreads,
                curIoThreads, minIoThreads, maxIoThreads);

            kDataChangesCount.WithLabels(deviceId, moduleId, Name)
                .Set(_messageTrigger.DataChangesCount);
            kValueChangesCount.WithLabels(deviceId, moduleId, Name)
                .Set(_messageTrigger.ValueChangesCount);
            kNotificationsProcessedCount.WithLabels(deviceId, moduleId, Name)
                .Set(_messageEncoder.NotificationsProcessedCount);
            kNotificationsDroppedCount.WithLabels(deviceId, moduleId, Name)
                .Set(_messageEncoder.NotificationsDroppedCount);
            kMessagesProcessedCount.WithLabels(deviceId, moduleId, Name)
                .Set(_messageEncoder.MessagesProcessedCount);
            kSentMessagesCount.WithLabels(deviceId, moduleId, Name)
                .Set(_messageSink.SentMessagesCount);
            kNumberOfConnectionRetries.WithLabels(deviceId, moduleId, Name)
                .Set(_messageTrigger.NumberOfConnectionRetries);
        }

        private readonly int _dataSetMessageBufferSize = 1;
        private readonly TimeSpan _batchTriggerInterval;
        private readonly int _maxEncodedMessageSize = 256 * 1024;
        private long _messageSending;

        private readonly IEngineConfiguration _config;
        private readonly IMessageSink _messageSink;
        private readonly IMessageEncoder _messageEncoder;
        private readonly IMessageTrigger _messageTrigger;
        private readonly ILogger _logger;
        private readonly IIdentity _identity;

        private readonly Timer _diagnosticsOutputTimer;
        private readonly TimeSpan _diagnosticInterval;
        private DateTime _diagnosticStart = DateTime.MinValue;

        private static readonly GaugeConfiguration kGaugeConfig = new GaugeConfiguration {
            LabelNames = new[] { "deviceid", "module", "triggerid" }
        };
        private static readonly Gauge kValueChangesCount = Metrics.CreateGauge(
            "iiot_edge_publisher_value_changes",
            "Opc ValuesChanges delivered for processing", kGaugeConfig);
        private static readonly Gauge kDataChangesCount = Metrics.CreateGauge(
            "iiot_edge_publisher_data_changes",
            "Opc DataChanges delivered for processing", kGaugeConfig);
        private static readonly Gauge kSentMessagesCount = Metrics.CreateGauge(
            "iiot_edge_publisher_sent_iot_messages",
            "IoT messages sent to hub", kGaugeConfig);
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
    }
}
