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
            if (Heartbeat && Payload.DataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.Heartbeat))
            {
                encoder.WriteBoolean(nameof(Payload.DataSetFieldContentMask.Heartbeat), Heartbeat);
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
                            not (nameof(DataSetWriterId)) and
                            not (nameof(EndpointUrl)) and
                            not (nameof(ApplicationUri)) and
                            not (nameof(WriterGroupId)) and
                            not (nameof(JsonNetworkMessage.PublisherId)))
                        .Select(e => (e.DataSetFieldName, e.Value.Value?.ToString())));
                }

                // We already wrote application uri and endpoint uri, so do not write again
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
            Heartbeat = decoder.ReadBoolean(nameof(Payload.DataSetFieldContentMask.Heartbeat));
            if (Heartbeat)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.Heartbeat;
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
            var stringDictionary = decoder.ReadStringDictionary(nameof(ExtensionFields));
            if (stringDictionary?.Count > 0)
            {
                dataSetFieldContentMask |= DataSetFieldContentFlags.ExtensionFields;
                var extensionFields = new List<ExtensionFieldModel>();
                foreach (var (name, v) in stringDictionary)
                {
                    if (name == nameof(DataSetWriterId))
                    {
                        DataSetWriterName = v;
                    }
                    else if (name == nameof(JsonNetworkMessage.PublisherId))
                    {
                        publisherId = v;
                    }
                    else if (name == nameof(WriterGroupId))
                    {
                        WriterGroupId = v;
                    }
                    else
                    {
                        extensionFields.Add(new ExtensionFieldModel
                        {
                            DataSetFieldName = name,
                            Value = v
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
            if (value != null || dataSetFieldContentMask != 0)
            {
                Payload = Payload.Add(displayName ?? string.Empty, value, dataSetFieldContentMask);
                return true;
            }
            // Only return true if we otherwise read a header value
            return withHeader;
        }
    }
}
