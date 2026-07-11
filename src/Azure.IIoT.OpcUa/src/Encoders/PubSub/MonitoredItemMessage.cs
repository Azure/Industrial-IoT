// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Samples message
    /// </summary>
    public class MonitoredItemMessage : JsonDataSetMessage
    {
        /// <summary>
        /// Node Id in string format as configured
        /// </summary>
        public string? NodeId { get; set; }

        /// <summary>
        /// Writer group name (dont change then name for backcompat)
        /// </summary>
        public string? WriterGroupId { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string? DisplayName => Payload.DataSetFields.SingleOrDefault().Name;

        /// <summary>
        /// Data value for variable change notification
        /// </summary>
        public Opc.Ua.DataValue? Value => Payload.DataSetFields.SingleOrDefault().Value;

        /// <summary>
        /// Extension fields
        /// </summary>
        public IReadOnlyList<ExtensionFieldModel>? ExtensionFields { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is not MonitoredItemMessage wrapper)
            {
                return false;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (!Opc.Ua.Utils.IsEqual(wrapper.NodeId, NodeId))
            {
                return false;
            }
            if (!wrapper.ExtensionFields.SetEqualsSafe(ExtensionFields,
                (a, b) => a?.Equals(b) ?? b == null))
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

            hash.Add(NodeId);
            hash.Add(ExtensionFields);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Encode the samples (monitored item) message to a flat json object. See the
        /// legacy JSON samples layout. Only emitted when writing with a per-message
        /// header (samples cannot be written as bare payloads).
        /// </summary>
        /// <param name="context"></param>
        /// <param name="publisherId"></param>
        /// <param name="withHeader"></param>
        internal override JsonNode? EncodeToNode(IServiceMessageContext context,
            string? publisherId, bool withHeader)
        {
            //
            // If not writing with samples header we fail. This is a configuration error,
            // rather than throwing constantly we just do not emit anything instead.
            //
            if (!withHeader)
            {
                return null;
            }

            var samples = new JsonObject();
            var fieldContentMask = Payload.DataSetFieldContentMask;
            if (fieldContentMask.HasFlag(DataSetFieldContentFlags.NodeId))
            {
                samples[nameof(NodeId)] = NodeId;
            }
            if (fieldContentMask.HasFlag(DataSetFieldContentFlags.EndpointUrl))
            {
                samples[nameof(EndpointUrl)] = EndpointUrl;
            }
            if (fieldContentMask.HasFlag(DataSetFieldContentFlags.ApplicationUri))
            {
                samples[nameof(ApplicationUri)] = ApplicationUri;
            }
            if (fieldContentMask.HasFlag(DataSetFieldContentFlags.DisplayName) &&
                !string.IsNullOrEmpty(DisplayName))
            {
                samples[nameof(DisplayName)] = DisplayName;
            }
            if (DataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.Timestamp))
            {
                samples[nameof(Timestamp)] = JsonPubSubCodec.EncodeDateTime(context,
                    Timestamp?.UtcDateTime ?? default);
            }
            if (Heartbeat && fieldContentMask.HasFlag(DataSetFieldContentFlags.Heartbeat))
            {
                samples[nameof(DataSetFieldContentFlags.Heartbeat)] = Heartbeat;
            }
            var valuePayload = Value;
            if (DataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.Status))
            {
                var status = Status;
                status ??= valuePayload != null
                    ? Opc.Ua.StatusCode.IsNotGood(valuePayload.Value.StatusCode)
                        ? valuePayload.Value.StatusCode : Opc.Ua.StatusCodes.Good
                    : Opc.Ua.StatusCodes.BadNoData;
                samples[nameof(Status)] = status.Value.AsString();
            }

            // Create a copy of the data value carrying only the masked components
            var variant = valuePayload?.WrappedValue ?? Opc.Ua.Variant.Null;
            var statusCode = default(Opc.Ua.StatusCode);
            if (DataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.Status) ||
                fieldContentMask.HasFlag(DataSetFieldContentFlags.StatusCode))
            {
                statusCode = valuePayload?.StatusCode ?? Opc.Ua.StatusCodes.BadNoData;
            }
            var sourceTimestamp = default(DateTimeUtc);
            var serverTimestamp = default(DateTimeUtc);
            if (fieldContentMask.HasFlag(DataSetFieldContentFlags.SourceTimestamp))
            {
                sourceTimestamp = valuePayload?.SourceTimestamp ?? default;
            }
            if (fieldContentMask.HasFlag(DataSetFieldContentFlags.ServerTimestamp))
            {
                serverTimestamp = valuePayload?.ServerTimestamp ?? default;
            }
            var value = new Opc.Ua.DataValue(variant, statusCode, sourceTimestamp, serverTimestamp);
            if (fieldContentMask.HasFlag(DataSetFieldContentFlags.SourceTimestamp) &&
                fieldContentMask.HasFlag(DataSetFieldContentFlags.SourcePicoSeconds))
            {
                value = value.WithSourcePicoseconds(valuePayload?.SourcePicoseconds ?? 0);
            }
            if (fieldContentMask.HasFlag(DataSetFieldContentFlags.ServerTimestamp) &&
                fieldContentMask.HasFlag(DataSetFieldContentFlags.ServerPicoSeconds))
            {
                value = value.WithServerPicoseconds(valuePayload?.ServerPicoseconds ?? 0);
            }
            var reversible = DataSetMessageContentMask.HasFlag(
                DataSetMessageContentFlags.ReversibleFieldEncoding);
            samples[nameof(Value)] = JsonPubSubCodec.EncodeDataValue(context, value, reversible);

            if (DataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.SequenceNumber))
            {
                samples[nameof(SequenceNumber)] = SequenceNumber;
            }

            if (fieldContentMask.HasFlag(DataSetFieldContentFlags.ExtensionFields))
            {
                var extensionFields = (nameof(DataSetWriterId), DataSetWriterName)
                    .YieldReturn();
                if (publisherId != null)
                {
                    extensionFields = extensionFields
                        .Append((nameof(JsonNetworkMessage.PublisherId), publisherId));
                }
                if (WriterGroupId != null)
                {
                    extensionFields = extensionFields
                        .Append((nameof(WriterGroupId), WriterGroupId));
                }
                if (ExtensionFields != null)
                {
                    extensionFields = extensionFields.Concat(ExtensionFields
                        .Where(e => e.DataSetFieldName is
                            not nameof(DataSetWriterId) and
                            not nameof(EndpointUrl) and
                            not nameof(ApplicationUri) and
                            not nameof(WriterGroupId) and
                            not nameof(JsonNetworkMessage.PublisherId))
                        .Select(e => (e.DataSetFieldName, e.Value?.ToString())));
                }

                var dictionary = new JsonObject();
                foreach (var (name, v) in extensionFields)
                {
                    if (name != null)
                    {
                        dictionary[name] = v;
                    }
                }
                samples[nameof(ExtensionFields)] = dictionary;
            }
            return samples;
        }

        /// <summary>
        /// Decode the samples (monitored item) message from a flat json object.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="withHeader"></param>
        /// <param name="publisherId"></param>
        internal override bool TryDecodeFromNode(IServiceMessageContext context,
            JsonNode? node, ref bool withHeader, ref string? publisherId)
        {
            if (node is not JsonObject obj || !obj.ContainsKey(nameof(Value)))
            {
                return false;
            }

            var value = JsonPubSubCodec.DecodeDataValue(context, obj[nameof(Value)]);
            DataSetFieldContentFlags dataSetFieldContentMask = 0u;
            if (value.ServerTimestamp != default)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.ServerTimestamp;
            }
            if (value.ServerPicoseconds != 0)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.ServerPicoSeconds;
            }
            if (value.SourceTimestamp != default)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.SourceTimestamp;
            }
            if (value.SourcePicoseconds != 0)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.SourcePicoSeconds;
            }
            if (value.StatusCode != 0)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.StatusCode;
            }

            // Read header
            DataSetMessageContentMask = 0u;
            var displayName = obj[nameof(DisplayName)]?.GetValue<string>();
            if (displayName != null)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.DisplayName;
            }
            NodeId = obj[nameof(NodeId)]?.GetValue<string>();
            if (NodeId != null)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.NodeId;
            }
            EndpointUrl = obj[nameof(EndpointUrl)]?.GetValue<string>();
            if (EndpointUrl != null)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.EndpointUrl;
            }
            ApplicationUri = obj[nameof(ApplicationUri)]?.GetValue<string>();
            if (ApplicationUri != null)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.ApplicationUri;
            }
            if (obj.ContainsKey(nameof(Timestamp)))
            {
                using var decoder = CreateDecoder(obj, context);
                var ts = decoder.ReadDateTime(nameof(Timestamp));
                if (ts != default)
                {
                    Timestamp = new DateTimeOffset(ts.ToDateTime(), TimeSpan.Zero);
                    DataSetMessageContentMask |= DataSetMessageContentFlags.Timestamp;
                }
            }
            Heartbeat = obj[nameof(DataSetFieldContentFlags.Heartbeat)]?
                .GetValue<bool>() ?? false;
            if (Heartbeat)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.Heartbeat;
            }
            var status = obj[nameof(Status)]?.GetValue<string>();
            if (status != null)
            {
                if (TypeMaps.StatusCodes.Value.TryGetIdentifier(status, out var statusCode))
                {
                    Status = statusCode;
                }
                else
                {
                    Status = status == "Good" ? Opc.Ua.StatusCodes.Good : Opc.Ua.StatusCodes.Bad;
                }
            }
            SequenceNumber = obj[nameof(SequenceNumber)]?.GetValue<uint>() ?? 0;
            if (SequenceNumber != 0)
            {
                DataSetMessageContentMask |= DataSetMessageContentFlags.SequenceNumber;
            }
            if (obj[nameof(ExtensionFields)] is JsonObject stringDictionary &&
                stringDictionary.Count > 0)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.ExtensionFields;
                var extensionFields = new List<ExtensionFieldModel>();
                foreach (var (name, v) in stringDictionary)
                {
                    var text = v?.GetValue<string>();
                    if (name == nameof(DataSetWriterId))
                    {
                        DataSetWriterName = text;
                    }
                    else if (name == nameof(JsonNetworkMessage.PublisherId))
                    {
                        publisherId = text;
                    }
                    else if (name == nameof(WriterGroupId))
                    {
                        WriterGroupId = text;
                    }
                    else
                    {
                        extensionFields.Add(new ExtensionFieldModel
                        {
                            DataSetFieldName = name,
                            Value = text
                        });
                    }
                }
                ExtensionFields = extensionFields;
            }
            else
            {
                ExtensionFields = null;
            }

            withHeader |= DataSetMessageContentMask != 0;
            Payload = Payload.Add(displayName ?? string.Empty, value, dataSetFieldContentMask);
            return true;
        }

        /// <summary>
        /// Create a json decoder over the provided object.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="context"></param>
        private static Opc.Ua.JsonDecoder CreateDecoder(JsonObject obj,
            IServiceMessageContext context)
        {
            return new Opc.Ua.JsonDecoder(obj.ToJsonString(), context);
        }
    }
}
