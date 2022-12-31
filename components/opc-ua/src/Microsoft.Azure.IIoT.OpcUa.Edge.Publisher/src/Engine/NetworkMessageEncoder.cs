// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.IIoT.OpcUa.Core;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using Opc.Ua.Encoders;
    using Opc.Ua.PubSub;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Creates PubSub encoded messages
    /// </summary>
    public class NetworkMessageEncoder : IMessageEncoder {

        /// <inheritdoc/>
        public uint NotificationsDroppedCount { get; private set; }

        /// <inheritdoc/>
        public uint NotificationsProcessedCount { get; private set; }

        /// <inheritdoc/>
        public uint MessagesProcessedCount { get; private set; }

        /// <inheritdoc/>
        public double AvgNotificationsPerMessage { get; private set; }

        /// <inheritdoc/>
        public double AvgMessageSize { get; private set; }

        /// <summary>
        /// Create instance of NetworkMessageEncoder.
        /// </summary>
        /// <param name="logger"> Logger to be used for reporting. </param>
        /// <param name="engineConfig"> injected configuration. </param>
        public NetworkMessageEncoder(ILogger logger, IEngineConfiguration engineConfig) {
            _logger = logger;
            _enableRoutingInfo = engineConfig.EnableRoutingInfo;
            _useStandardsCompliantEncoding = engineConfig.UseStandardsCompliantEncoding;
        }

        /// <inheritdoc/>
        public Task<IEnumerable<NetworkMessageChunkModel>> EncodeAsync(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize) {
            try {
                var result = EncodeInternal(messages, maxMessageSize, false);
                return Task.FromResult<IEnumerable<NetworkMessageChunkModel>>(result.ToList());
            }
            catch (Exception e) {
                _logger.Error(e, "Failed to encode {numOfMessages} messages", messages.Count());
                return Task.FromResult(Enumerable.Empty<NetworkMessageChunkModel>());
            }
        }

        /// <inheritdoc/>
        public Task<IEnumerable<NetworkMessageChunkModel>> EncodeBatchAsync(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize) {
            try {
                var result = EncodeInternal(messages, maxMessageSize, true);
                return Task.FromResult<IEnumerable<NetworkMessageChunkModel>>(result.ToList());
            }
            catch (Exception e) {
                _logger.Error(e, "Failed to encode {numOfMessages} messages", messages.Count());
                return Task.FromResult(Enumerable.Empty<NetworkMessageChunkModel>());
            }
        }

        /// <summary>
        /// Encode messages
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="maxMessageSize"></param>
        /// <param name="isBatched"></param>
        /// <returns></returns>
        public IEnumerable<NetworkMessageChunkModel> EncodeInternal(IEnumerable<DataSetMessageModel> messages,
            int maxMessageSize, bool isBatched) {

            //
            // by design all messages are generated in the same session context, therefore it is safe to
            // get the first message's context
            //
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)?.ServiceMessageContext;
            var chunkedMessages = new List<NetworkMessageChunkModel>();
            if (encodingContext == null) {
                // Drop all messages
                Drop(messages);
                return chunkedMessages;
            }

            var networkMessages = GetNetworkMessages(messages, isBatched);
            foreach (var (notificationsPerMessage, networkMessage) in networkMessages) {
                var chunks = networkMessage.Encode(encodingContext, maxMessageSize);
                var tooBig = 0;
                var notificationsPerChunk = notificationsPerMessage / chunks.Count;
                foreach (var body in chunks) {
                    if (body == null) {
                        //
                        // Failed to press a notification into message size limit
                        // This is somewhat correct as the smallest dropped chunk is
                        // a message containing only a single data set message which
                        // contains (parts) of a notification.
                        //
                        _logger.Warning("Resulting chunk is too large, dropped a notification.");
                        tooBig++;
                        continue;
                    }

                    chunkedMessages.Add(new NetworkMessageChunkModel {
                        Body = body,
                        ContentEncoding = "utf-8",
                        Timestamp = DateTime.UtcNow,
                        ContentType = ContentMimeType.Json,
                        MessageSchema = MessageSchemaTypes.NetworkMessageJson,
                        RoutingInfo = _enableRoutingInfo ? networkMessage.DataSetWriterGroup : null,
                    });

                    AvgMessageSize = (AvgMessageSize * MessagesProcessedCount + body.Length) /
                        (MessagesProcessedCount + 1);
                    AvgNotificationsPerMessage = (AvgNotificationsPerMessage * MessagesProcessedCount +
                        notificationsPerChunk) / (MessagesProcessedCount + 1);
                    MessagesProcessedCount++;
                }

                // We dropped a number of notifications but processed the remainder successfully
                NotificationsDroppedCount += (uint)tooBig;
                if (notificationsPerMessage > tooBig) {
                    NotificationsProcessedCount += (uint)(notificationsPerMessage - tooBig);
                }
            }
            return chunkedMessages;
        }

        /// <summary>
        /// Produce network messages from the data set message model
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="isBatched"></param>
        /// <returns></returns>
        private List<(int, PubSubMessage)> GetNetworkMessages(IEnumerable<DataSetMessageModel> messages, bool isBatched) {
            var result = new List<(int, PubSubMessage)>();
            // Group messages by publisher, then writer group and then by dataset class id
            foreach (var publishers in messages.GroupBy(m => m.PublisherId)) {
                var publisherId = publishers.Key;
                foreach (var groups in publishers.GroupBy(m => m.WriterGroup)) {
                    var writerGroup = groups.Key;
                    if (writerGroup?.MessageSettings == null) {
                        // Must have a writer group
                        Drop(groups);
                        continue;
                    }
                    var encoding = writerGroup.MessageType ?? MessageEncoding.Json;
                    var networkMessageContentMask =
                        writerGroup.MessageSettings.NetworkMessageContentMask.ToStackType(encoding);
                    foreach (var dataSetClass in groups
                        .GroupBy(m => m.Writer?.DataSet?.DataSetMetaData?.DataSetClassId ?? Guid.Empty)) {

                        var dataSetClassId = dataSetClass.Key;
                        var currentMessage = CreateMessage(writerGroup, encoding,
                            networkMessageContentMask, dataSetClassId, publisherId);
                        var currentNotificationCount = 0;
                        foreach (var message in dataSetClass) {
                            if (message.Writer == null) {
                                // Must have a writer
                                Drop(message.YieldReturn());
                                continue;
                            }
                            var dataSetMessageContentMask =
                                (message.Writer.MessageSettings?.DataSetMessageContentMask).ToStackType(encoding);
                            var dataSetFieldContentMask = message.Writer.DataSetFieldContentMask.ToStackType();
                            if (message.Notifications != null) {
                                var notificationQueues = message.Notifications
                                    .GroupBy(m => m.DataSetFieldName)
                                    .Select(c => new Queue<MonitoredItemNotificationModel>(c.ToArray()))
                                    .ToArray();

                                while (notificationQueues.Any(q => q.Any())) {
                                    var payload = notificationQueues
                                        .Select(q => q.Any() ? q.Dequeue() : null)
                                        .Where(s => s != null)
                                        .ToDictionary(
                                            s => s.DataSetFieldName,
                                            s => s.Value);

                                    BaseDataSetMessage dataSetMessage = encoding.HasFlag(MessageEncoding.Json)
                                        ? new JsonDataSetMessage {

                                        } : new UadpDataSetMessage {
                                            MetaData = message.MetaData,

                                        };

                                    dataSetMessage.DataSetWriterId = message.SubscriptionId;
                                    dataSetMessage.DataSetWriterName = message.Writer.DataSetWriterName;
                                    dataSetMessage.MessageType = message.MessageType;
                                    dataSetMessage.MetaDataVersion = message.MetaData?.ConfigurationVersion
                                        ?? new ConfigurationVersionDataType { MajorVersion = 1 };
                                    dataSetMessage.DataSetMessageContentMask = dataSetMessageContentMask;
                                    dataSetMessage.Timestamp = message.TimeStamp ?? DateTime.UtcNow;
                                    dataSetMessage.Picoseconds = 0;
                                    dataSetMessage.SequenceNumber = message.SequenceNumber;
                                    dataSetMessage.Status = payload.Values.Any(s => StatusCode.IsNotGood(s.StatusCode)) ?
                                        StatusCodes.Bad : StatusCodes.Good;
                                    dataSetMessage.Payload = new DataSet(payload, (uint)dataSetFieldContentMask);
                                    currentMessage.Messages.Add(dataSetMessage);

                                    if (writerGroup.MessageSettings?.MaxMessagesPerPublish != null &&
                                        currentMessage.Messages.Count >= writerGroup.MessageSettings.MaxMessagesPerPublish) {
                                        result.Add((currentNotificationCount, currentMessage));
                                        currentMessage = CreateMessage(writerGroup, encoding, networkMessageContentMask,
                                            dataSetClassId, publisherId);
                                        currentNotificationCount = 0;
                                    }
                                }
                                currentNotificationCount++;
                            }
                            else if (message.MetaData != null) {
                                if (currentMessage.Messages.Count > 0) {
                                    // Start a new message but first emit current
                                    result.Add((currentNotificationCount, currentMessage));
                                    currentMessage = CreateMessage(writerGroup, encoding, networkMessageContentMask,
                                        dataSetClassId, publisherId);
                                    currentNotificationCount = 0;
                                }
                                var metaData = (DataSetMetaDataType)Utils.Clone(message.MetaData);
                                metaData.Description = message.Writer?.DataSet?.DataSetMetaData?.Description;
                                metaData.Name = message.Writer?.DataSet?.DataSetMetaData?.Name;

                                PubSubMessage metadataMessage = encoding.HasFlag(MessageEncoding.Json)
                                    ? new JsonMetadataMessage {
                                        UseAdvancedEncoding = !_useStandardsCompliantEncoding,
                                        UseGzipCompression = encoding.HasFlag(MessageEncoding.Gzip),
                                        DataSetWriterId = message.SubscriptionId,
                                        MetaData = metaData,
                                        DataSetWriterName = message.Writer.DataSetWriterName
                                    } : new UadpMetadataMessage {
                                        DataSetWriterId = message.SubscriptionId,
                                        MetaData = metaData
                                    };
                                metadataMessage.MessageId = Guid.NewGuid().ToString();
                                metadataMessage.PublisherId = publisherId;
                                metadataMessage.DataSetWriterGroup = writerGroup.WriterGroupId;
                                result.Add((0, metadataMessage));
                            }
                        }
                        if (currentMessage.Messages.Count > 0) {
                            result.Add((currentNotificationCount, currentMessage));
                        }

                        BaseNetworkMessage CreateMessage(WriterGroupModel writerGroup, MessageEncoding encoding,
                            uint networkMessageContentMask, Guid dataSetClassId, string publisherId) {
                            BaseNetworkMessage currentMessage = encoding.HasFlag(MessageEncoding.Json) ? new JsonNetworkMessage {
                                UseAdvancedEncoding = !_useStandardsCompliantEncoding,
                                UseGzipCompression = encoding.HasFlag(MessageEncoding.Gzip),
                                UseArrayEnvelope = !_useStandardsCompliantEncoding && isBatched,
                            } : new UadpNetworkMessage {
                                //   WriterGroupId = writerGroup.Index,
                                //   GroupVersion = writerGroup.Version,
                                Timestamp = DateTime.UtcNow,
                                PicoSeconds = 0
                            };
                            currentMessage.MessageId = Guid.NewGuid().ToString();
                            currentMessage.NetworkMessageContentMask = networkMessageContentMask;
                            currentMessage.PublisherId = publisherId;
                            currentMessage.DataSetClassId = dataSetClassId;
                            currentMessage.DataSetWriterGroup = writerGroup.WriterGroupId;
                            return currentMessage;
                        }
                    }
                }
            }
            return result;
        }

        private void Drop(IEnumerable<DataSetMessageModel> messages) {
            int totalNotifications = messages.Sum(m => m?.Notifications?.Count ?? 0);
            NotificationsDroppedCount += (uint)totalNotifications;
            _logger.Warning("Dropped {totalNotifications} values", totalNotifications);
        }

        private readonly ILogger _logger;
        private readonly bool _enableRoutingInfo;
        private readonly bool _useStandardsCompliantEncoding;
    }
}
