// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using System.Linq;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Data set message
    /// </summary>
    public class JsonDataSetMessage : BaseDataSetMessage
    {
        /// <summary>
        /// Compatibility with 2.8 when encoding and decoding
        /// </summary>
        public bool UseCompatibilityMode { get; set; }

        /// <summary>
        /// Dataset writer name
        /// </summary>
        public string? DataSetWriterName { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is not JsonDataSetMessage wrapper)
            {
                return false;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (!Opc.Ua.Utils.IsEqual(wrapper.DataSetWriterName, DataSetWriterName))
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());

            hash.Add(DataSetWriterName);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Encode the dataset message into a json node. When <paramref name="withHeader"/>
        /// is set the returned node is an object carrying the DataSetMessage header
        /// fields plus a Payload property, otherwise the raw dataset payload node is
        /// returned. See OPC UA Part 14 §7.2.5.4.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="publisherId"></param>
        /// <param name="withHeader"></param>
        /// <param name="useAdvancedEncoding"></param>
        /// <param name="namespaceFormat"></param>
        internal virtual JsonNode? EncodeToNode(IServiceMessageContext context,
            string? publisherId, bool withHeader, bool useAdvancedEncoding,
            NamespaceFormat namespaceFormat)
        {
            var reversible = DataSetMessageContentMask.HasFlag(
                DataSetMessageContentFlags.ReversibleFieldEncoding);
            if (!withHeader)
            {
                return JsonPubSubCodec.EncodeDataSet(context, Payload, reversible,
                    useAdvancedEncoding, namespaceFormat);
            }

            var message = new JsonObject();
            if ((DataSetMessageContentMask & DataSetMessageContentFlags.DataSetWriterId) != 0)
            {
                if (!UseCompatibilityMode)
                {
                    message[nameof(DataSetWriterId)] = DataSetWriterId;
                }
                else
                {
                    // Up to version 2.8 we wrote the string id as id which is not per standard
                    message[nameof(DataSetWriterId)] = DataSetWriterName;
                }
            }
            if ((DataSetMessageContentMask & DataSetMessageContentFlags.SequenceNumber) != 0)
            {
                message[nameof(SequenceNumber)] = SequenceNumber;
            }
            if ((DataSetMessageContentMask & DataSetMessageContentFlags.MetaDataVersion) != 0 &&
                MetaDataVersion != null)
            {
                message[nameof(MetaDataVersion)] =
                    JsonPubSubCodec.EncodeEncodeable(context, MetaDataVersion);
            }
            if ((DataSetMessageContentMask & DataSetMessageContentFlags.Timestamp) != 0)
            {
                var timestamp = JsonPubSubCodec.EncodeDateTime(
                    context, Timestamp?.UtcDateTime ?? default);
                if (timestamp != null)
                {
                    message[nameof(Timestamp)] = timestamp;
                }
            }
            if ((DataSetMessageContentMask & DataSetMessageContentFlags.Status) != 0)
            {
                var status = Status ?? Payload.DataSetFields
                    .FirstOrDefault(s => Opc.Ua.StatusCode.IsNotGood(s.Value?.StatusCode ??
                        Opc.Ua.StatusCodes.BadNoData)).Value?.StatusCode ?? Opc.Ua.StatusCodes.Good;
                message[nameof(Status)] = status.Code;
            }
            if ((DataSetMessageContentMask & DataSetMessageContentFlags.MessageType) != 0)
            {
                var messageType = MessageType switch
                {
                    MessageType.KeyFrame => "ua-keyframe",
                    MessageType.Event => "ua-event",
                    MessageType.KeepAlive => "ua-keepalive",
                    MessageType.Condition => "ua-condition",
                    MessageType.DeltaFrame => "ua-deltaframe",
                    _ => null
                };
                if (messageType != null)
                {
                    message[nameof(MessageType)] = messageType;
                }
            }
            if (!UseCompatibilityMode &&
                (DataSetMessageContentMask & DataSetMessageContentFlags.DataSetWriterName) != 0)
            {
                message[nameof(DataSetWriterName)] = DataSetWriterName;
            }
            message[nameof(Payload)] = JsonPubSubCodec.EncodeDataSet(
                context, Payload, reversible, useAdvancedEncoding, namespaceFormat);
            return message;
        }

        /// <summary>
        /// Decode the dataset message from a json node. See OPC UA Part 14 §7.2.5.4.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="withHeader"></param>
        /// <param name="publisherId"></param>
        internal virtual bool TryDecodeFromNode(IServiceMessageContext context,
            JsonNode? node, ref bool withHeader, ref string? publisherId)
        {
            if (node is not JsonObject obj)
            {
                return false;
            }
            if (TryReadHeader(context, obj, out var dataSetMessageContentMask))
            {
                withHeader = true;
                DataSetMessageContentMask = dataSetMessageContentMask;
                Payload = JsonPubSubCodec.DecodeDataSet(context, obj[nameof(Payload)]);
                return true;
            }
            if (withHeader)
            {
                // Previously we found a header, not now, we fail here
                return false;
            }
            // Reset content and treat the whole object as the payload
            DataSetMessageContentMask = 0;
            MessageType = MessageType.KeyFrame;
            DataSetWriterId = 0;
            DataSetWriterName = null;
            SequenceNumber = 0;
            MetaDataVersion = null;
            Timestamp = DateTimeOffset.MinValue;
            Payload = JsonPubSubCodec.DecodeDataSet(context, obj);
            return true;
        }

        /// <summary>
        /// Read the dataset message header.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="obj"></param>
        /// <param name="dataSetMessageContentMask"></param>
        private bool TryReadHeader(IServiceMessageContext context, JsonObject obj,
            out DataSetMessageContentFlags dataSetMessageContentMask)
        {
            dataSetMessageContentMask = 0;
            if (!obj.ContainsKey(nameof(Payload)))
            {
                return false;
            }
            var json = obj.ToJsonString();
            using var decoder = new Opc.Ua.JsonDecoder(json, context);
            if (decoder.HasField(nameof(DataSetWriterId)))
            {
                // Up to version 2.8 we wrote the string id as id which is not
                // per standard. The strict 2.0 decoder throws instead of
                // returning a default when a numeric read hits a string, so
                // inspect the json value kind before reading.
                if (obj[nameof(DataSetWriterId)] is JsonValue writerId &&
                    writerId.GetValueKind() ==
                        System.Text.Json.JsonValueKind.String)
                {
                    DataSetWriterName = decoder.ReadString(nameof(DataSetWriterId));
                    if (DataSetWriterName != null)
                    {
                        UseCompatibilityMode = true;
                        dataSetMessageContentMask |= DataSetMessageContentFlags.DataSetWriterId;
                        dataSetMessageContentMask |= DataSetMessageContentFlags.DataSetWriterName;
                    }
                }
                else
                {
                    DataSetWriterId = decoder.ReadUInt16(nameof(DataSetWriterId));
                    dataSetMessageContentMask |= DataSetMessageContentFlags.DataSetWriterId;
                }
            }
            if (decoder.HasField(nameof(MetaDataVersion)))
            {
                MetaDataVersion = decoder.ReadEncodeable<Opc.Ua.ConfigurationVersionDataType>(
                    nameof(MetaDataVersion));
                dataSetMessageContentMask |= DataSetMessageContentFlags.MetaDataVersion;
            }
            if (decoder.HasField(nameof(SequenceNumber)))
            {
                SequenceNumber = decoder.ReadUInt32(nameof(SequenceNumber));
                dataSetMessageContentMask |= DataSetMessageContentFlags.SequenceNumber;
            }
            if (decoder.HasField(nameof(Timestamp)))
            {
                Timestamp = new DateTimeOffset(
                    decoder.ReadDateTime(nameof(Timestamp)).ToDateTime(), TimeSpan.Zero);
                dataSetMessageContentMask |= DataSetMessageContentFlags.Timestamp;
            }
            if (decoder.HasField(nameof(Status)))
            {
                UseCompatibilityMode = obj[nameof(Status)] is JsonObject;
                dataSetMessageContentMask |= DataSetMessageContentFlags.Status;
                if (UseCompatibilityMode)
                {
                    Status = decoder.ReadStatusCode(nameof(Status));
                }
                else
                {
                    Status = decoder.ReadUInt32(nameof(Status));
                }
            }
            if (decoder.HasField(nameof(MessageType)))
            {
                var messageType = decoder.ReadString(nameof(MessageType));
                dataSetMessageContentMask |= DataSetMessageContentFlags.MessageType;
                MessageType = messageType switch
                {
                    "ua-deltaframe" => MessageType.DeltaFrame,
                    "ua-event" => MessageType.Event,
                    "ua-keepalive" => MessageType.KeepAlive,
                    "ua-condition" => MessageType.Condition,
                    _ => MessageType.KeyFrame
                };
            }
            if (decoder.HasField(nameof(DataSetWriterName)))
            {
                DataSetWriterName = decoder.ReadString(nameof(DataSetWriterName));
                dataSetMessageContentMask |= DataSetMessageContentFlags.DataSetWriterName;
            }
            return true;
        }
    }
}
