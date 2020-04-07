// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core;
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

    /// <summary>
    /// Publisher monitored item message encoder
    /// </summary>
    public class MonitoredItemMessageEncoder : IMessageEncoder {

        /// <inheritdoc/>
        public Task<IEnumerable<NetworkMessageModel>> EncodeAsync(
            IEnumerable<DataSetMessageModel> messages) {
            try {
                var resultJson = EncodeAsJson(messages);
                var resultUadp = EncodeAsUadp(messages);
                var result = resultJson.Concat(resultUadp);
                return Task.FromResult(result);
            }
            catch (Exception e) {
                return Task.FromException<IEnumerable<NetworkMessageModel>>(e);
            }
        }

        /// <inheritdoc/>
        public Task<IEnumerable<NetworkMessageModel>> EncodeBatchAsync(
            IEnumerable<DataSetMessageModel> messages) {
            try {
                var resultJson = EncodeBatchAsJson(messages);
                var resultUadp = EncodeBatchAsUadp(messages);
                var result = resultJson.Concat(resultUadp);
                return Task.FromResult(result);
            }
            catch (Exception e){
                return Task.FromException<IEnumerable<NetworkMessageModel>>(e);
            }
        }

        /// <summary>
        /// Perform DataSetMessageModel to NetworkMessageModel Json batch encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeBatchAsJson(
            IEnumerable<DataSetMessageModel> messages) {

            var notifications = GetMonitoredItemMessages(messages, MessageEncoding.Json);
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
                    var helperEncoder = new JsonEncoderEx(helperWriter,
                        messages.First().ServiceMessageContext) {
                        UseAdvancedEncoding = true,
                        UseUriEncoding = true,
                        UseReversibleEncoding = false
                    };
                    notification.Encode(helperEncoder);
                    helperEncoder.Close();

                    var notificationSize = Encoding.UTF8.GetByteCount(helperWriter.ToString());
                    messageCompleted = MaxMessageBodySize < (messageSize + notificationSize);

                    if (!messageCompleted) {
                        chunk.Add(notification);
                        processing = current.MoveNext();
                        messageSize += notificationSize + (processing ? 1 : 0);
                    }
                }
                if (!processing || messageCompleted) {
                    var writer = new StringWriter();
                    var encoder = new JsonEncoderEx(writer,
                        messages.First().ServiceMessageContext,
                        JsonEncoderEx.JsonEncoding.Array) {
                        UseAdvancedEncoding = true,
                        UseUriEncoding = true,
                        UseReversibleEncoding = false
                    };
                    foreach (var element in chunk) {
                        encoder.WriteEncodeable(null, element);
                    }
                    encoder.Close();
                    messageSize = 2;  // array brackets
                    var encoded = new NetworkMessageModel {
                        Body = Encoding.UTF8.GetBytes(writer.ToString()),
                        ContentEncoding = "utf-8",
                        Timestamp = DateTime.UtcNow,
                        ContentType = ContentMimeType.UaJson,
                        // Todo, check what would make sense
                        // MessageId = message.SequenceNumber.ToString(),
                        MessageSchema = MessageSchemaTypes.MonitoredItemMessageJson
                    };
                    yield return encoded;
                }
            }
        }

        /// <summary>
        /// Perform DataSetMessageModel to NetworkMessageModel binary batch encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeBatchAsUadp(
            IEnumerable<DataSetMessageModel> messages) {
        
            var notifications = GetMonitoredItemMessages(messages, MessageEncoding.Uadp);
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
                    var helperEncoder = new BinaryEncoder(messages.First().ServiceMessageContext);
                    helperEncoder.WriteEncodeable(null, notification);
                    var notificationSize = helperEncoder.CloseAndReturnBuffer().Length;
                    messageCompleted = MaxMessageBodySize < (messageSize + notificationSize);
                    if (!messageCompleted) {
                        chunk.Add(notification);
                        processing = current.MoveNext();
                        messageSize += notificationSize;
                    }
                }
                if (!processing || messageCompleted) {
                    var encoder = new BinaryEncoder(messages.First().ServiceMessageContext);
                    encoder.WriteEncodeableArray(null, chunk);
                    chunk.Clear();
                    messageSize = 4;
                    var encoded = new NetworkMessageModel {
                        Body = encoder.CloseAndReturnBuffer(),
                        Timestamp = DateTime.UtcNow,
                        ContentType = ContentMimeType.UaBinary,
                        // Todo, check what would make sense
                        // MessageId = message.SequenceNumber.ToString(),
                        MessageSchema = MessageSchemaTypes.MonitoredItemMessageBinary
                    };
                    yield return encoded;
                }
            }
        }

        /// <summary>
        /// Perform event to message encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeAsJson(
            IEnumerable<DataSetMessageModel> messages) {

            var notifications = GetMonitoredItemMessages(messages, MessageEncoding.Json);
            if (notifications.Count() == 0) {
                yield break;
            }
            foreach (var networkMessage in notifications) {
                var writer = new StringWriter();
                var encoder = new JsonEncoderEx(writer,
                    messages.First().ServiceMessageContext) {
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
                    //MessageId = message.SequenceNumber,
                    MessageSchema = MessageSchemaTypes.MonitoredItemMessageJson
                };
                yield return encoded;
            }
        }

        /// <summary>
        /// Perform event to message encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeAsUadp(
            IEnumerable<DataSetMessageModel> messages) {

            var notifications = GetMonitoredItemMessages(messages, MessageEncoding.Uadp);
            if (notifications.Count() == 0) {
                yield break;
            }
            foreach (var networkMessage in notifications) {
                var encoder = new BinaryEncoder(messages.First().ServiceMessageContext);
                networkMessage.Encode(encoder);
                var encoded = new NetworkMessageModel {
                    Body = encoder.CloseAndReturnBuffer(),
                    Timestamp = DateTime.UtcNow,
                    ContentType = ContentMimeType.UaBinary,
                    //MessageId = message.SequenceNumber.ToString(),
                    MessageSchema = MessageSchemaTypes.MonitoredItemMessageBinary
                };
                yield return encoded;
            }
        }

        /// <summary>
        /// Produce Monitored Item Messages from the data set message model for the especified encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="encoding"></param>
        private IEnumerable<MonitoredItemMessage> GetMonitoredItemMessages(
            IEnumerable<DataSetMessageModel> messages, MessageEncoding encoding) {
            foreach (var message in messages) {
                if (message.WriterGroup?.MessageType.GetValueOrDefault(MessageEncoding.Json) == encoding) {
                    foreach (var notification in message.Notifications) {
                        yield return new MonitoredItemMessage {
                            MessageContentMask = (message.Writer?.MessageSettings?
                                .DataSetMessageContentMask).ToMonitoredItemMessageMask(
                                    message.Writer?.DataSetFieldContentMask),
                            ApplicationUri = message.ApplicationUri,
                            EndpointUrl = message.EndpointUrl,
                            ExtensionFields = message.Writer?.DataSet?.ExtensionFields,
                            NodeId = notification.NodeId.ToExpandedNodeId(message.ServiceMessageContext.NamespaceUris),
                            Timestamp = message.TimeStamp ?? DateTime.UtcNow,
                            Value = notification.Value,
                            DisplayName = notification.DisplayName
                        };
                    }
                }
            }
        }

        //  reserve 2K for header
        private readonly int MaxMessageBodySize = 254 * 1024;
    }
}

