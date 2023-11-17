﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Encoders.Utils;
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
        /// Writer group id
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
        public DataValue? Value => Payload.Values.SingleOrDefault();

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
            if (!(obj is MonitoredItemMessage wrapper))
            {
                return false;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (!Utils.IsEqual(wrapper.EndpointUrl, EndpointUrl) ||
                !Utils.IsEqual(wrapper.ApplicationUri, ApplicationUri) ||
                !Utils.IsEqual(wrapper.NodeId, NodeId))
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
        internal override void Encode(JsonEncoderEx encoder, string? publisherId, bool withHeader, string? property)
        {
            //
            // If not writing with samples header or writing to a property we fail. This is a
            // configuration error, rather than throwing constantly we just do not emit anything instead.
            //
            if (!withHeader || property != null)
            {
                return;
            }

            if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMaskEx.NodeId) != 0)
            {
                encoder.WriteString(nameof(NodeId), NodeId);
            }
            if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMaskEx.EndpointUrl) != 0)
            {
                encoder.WriteString(nameof(EndpointUrl), EndpointUrl);
            }
            if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMaskEx.ApplicationUri) != 0)
            {
                encoder.WriteString(nameof(ApplicationUri), ApplicationUri);
            }
            if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMaskEx.DisplayName) != 0 &&
                !string.IsNullOrEmpty(DisplayName))
            {
                encoder.WriteString(nameof(DisplayName), DisplayName);
            }
            if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.Timestamp) != 0)
            {
                encoder.WriteDateTime(nameof(Timestamp), Timestamp ?? default);
            }
            if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.Status) != 0 && Value != null)
            {
                var status = Status ?? Payload.Values
                    .FirstOrDefault(s => StatusCode.IsNotGood(s?.StatusCode ?? StatusCodes.BadNoData))?
                        .StatusCode ?? StatusCodes.Good;
                encoder.WriteString(nameof(Status), StatusCode.LookupSymbolicId(status.Code));
            }

            var value = new DataValue(Value?.WrappedValue ?? Variant.Null);
            if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.Status) != 0 ||
                (Payload.DataSetFieldContentMask & (uint)DataSetFieldContentMask.StatusCode) != 0)
            {
                value.StatusCode = Value?.StatusCode ?? StatusCodes.BadNoData;
            }

            if ((Payload.DataSetFieldContentMask & (uint)DataSetFieldContentMask.SourceTimestamp) != 0)
            {
                value.SourceTimestamp = Value?.SourceTimestamp ?? DateTime.MinValue;
                if ((Payload.DataSetFieldContentMask & (uint)DataSetFieldContentMask.SourcePicoSeconds) != 0)
                {
                    value.SourcePicoseconds = Value?.SourcePicoseconds ?? 0;
                }
            }
            if ((Payload.DataSetFieldContentMask & (uint)DataSetFieldContentMask.ServerTimestamp) != 0)
            {
                value.ServerTimestamp = Value?.ServerTimestamp ?? DateTime.MinValue;
                if ((Payload.DataSetFieldContentMask & (uint)DataSetFieldContentMask.ServerPicoSeconds) != 0)
                {
                    value.ServerPicoseconds = Value?.ServerPicoseconds ?? 0;
                }
            }

            // force published timestamp into to source timestamp for the legacy heartbeat compatibility
            if (MessageType == MessageType.KeepAlive &&
                ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.Timestamp) == 0) &&
                ((Payload.DataSetFieldContentMask & (uint)DataSetFieldContentMask.SourceTimestamp) != 0))
            {
                value.SourceTimestamp = Timestamp ?? default;
            }

            var reversibleMode = encoder.UseReversibleEncoding;
            try
            {
                encoder.UseReversibleEncoding =
                    (DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask2.ReversibleFieldEncoding) != 0;
                encoder.WriteDataValue(nameof(Value), value);
            }
            finally
            {
                encoder.UseReversibleEncoding = reversibleMode;
            }

            if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.SequenceNumber) != 0)
            {
                encoder.WriteUInt32(nameof(SequenceNumber), SequenceNumber);
            }
            if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMaskEx.ExtensionFields) != 0)
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
                        .Where(e => e.Key != nameof(DataSetWriterId) &&
                                    e.Key != nameof(JsonNetworkMessage.PublisherId))
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
            var dataSetFieldContentMask = 0u;
            if (value != null)
            {
                if (value.ServerTimestamp != DateTime.MinValue)
                {
                    dataSetFieldContentMask |= (uint)DataSetFieldContentMask.ServerTimestamp;
                }
                if (value.ServerPicoseconds != 0)
                {
                    dataSetFieldContentMask |= (uint)DataSetFieldContentMask.ServerPicoSeconds;
                }
                if (value.SourceTimestamp != DateTime.MinValue)
                {
                    dataSetFieldContentMask |= (uint)DataSetFieldContentMask.SourceTimestamp;
                }
                if (value.SourcePicoseconds != 0)
                {
                    dataSetFieldContentMask |= (uint)DataSetFieldContentMask.SourcePicoSeconds;
                }
                if (value.StatusCode != 0)
                {
                    dataSetFieldContentMask |= (uint)DataSetFieldContentMask.StatusCode;
                }
            }

            // Read header
            DataSetMessageContentMask = 0u;
            var displayName = decoder.ReadString(nameof(DisplayName));
            if (displayName != null)
            {
                DataSetMessageContentMask |= (uint)JsonDataSetMessageContentMaskEx.DisplayName;
            }
            NodeId = decoder.ReadString(nameof(NodeId));
            if (NodeId != null)
            {
                DataSetMessageContentMask |= (uint)JsonDataSetMessageContentMaskEx.NodeId;
            }
            EndpointUrl = decoder.ReadString(nameof(EndpointUrl));
            if (EndpointUrl != null)
            {
                DataSetMessageContentMask |= (uint)JsonDataSetMessageContentMaskEx.EndpointUrl;
            }
            ApplicationUri = decoder.ReadString(nameof(ApplicationUri));
            if (ApplicationUri != null)
            {
                DataSetMessageContentMask |= (uint)JsonDataSetMessageContentMaskEx.ApplicationUri;
            }
            Timestamp = decoder.ReadDateTime(nameof(Timestamp));
            if (Timestamp != DateTime.MinValue)
            {
                DataSetMessageContentMask |= (uint)JsonDataSetMessageContentMask.Timestamp;
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
                    Status = status == "Good" ? StatusCodes.Good : StatusCodes.Bad;
                }
            }
            SequenceNumber = decoder.ReadUInt32(nameof(SequenceNumber));
            if (SequenceNumber != 0)
            {
                DataSetMessageContentMask |= (uint)JsonDataSetMessageContentMask.SequenceNumber;
            }
            var extensionFields = decoder.ReadStringDictionary(nameof(ExtensionFields));
            if (extensionFields != null)
            {
                DataSetMessageContentMask |= (uint)JsonDataSetMessageContentMaskEx.ExtensionFields;
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
