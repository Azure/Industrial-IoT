// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
        /// Endpoint url
        /// </summary>
        public string? EndpointUrl { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        public string? ApplicationUri { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string? DisplayName => Payload.Keys.SingleOrDefault();

        /// <summary>
        /// Data value for variable change notification
        /// </summary>
        public Opc.Ua.DataValue? Value => Payload.Values.SingleOrDefault();

        /// <summary>
        /// Extension fields
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public IDictionary<string, VariantValue>? ExtensionFields { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

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
            if (!Opc.Ua.Utils.IsEqual(wrapper.EndpointUrl, EndpointUrl) ||
                !Opc.Ua.Utils.IsEqual(wrapper.ApplicationUri, ApplicationUri) ||
                !Opc.Ua.Utils.IsEqual(wrapper.NodeId, NodeId))
            {
                return false;
            }
            if (!wrapper.ExtensionFields.DictionaryEqualsSafe(ExtensionFields,
                (a, b) => a.Equals(b)))
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

            hash.Add(EndpointUrl);
            hash.Add(ApplicationUri);
            hash.Add(NodeId);
            hash.Add(ExtensionFields);
            return hash.ToHashCode();
        }

        /// <inheritdoc/>
        internal override void Encode(JsonEncoderEx encoder, string? publisherId, bool withHeader,
            string? property)
        {
            //
            // If not writing with samples header or writing to a property we fail. This is a
            // configuration error, rather than throwing constantly we just do not emit anything
            // instead.
            //
            if (!withHeader || property != null)
            {
                return;
            }

            if (Payload.DataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.NodeId))
            {
                encoder.WriteString(nameof(NodeId), NodeId);
            }
            if (Payload.DataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.EndpointUrl))
            {
                encoder.WriteString(nameof(EndpointUrl), EndpointUrl);
            }
            if (Payload.DataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.ApplicationUri))
            {
                encoder.WriteString(nameof(ApplicationUri), ApplicationUri);
            }
            if (Payload.DataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.DisplayName) &&
                !string.IsNullOrEmpty(DisplayName))
            {
                encoder.WriteString(nameof(DisplayName), DisplayName);
            }
            if (DataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.Timestamp))
            {
                encoder.WriteDateTime(nameof(Timestamp), Timestamp?.UtcDateTime ?? default);
            }

            var valuePayload = Value;
            if (DataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.Status))
            {
                var status = Status;
                if (status == null)
                {
                    status = valuePayload != null ?
                        Opc.Ua.StatusCode.IsNotGood(valuePayload.StatusCode) ?
                            valuePayload.StatusCode : Opc.Ua.StatusCodes.Good :
                       (Opc.Ua.StatusCode?)Opc.Ua.StatusCodes.BadNoData;
                }
                encoder.WriteString(nameof(Status), status.Value.AsString());
            }

            // Create a copy of the data value and update the timestamps and status
            var value = new Opc.Ua.DataValue(valuePayload?.WrappedValue ?? Opc.Ua.Variant.Null);
            if (DataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.Status) ||
                Payload.DataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.StatusCode))
            {
                value.StatusCode = valuePayload?.StatusCode ?? Opc.Ua.StatusCodes.BadNoData;
            }

            if (Payload.DataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.SourceTimestamp))
            {
                value.SourceTimestamp = valuePayload?.SourceTimestamp ?? DateTime.MinValue;
                if (Payload.DataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.SourcePicoSeconds))
                {
                    value.SourcePicoseconds = valuePayload?.SourcePicoseconds ?? 0;
                }
            }
            if (Payload.DataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.ServerTimestamp))
            {
                value.ServerTimestamp = valuePayload?.ServerTimestamp ?? DateTime.MinValue;
                if (Payload.DataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.ServerPicoSeconds))
                {
                    value.ServerPicoseconds = valuePayload?.ServerPicoseconds ?? 0;
                }
            }

            var reversibleMode = encoder.UseReversibleEncoding;
            try
            {
                encoder.UseReversibleEncoding =
                    DataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.ReversibleFieldEncoding);
                encoder.WriteDataValue(nameof(Value), value);
            }
            finally
            {
                encoder.UseReversibleEncoding = reversibleMode;
            }

            if (DataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.SequenceNumber))
            {
                encoder.WriteUInt32(nameof(SequenceNumber), SequenceNumber);
            }

            if (Payload.DataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.ExtensionFields))
            {
                var extensionFields = new KeyValuePair<string, string?>(nameof(DataSetWriterId), DataSetWriterName)
                    .YieldReturn();
                if (publisherId != null)
                {
                    extensionFields = extensionFields
                        .Append(new KeyValuePair<string, string?>(nameof(JsonNetworkMessage.PublisherId), publisherId));
                }
                if (WriterGroupId != null)
                {
                    extensionFields = extensionFields
                        .Append(new KeyValuePair<string, string?>(nameof(WriterGroupId), WriterGroupId));
                }
                if (ExtensionFields != null)
                {
                    extensionFields = extensionFields.Concat(ExtensionFields
                        .Where(e => e.Key is not (nameof(DataSetWriterId)) and
                                    not (nameof(JsonNetworkMessage.PublisherId)))
                        .Select(e => new KeyValuePair<string, string?>(e.Key, e.Value.Value?.ToString())));
                }
                encoder.WriteStringDictionary(nameof(ExtensionFields), extensionFields);
            }
        }

        /// <inheritdoc/>
        internal override bool TryDecode(JsonDecoderEx decoder, string? property, ref bool withHeader,
            ref string? publisherId)
        {
            // If reading from property return false as this means we are a standard dataset message
            if (property != null)
            {
                return false;
            }

            var value = decoder.ReadDataValue(nameof(Value));
            DataSetFieldContentFlags dataSetFieldContentMask = 0u;
            if (value != null)
            {
                if (value.ServerTimestamp != DateTime.MinValue)
                {
                    dataSetFieldContentMask |= DataSetFieldContentFlags.ServerTimestamp;
                }
                if (value.ServerPicoseconds != 0)
                {
                    dataSetFieldContentMask |= DataSetFieldContentFlags.ServerPicoSeconds;
                }
                if (value.SourceTimestamp != DateTime.MinValue)
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
            }

            // Read header
            DataSetMessageContentMask = 0u;
            var displayName = decoder.ReadString(nameof(DisplayName));
            if (displayName != null)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.DisplayName;
            }
            NodeId = decoder.ReadString(nameof(NodeId));
            if (NodeId != null)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.NodeId;
            }
            EndpointUrl = decoder.ReadString(nameof(EndpointUrl));
            if (EndpointUrl != null)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.EndpointUrl;
            }
            ApplicationUri = decoder.ReadString(nameof(ApplicationUri));
            if (ApplicationUri != null)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.ApplicationUri;
            }
            var timestamp = decoder.ReadDateTime(nameof(Timestamp));
            if (timestamp != DateTime.MinValue)
            {
                Timestamp = timestamp;
                DataSetMessageContentMask |= DataSetMessageContentFlags.Timestamp;
            }
            var status = decoder.ReadString(nameof(Status));
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
            SequenceNumber = decoder.ReadUInt32(nameof(SequenceNumber));
            if (SequenceNumber != 0)
            {
                DataSetMessageContentMask |= DataSetMessageContentFlags.SequenceNumber;
            }
            var extensionFields = decoder.ReadStringDictionary(nameof(ExtensionFields));
            if (extensionFields != null)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.ExtensionFields;
                ExtensionFields = new Dictionary<string, VariantValue>();
                foreach (var item in extensionFields)
                {
                    ExtensionFields.AddOrUpdate(item.Key, item.Value);
                }
                if (extensionFields.TryGetValue(nameof(DataSetWriterId), out var dataSetWriterName))
                {
                    DataSetWriterName = dataSetWriterName;
                }
                if (extensionFields.TryGetValue(nameof(WriterGroupId), out var writerGroupid))
                {
                    WriterGroupId = writerGroupid;
                }
                extensionFields.TryGetValue(nameof(JsonNetworkMessage.PublisherId), out publisherId);
            }

            withHeader |= DataSetMessageContentMask != 0;
            if (value != null || dataSetFieldContentMask != 0)
            {
                Payload.Clear();
                Payload.DataSetFieldContentMask = dataSetFieldContentMask;
                Payload.Add(displayName ?? string.Empty, value);

                return true;
            }
            // Only return true if we otherwise read a header value
            return withHeader;
        }
    }
}
