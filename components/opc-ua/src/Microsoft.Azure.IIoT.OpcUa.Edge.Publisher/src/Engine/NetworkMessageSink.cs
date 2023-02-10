// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Serilog;
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Threading.Tasks;

    /// <summary>
    /// Network message sink
    /// </summary>
    public class NetworkMessageSink : IMessageSink {

        /// <inheritdoc/>
        public int MaxMessageSize => _clientAccessor.Client.MaxEventBufferSize;

        /// <summary>
        /// Create network message sink
        /// </summary>
        /// <param name="clientAccessor"></param>
        /// <param name="metrics"></param>
        /// <param name="logger"></param>
        public NetworkMessageSink(IClientAccessor clientAccessor, IMetricsContext metrics,
            ILogger logger) : this(metrics ?? throw new ArgumentNullException(nameof(metrics))) {

            _clientAccessor = clientAccessor
                ?? throw new ArgumentNullException(nameof(clientAccessor));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public ITelemetryEvent CreateMessage() {
            return _clientAccessor.Client.CreateTelemetryEvent();
        }

        /// <inheritdoc/>
        public async Task SendAsync(ITelemetryEvent message) {
            if (message == null) {
                return;
            }
            try {
                var sw = Stopwatch.StartNew();
                try {
                    await _clientAccessor.Client.SendEventAsync(message).ConfigureAwait(false);
                    _messagesSentCount++;
                }
                catch (Exception e) {
                    kMessagesErrors.Add(1, _tagList);
                    _logger.Error(e, "Error sending network message(s)");
                }
                kSendingDuration.Record(sw.ElapsedMilliseconds, _tagList);
            }
            finally {
                message.Dispose();
            }
        }

        /// <summary>
        /// Create observable metric registrations
        /// </summary>
        private NetworkMessageSink(IMetricsContext metrics) {
            _tagList = metrics.TagList;
            Diagnostics.Meter.CreateObservableCounter("iiot_edge_publisher_sent_iot_messages",
                () => new Measurement<long>(_messagesSentCount, _tagList), "Messages",
                "Number of IoT messages successfully sent to Sink (IoT Hub or Edge Hub).");
            Diagnostics.Meter.CreateObservableGauge("iiot_edge_publisher_sent_iot_messages_per_second",
                () => new Measurement<double>(_messagesSentCount / UpTime, _tagList), "Messages/second",
                "IoT messages/second sent to Sink (IoT Hub or Edge Hub).");
            Diagnostics.Meter.CreateObservableGauge("iiot_edge_publisher_estimated_message_chunks_per_day",
                () => new Measurement<double>(_messagesSentCount, _tagList), "Messages/day",
                "Estimated 4kb message chunks used from daily quota.");
        }
        static Counter<long> kMessagesErrors = Diagnostics.Meter.CreateCounter<long>(
            "iiot_edge_publisher_failed_iot_messages", "messages", "Number of failures sending a network message.");
        static Histogram<double> kSendingDuration = Diagnostics.Meter.CreateHistogram<double>(
            "iiot_edge_publisher_messages_duration", "milliseconds", "Histogram of message sending durations.");

        private double UpTime => (DateTime.UtcNow - _startTime).TotalSeconds;
        private readonly DateTime _startTime = DateTime.UtcNow;
        private readonly ILogger _logger;
        private readonly IClientAccessor _clientAccessor;
        private readonly TagList _tagList;
        private long _messagesSentCount;
    }
}
