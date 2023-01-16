// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.Devices.Client;
    using Prometheus;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Iot hub client sink
    /// </summary>
    public class IoTHubMessageSink : IMessageSink {

        /// <inheritdoc/>
        public long SentMessagesCount { get; private set; }

        /// <inheritdoc/>
        public int MaxMessageSize => _clientAccessor.Client.MaxMessageSize;

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
        public async Task SendAsync(IEnumerable<NetworkMessageModel> messages) {
            if (messages == null || !messages.Any()) {
                return;
            }
            var routingInfo = messages.First().RoutingInfo;

            var messageObjects = messages
                .Select(m => CreateMessage(m.Body, m.MessageSchema,
                    m.ContentType, m.ContentEncoding, m.RoutingInfo))
                .ToList();
            try {
                var messagesCount = messageObjects.Count;
                _logger.Verbose("Sending {count} objects to IoT Hub...", messagesCount);

                if (SentMessagesCount > kMessageCounterResetThreshold) {
                    _logger.Debug("Message counter has been reset to prevent overflow. " +
                        "So far, {SentMessagesCount} messages has been sent to IoT Hub.",
                        SentMessagesCount);
                    kMessagesSent.WithLabels(IotHubMessageSinkGuid, IotHubMessageSinkStartTime).Set(SentMessagesCount);
                    SentMessagesCount = 0;
                }
                using (kSendingDuration.NewTimer()) {
                    var sw = new Stopwatch();
                    sw.Start();

                    try {
                        if (string.IsNullOrEmpty(routingInfo)) {
                            if (messagesCount == 1) {
                                await _clientAccessor.Client.SendEventAsync(messageObjects.First()).ConfigureAwait(false);
                            }
                            else {
                                await _clientAccessor.Client.SendEventBatchAsync(messageObjects).ConfigureAwait(false);
                            }
                        }
                        else {
                            if (messagesCount == 1) {
                                await _clientAccessor.Client.SendEventAsync(routingInfo, messageObjects.First()).ConfigureAwait(false);
                            }
                            else {
                                await _clientAccessor.Client.SendEventBatchAsync(routingInfo, messageObjects).ConfigureAwait(false);
                            }
                        }
                    }
                    catch (Exception e) {
                        _logger.Error(e, "Error sending message(s) to IoT Hub");
                    }

                    sw.Stop();
                    _logger.Verbose("Sent {count} messages in {time} to IoTHub.", messagesCount, sw.Elapsed);
                }
                SentMessagesCount += messagesCount;
                kMessagesSent.WithLabels(IotHubMessageSinkGuid, IotHubMessageSinkStartTime).Set(SentMessagesCount);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Error while sending messages to IoT Hub."); // we do not set the block into a faulted state.
            }
        }

        /// <summary>
        /// Create messages
        /// </summary>
        /// <param name="body"></param>
        /// <param name="eventSchema"></param>
        /// <param name="contentType"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="routingInfo"></param>
        /// <returns></returns>
        private static Message CreateMessage(byte[] body, string eventSchema,
            string contentType, string contentEncoding, string routingInfo) {
            var msg = new Message(body) {
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                // TODO - setting CreationTime causes issues in the Azure IoT java SDK
                //  revert the comment whrn the issue is fixed
                //  CreationTimeUtc = DateTime.UtcNow
            };
            if (!string.IsNullOrEmpty(eventSchema)) {
                msg.Properties.Add(CommonProperties.EventSchemaType, eventSchema);
            }
            if (!string.IsNullOrEmpty(contentType)) {
                msg.Properties.Add(SystemProperties.MessageSchema, contentType);
            }
            if (!string.IsNullOrEmpty(contentEncoding)) {
                msg.Properties.Add(CommonProperties.ContentEncoding, contentEncoding);
            }
            if (!string.IsNullOrEmpty(routingInfo)) {
                msg.Properties.Add(CommonProperties.RoutingInfo, routingInfo);
            }
            return msg;
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
