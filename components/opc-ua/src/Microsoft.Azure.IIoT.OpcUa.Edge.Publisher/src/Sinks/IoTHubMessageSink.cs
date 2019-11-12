// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Sinks {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.Devices.Client;
    using Serilog;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Iot hub client sink
    /// </summary>
    public class IoTHubMessageSink : IMessageSink, IDisposable {

        /// <inheritdoc/>
        public long SentMessagesCount { get; private set; }

        /// <summary>
        /// Create IoT hub message sink
        /// </summary>
        /// <param name="clientFactory"></param>
        /// <param name="logger"></param>
        public IoTHubMessageSink(IClientFactory clientFactory, ILogger logger) {
            _client = clientFactory.CreateAsync().Result;
            // TODO : Use higher level abstraction in module framework to send
            _logger = logger;
        }

        /// <inheritdoc/>
        public void Dispose() {
            // TODO : Use higher level abstraction in module framework to send
            _client.Dispose();
        }

        /// <inheritdoc/>
        public async Task SendAsync(EncodedMessage[] messages) {
            if (messages == null || !messages.Any()) {
                return;
            }
            var messageObjects = messages
                .Select(m => CreateMessage(m.Body, m.MessageSchema, m.ContentType, m.MessageId))
                .ToList();
            try {
                _logger.Verbose("Sending {count} objects to IoT Hub...", messages.Length);
                var messagesCount = messages.Length;

                if (SentMessagesCount > kMessageCounterResetThreshold) {
                    _logger.Debug("Message counter has been reset to prevent overflow. " +
                        "So far, {SentMessagesCount} messages has been sent to IoT Hub.", SentMessagesCount);
                    SentMessagesCount = 0;
                }
                SentMessagesCount += messages.Length;
                if (messages.Length == 1) {
                    await _client.SendEventAsync(messageObjects.First());
                }
                else {
                    await _client.SendEventBatchAsync(messageObjects);
                }
            }
            finally {
                messageObjects.ForEach(m => m.Dispose());
            }
        }

        /// <summary>
        /// Create messages
        /// </summary>
        /// <param name="body"></param>
        /// <param name="eventSchema"></param>
        /// <param name="contentType"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        private static Message CreateMessage(string body, string eventSchema,
            string contentType, string messageId) {
            var msg = new Message(Encoding.UTF8.GetBytes(body)) {
                ContentType = contentType,
                ContentEncoding = "utf-8",
                MessageId = messageId,
                CreationTimeUtc = DateTime.UtcNow
            };
            if (!string.IsNullOrEmpty(eventSchema)) {
                msg.Properties.Add(CommonProperties.EventSchemaType, eventSchema);
            }
            msg.Properties.Add(CommonProperties.ContentEncoding,
                "utf-8");
            msg.Properties.Add(CommonProperties.CreationTimeUtc,
                msg.CreationTimeUtc.ToString());
            return msg;
        }

        private const long kMessageCounterResetThreshold = long.MaxValue - 1000;
        private readonly IClient _client;
        private readonly ILogger _logger;
    }
}