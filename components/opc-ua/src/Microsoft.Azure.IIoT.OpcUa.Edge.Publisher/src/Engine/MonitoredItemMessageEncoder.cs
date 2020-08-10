// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Core;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using Opc.Ua.Encoders;
    using Opc.Ua.Extensions;
    using Opc.Ua.PubSub;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// Publisher monitored item message encoder
    /// </summary>
    public class MonitoredItemMessageEncoder : IMessageEncoder {

        /// <inheritdoc/>
        public uint NotificationsDroppedCount { get; private set; }

        /// <inheritdoc/>
        public ulong NotificationsProcessedCount { get; private set; }

        /// <inheritdoc/>
        public ulong MessagesProcessedCount { get; private set; }

        /// <inheritdoc/>
        public double AvgNotificationsPerMessage { get; private set; }

        /// <inheritdoc/>
        public double AvgMessageSize { get; private set; }

        /// <inheritdoc/>
        private readonly ILogger _logger;

        /// <inheritdoc/>
        public MonitoredItemMessageEncoder(ILogger logger) {
            _logger = logger;
        }

        /// <inheritdoc/>
        public Task<IEnumerable<NetworkMessageModel>> EncodeAsync(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize) {
            try {
                var resultJson = EncodeAsJson(messages, maxMessageSize);
                var resultUadp = EncodeAsUadp(messages, maxMessageSize);
                var result = resultJson.Concat(resultUadp);
                return Task.FromResult(result);
            }
            catch (Exception e) {
                return Task.FromException<IEnumerable<NetworkMessageModel>>(e);
            }
        }

        /// <inheritdoc/>
        public Task<IEnumerable<NetworkMessageModel>> EncodeBatchAsync(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize) {
            try {
                var resultJson = EncodeBatchAsJson(messages, maxMessageSize);
                var resultUadp = EncodeBatchAsUadp(messages, maxMessageSize);
                var result = resultJson.Concat(resultUadp);
                return Task.FromResult(result);
            }
            catch (Exception e) {
                return Task.FromException<IEnumerable<NetworkMessageModel>>(e);
            }
        }

        /// <summary>
        /// Perform DataSetMessageModel to NetworkMessageModel batch using Json encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="maxMessageSize"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeBatchAsJson(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize) {

            // by design all messages are generated in the same session context,
            // therefore it is safe to get the first message's context
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)
                ?.ServiceMessageContext;
            var notifications = GetMonitoredItemMessages(messages, MessageEncoding.Json, encodingContext);
            if (notifications.Count() == 0) {
                yield break;
            }
            var current = notifications.GetEnumerator();
            var processing = current.MoveNext();
            var messageSize = 2; // array brackets
            var chunk = new Collection<MonitoredItemMessage>();
            while (processing) {
                var notification = current.Current;
                var messageCompleted = false;
                if (notification != null) {
                    var helperWriter = new StringWriter();
                    var helperEncoder = new JsonEncoderEx(helperWriter, encodingContext) {
                        UseAdvancedEncoding = true,
                        UseUriEncoding = true,
                        UseReversibleEncoding = false
                    };
                    notification.Encode(helperEncoder);
                    helperEncoder.Close();
                    var notificationSize = Encoding.UTF8.GetByteCount(helperWriter.ToString());
                    if (notificationSize > maxMessageSize) {
                        // Message too large, drop it.
                        NotificationsDroppedCount++;
                        _logger.Warning("Message too large, dropped {notificationsPerMessage} values");
                        processing = current.MoveNext();
                    }
                    else {
                        messageCompleted = maxMessageSize < (messageSize + notificationSize);
                        if (!messageCompleted) {
                            NotificationsProcessedCount++;
                            chunk.Add(notification);
                            processing = current.MoveNext();
                            messageSize += notificationSize + (processing ? 1 : 0);
                        }
                    }
                }
                if (!processing || messageCompleted) {
                    var writer = new StringWriter();
                    var encoder = new JsonEncoderEx(writer, encodingContext,
                        JsonEncoderEx.JsonEncoding.Array) {
                        UseAdvancedEncoding = true,
                        UseUriEncoding = true,
                        UseReversibleEncoding = false
                    };
                    foreach(var element in chunk) {
                        encoder.WriteEncodeable(null, element);
                    }
                    encoder.Close();
                    var encoded = new NetworkMessageModel {
                        Body = Encoding.UTF8.GetBytes(writer.ToString()),
                        ContentEncoding = "utf-8",
                        Timestamp = DateTime.UtcNow,
                        ContentType = ContentMimeType.UaJson,
                        MessageSchema = MessageSchemaTypes.MonitoredItemMessageJson
                    };
                    AvgMessageSize = (AvgMessageSize * MessagesProcessedCount + encoded.Body.Length) /
                        (MessagesProcessedCount + 1);
                    AvgNotificationsPerMessage = (AvgNotificationsPerMessage * MessagesProcessedCount +
                        chunk.Count) / (MessagesProcessedCount + 1);
                        MessagesProcessedCount++;
                    chunk.Clear();
                    messageSize = 2;
                    yield return encoded;
                }
            }
        }

        /// <summary>
        /// Perform DataSetMessageModel to batch NetworkMessageModel using binary encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="maxMessageSize"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeBatchAsUadp(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize) {

            // by design all messages are generated in the same session context,
            // therefore it is safe to get the first message's context
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)
                ?.ServiceMessageContext;
            var notifications = GetMonitoredItemMessages(messages, MessageEncoding.Uadp, encodingContext);
            if (notifications.Count() == 0) {
                yield break;
            }
            var current = notifications.GetEnumerator();
            var processing = current.MoveNext();
            var messageSize = 4; // array length size
            var chunk = new Collection<MonitoredItemMessage>();
            while (processing) {
                var notification = current.Current;
                var messageCompleted = false;
                if (notification != null) {
                    var helperEncoder = new BinaryEncoder(encodingContext);
                    helperEncoder.WriteEncodeable(null, notification);
                    var notificationSize = helperEncoder.CloseAndReturnBuffer().Length;
                    if (notificationSize > maxMessageSize) {
                        // Message too large, drop it.
                        NotificationsDroppedCount++;
                        _logger.Warning("Message too large, dropped {notificationsPerMessage} values");
                        processing = current.MoveNext();
                    }
                    else {
                        messageCompleted = maxMessageSize < (messageSize + notificationSize);
                        if (!messageCompleted) {
                            chunk.Add(notification);
                            NotificationsProcessedCount++;
                            processing = current.MoveNext();
                            messageSize += notificationSize;
                        }
                    }
                }
                if (!processing || messageCompleted) {
                    var encoder = new BinaryEncoder(encodingContext);
                    encoder.WriteBoolean(null, true); // is Batch
                    encoder.WriteEncodeableArray(null, chunk);
                    var encoded = new NetworkMessageModel {
                        Body = encoder.CloseAndReturnBuffer(),
                        Timestamp = DateTime.UtcNow,
                        ContentType = ContentMimeType.UaBinary,
                        MessageSchema = MessageSchemaTypes.MonitoredItemMessageBinary
                    };
                    AvgMessageSize = (AvgMessageSize * MessagesProcessedCount + encoded.Body.Length) /
                        (MessagesProcessedCount + 1);
                    AvgNotificationsPerMessage = (AvgNotificationsPerMessage * MessagesProcessedCount +
                        chunk.Count) / (MessagesProcessedCount + 1);
                    MessagesProcessedCount++;
                    chunk.Clear();
                    messageSize = 4;
                    yield return encoded;
                }
            }
        }

        /// <summary>
        /// Perform event to message Json encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="maxMessageSize"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeAsJson(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize) {

            // by design all messages are generated in the same session context,
            // therefore it is safe to get the first message's context
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)
                ?.ServiceMessageContext;
            var notifications = GetMonitoredItemMessages(messages, MessageEncoding.Json, encodingContext);
            if (notifications.Count() == 0) {
                yield break;
            }
            foreach (var networkMessage in notifications) {
                var writer = new StringWriter();
                var encoder = new JsonEncoderEx(writer, encodingContext) {
                    UseAdvancedEncoding = true,
                    UseUriEncoding = true,
                    UseReversibleEncoding = false
                };
                networkMessage.Encode(encoder);
                encoder.Close();
                var encoded = new NetworkMessageModel {
                    Body = Encoding.UTF8.GetBytes(writer.ToString()),
                    ContentEncoding = "utf-8",
                    Timestamp = DateTime.UtcNow,
                    ContentType = ContentMimeType.UaLegacyPublisher,
                    MessageSchema = MessageSchemaTypes.MonitoredItemMessageJson
                };
                if (encoded.Body.Length > maxMessageSize) {
                    // Message too large, drop it.
                    NotificationsDroppedCount++;
                    _logger.Warning("Message too large, dropped {notificationsPerMessage} values");
                    yield break;
                }
                NotificationsProcessedCount++;
                AvgMessageSize = (AvgMessageSize * MessagesProcessedCount + encoded.Body.Length) /
                    (MessagesProcessedCount + 1);
                AvgNotificationsPerMessage = (AvgNotificationsPerMessage * MessagesProcessedCount + 1) /
                    (MessagesProcessedCount + 1);
                MessagesProcessedCount++;
                yield return encoded;
            }
        }

        /// <summary>
        /// Perform event to message binary encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="maxMessageSize"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeAsUadp(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize) {

            // by design all messages are generated in the same session context,
            // therefore it is safe to get the first message's context
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)
                ?.ServiceMessageContext;
            var notifications = GetMonitoredItemMessages(messages, MessageEncoding.Uadp, encodingContext);
            if (notifications.Count() == 0) {
                yield break;
            }
            foreach (var networkMessage in notifications) {
                var encoder = new BinaryEncoder(encodingContext);
                encoder.WriteBoolean(null, false); // is not Batch
                encoder.WriteEncodeable(null, networkMessage);
                networkMessage.Encode(encoder);
                var encoded = new NetworkMessageModel {
                    Body = encoder.CloseAndReturnBuffer(),
                    Timestamp = DateTime.UtcNow,
                    ContentType = ContentMimeType.UaBinary,
                    MessageSchema = MessageSchemaTypes.MonitoredItemMessageBinary
                };
                if (encoded.Body.Length > maxMessageSize) {
                    // Message too large, drop it.
                    NotificationsDroppedCount++;
                    _logger.Warning("Message too large, dropped {notificationsPerMessage} values");
                    yield break;
                }
                NotificationsProcessedCount++;
                AvgMessageSize = (AvgMessageSize * MessagesProcessedCount + encoded.Body.Length) /
                    (MessagesProcessedCount + 1);
                AvgNotificationsPerMessage = (AvgNotificationsPerMessage * MessagesProcessedCount + 1) /
                    (MessagesProcessedCount + 1);
                MessagesProcessedCount++;
                yield return encoded;
            }
        }

        /// <summary>
        /// Produce Monitored Item Messages from the data set message model for the specified encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="encoding"></param>
        /// <param name="context"></param>
        private IEnumerable<MonitoredItemMessage> GetMonitoredItemMessages(
            IEnumerable<DataSetMessageModel> messages, MessageEncoding encoding,
            ServiceMessageContext context) {
            if (context?.NamespaceUris == null) {
                // Declare all notifications in messages as dropped.
                int totalNotifications = messages.Sum(m => m?.Notifications?.Count() ?? 0);
                NotificationsDroppedCount += (uint)totalNotifications;
                _logger.Warning("Namespace is empty, dropped {totalNotifications} values");
                yield break;
            }
            foreach (var message in messages) {
                if (message.WriterGroup?.MessageType.GetValueOrDefault(MessageEncoding.Json) == encoding) {
                    foreach (var notification in message.Notifications) {
                        var result = new MonitoredItemMessage {
                            MessageContentMask = (message.Writer?.MessageSettings?
                               .DataSetMessageContentMask).ToMonitoredItemMessageMask(
                                   message.Writer?.DataSetFieldContentMask),
                            ApplicationUri = message.ApplicationUri,
                            EndpointUrl = message.EndpointUrl,
                            ExtensionFields = message.Writer?.DataSet?.ExtensionFields,
                            NodeId = notification.NodeId.ToExpandedNodeId(context.NamespaceUris),
                            Timestamp = message.TimeStamp ?? DateTime.UtcNow,
                            Value = notification.Value,
                            DisplayName = notification.DisplayName,
                            SequenceNumber = notification.SequenceNumber.GetValueOrDefault(0)
                        };
                        // force published timestamp into to source timestamp for the legacy heartbeat compatibility
                        if (notification.IsHeartbeat &&
                            ((result.MessageContentMask & (uint)MonitoredItemMessageContentMask.Timestamp) == 0) &&
                            ((result.MessageContentMask & (uint)MonitoredItemMessageContentMask.SourceTimestamp) != 0)) {
                            result.Value.SourceTimestamp = result.Timestamp;
                        }
                        yield return result;
                    }
                }
            }
        }
    }
}