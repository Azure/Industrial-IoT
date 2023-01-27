// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Prometheus;
    using Serilog;
    using System;
    using System.Globalization;
    using System.Threading.Tasks;

    /// <summary>
    /// Iot hub client sink
    /// </summary>
    public class IoTHubMessageSink : IMessageSink {

        /// <inheritdoc/>
        public long SentMessagesCount { get; private set; }

        /// <inheritdoc/>
        public int MaxBodySize => _clientAccessor.Client.MaxBodySize;

        /// <summary>
        /// Create IoT hub message sink
        /// </summary>
        /// <param name="clientAccessor"></param>
        /// <param name="logger"></param>
        public IoTHubMessageSink(IClientAccessor clientAccessor, ILogger logger) {
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
                if (SentMessagesCount > kMessageCounterResetThreshold) {
                    _logger.Debug("Message counter has been reset to prevent overflow. " +
                        "So far, {SentMessagesCount} messages has been sent to IoT Hub.",
                        SentMessagesCount);
                    kMessagesSent.WithLabels(IotHubMessageSinkGuid, IotHubMessageSinkStartTime).Set(SentMessagesCount);
                    SentMessagesCount = 0;
                }
                using (kSendingDuration.NewTimer()) {
                    try {
                        await _clientAccessor.Client.SendEventAsync(message).ConfigureAwait(false);
                    }
                    catch (Exception e) {
                        _logger.Error(e, "Error sending message(s) to IoT Hub");
                    }
                }
                SentMessagesCount++;
                kMessagesSent.WithLabels(IotHubMessageSinkGuid, IotHubMessageSinkStartTime).Set(SentMessagesCount);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Error while sending messages to IoT Hub."); // we do not set the block into a faulted state.
            }
            finally {
                message.Dispose();
            }
        }

        private const long kMessageCounterResetThreshold = long.MaxValue - 10000;
        private readonly ILogger _logger;
        private readonly IClientAccessor _clientAccessor;
        private readonly string IotHubMessageSinkGuid = Guid.NewGuid().ToString();
        private readonly string IotHubMessageSinkStartTime = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
                        CultureInfo.InvariantCulture);
        private static readonly Gauge kMessagesSent = Metrics.CreateGauge(
            "iiot_edge_publisher_messages", "Number of messages sent to IotHub",
                new GaugeConfiguration {
                    LabelNames = new[] { "runid", "timestamp_utc" }
                });
        private static readonly Histogram kSendingDuration = Metrics.CreateHistogram(
            "iiot_edge_publisher_messages_duration", "Histogram of message sending durations");
    }
}
