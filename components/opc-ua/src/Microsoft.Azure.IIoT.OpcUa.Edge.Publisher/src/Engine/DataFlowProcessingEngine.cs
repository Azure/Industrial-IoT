// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Exceptions;
    using Serilog;
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Dataflow engine
    /// </summary>
    public class DataFlowProcessingEngine : IProcessingEngine, IDisposable {

        /// <inheritdoc/>
        public IMessageEncoder MessageEncoder { get; private set; }

        /// <inheritdoc/>
        public bool IsRunning { get; private set; }

        /// <inheritdoc/>
        public string Name => _messageTrigger.Id;

        /// <summary>
        /// Create engine
        /// </summary>
        /// <param name="messageTrigger"></param>
        /// <param name="messageSink"></param>
        /// <param name="engineConfiguration"></param>
        /// <param name="logger"></param>
        public DataFlowProcessingEngine(IMessageTrigger messageTrigger,
            IMessageSink messageSink, IEngineConfiguration engineConfiguration, ILogger logger) {
            _messageTrigger = messageTrigger;
            _messageSink = messageSink;
            _logger = logger;

            _messageTrigger.MessageReceived += MessageTriggerMessageReceived;

            if (engineConfiguration.DiagnosticsInterval.HasValue &&
                engineConfiguration.DiagnosticsInterval > TimeSpan.Zero) {
                _diagnosticsOutputTimer = new Timer(DiagnosticsOutputTimer_Elapsed, null, 0,
                    (int)engineConfiguration.DiagnosticsInterval.Value.TotalMilliseconds);
            }

            if (engineConfiguration.BatchSize.HasValue && engineConfiguration.BatchSize.Value > 1) {
                _bufferSize = engineConfiguration.BatchSize.Value;
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            _messageTrigger.MessageReceived -= MessageTriggerMessageReceived;
            _diagnosticsOutputTimer?.Dispose();
        }

        /// <inheritdoc/>
        public Task<JToken> GetCurrentJobState() {
            return Task.FromResult<JToken>(null);
        }

        /// <inheritdoc/>
        public async Task RunAsync(ProcessMode processMode, CancellationToken cancellationToken) {
            if (MessageEncoder == null) {
                throw new NotInitializedException();
            }

            try {
                if (IsRunning) {
                    return;
                }

                IsRunning = true;

                _encodingBlock = new TransformBlock<IMessageData, EncodedMessage>(
                    input => MessageEncoder.Encode(input),
                    new ExecutionDataflowBlockOptions {
                        CancellationToken = cancellationToken
                    });

                _batchBlock = new BatchBlock<EncodedMessage>(_bufferSize,
                    new GroupingDataflowBlockOptions {
                        CancellationToken = cancellationToken
                    });

                _sinkBlock = new ActionBlock<EncodedMessage[]>(
                    input => _messageSink.SendAsync(input),
                    new ExecutionDataflowBlockOptions {
                        CancellationToken = cancellationToken
                    });

                _encodingBlock.LinkTo(_batchBlock);
                _batchBlock.LinkTo(_sinkBlock);

                await _messageTrigger.RunAsync(cancellationToken);

                IsRunning = false;
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
        public void Initialize(IMessageEncoder encoder) {
            if (MessageEncoder != null) {
                throw new AlreadyInitializedException();
            }
            MessageEncoder = encoder;
        }

        /// <summary>
        /// Diagnostics timer
        /// </summary>
        /// <param name="state"></param>
        private void DiagnosticsOutputTimer_Elapsed(object state) {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("   DIAGNOSTICS INFORMATION");
            sb.AppendLine("   =======================");
            sb.AppendLine($"   # Messages Sent to IoT Hub: {_messageSink.SentMessagesCount}");
            sb.AppendLine($"   # Number of connection retries since last error: {_messageTrigger.NumberOfConnectionRetries}");
            sb.AppendLine("   =======================");
            _logger.Information(sb.ToString());
            // TODO: Use structured logging!
        }

        /// <summary>
        /// Message received handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void MessageTriggerMessageReceived(object sender, MessageReceivedEventArgs args) {
            _encodingBlock.Post(args.Message);
        }

        private readonly int _bufferSize = 1;
        private readonly Timer _diagnosticsOutputTimer;
        private readonly ILogger _logger;
        private readonly IMessageSink _messageSink;
        private readonly IMessageTrigger _messageTrigger;

        private BatchBlock<EncodedMessage> _batchBlock;
        private TransformBlock<IMessageData, EncodedMessage> _encodingBlock;
        private ActionBlock<EncodedMessage[]> _sinkBlock;
    }
}