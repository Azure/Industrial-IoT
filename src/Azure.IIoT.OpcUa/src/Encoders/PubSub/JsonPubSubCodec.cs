// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Nodes;

    /// <summary>
    /// JSON PubSub value codec built on top of the UA-.NETStandard 2.0
    /// <see cref="Opc.Ua.JsonEncoder"/> / <see cref="Opc.Ua.JsonDecoder"/>.
    ///
    /// The fork specific <c>JsonEncoderEx</c>/<c>JsonDecoderEx</c> streaming
    /// codecs were removed in the 2.0 migration. Rather than re-forking them,
    /// the JSON PubSub network message envelope (OPC UA Part 14 §7.2.3) is now
    /// assembled with <see cref="System.Text.Json.Nodes"/> while every OPC UA
    /// typed field value (Variant / DataValue / IEncodeable) is encoded and
    /// decoded field-by-field with the 2.0 stack codec. The 2.0 codec output is
    /// accepted as-is (behavioral compatibility bar; not byte-for-byte with the
    /// old fork).
    /// </summary>
    internal static class JsonPubSubCodec
    {
        /// <summary>
        /// Encode a variant to a json node using the reversible (compact) or
        /// non-reversible (verbose) OPC UA JSON data encoding.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        /// <param name="reversible"></param>
        public static JsonNode? EncodeVariant(IServiceMessageContext context,
            Variant value, bool reversible)
        {
            return EncodeField(context, reversible
                ? JsonEncoderOptions.Compact : JsonEncoderOptions.Verbose,
                e => e.WriteVariant(kField, value));
        }

        /// <summary>
        /// Encode a variant using the non-reversible raw data encoding.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        public static JsonNode? EncodeRawVariant(IServiceMessageContext context,
            Variant value)
        {
            return EncodeField(context, JsonEncoderOptions.RawData,
                e => e.WriteVariantValue(kField, value));
        }

        /// <summary>
        /// Encode a data value to a json node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        /// <param name="reversible"></param>
        public static JsonNode? EncodeDataValue(IServiceMessageContext context,
            in DataValue value, bool reversible)
        {
            var dv = value;
            return EncodeField(context, reversible
                ? JsonEncoderOptions.Compact : JsonEncoderOptions.Verbose,
                e => e.WriteDataValue(kField, dv));
        }

        /// <summary>
        /// Encode an encodeable to a json node.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="value"></param>
        public static JsonNode? EncodeEncodeable<T>(IServiceMessageContext context,
            T value) where T : IEncodeable, new()
        {
            return EncodeField(context, JsonEncoderOptions.Compact,
                e => e.WriteEncodeable(kField, value));
        }

        /// <summary>
        /// Encode a date time to a json node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        public static JsonNode? EncodeDateTime(IServiceMessageContext context,
            DateTimeUtc value)
        {
            return EncodeField(context, JsonEncoderOptions.Verbose,
                e => e.WriteDateTime(kField, value));
        }

        /// <summary>
        /// Decode a variant from a json node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        public static Variant DecodeVariant(IServiceMessageContext context,
            JsonNode? node)
        {
            using var decoder = DecoderFor(node, context);
            return decoder.ReadVariant(kField);
        }

        /// <summary>
        /// Decode a data value from a json node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        public static DataValue DecodeDataValue(IServiceMessageContext context,
            JsonNode? node)
        {
            using var decoder = DecoderFor(node, context);
            return decoder.ReadDataValue(kField);
        }

        /// <summary>
        /// Decode an encodeable from a json node.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="node"></param>
        public static T DecodeEncodeable<T>(IServiceMessageContext context,
            JsonNode? node) where T : IEncodeable, new()
        {
            using var decoder = DecoderFor(node, context);
            return decoder.ReadEncodeable<T>(kField);
        }

        /// <summary>
        /// Encode a dataset payload (OPC UA Part 14 §7.2.5.4 Payload) into a
        /// json node honoring the dataset field content mask.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="dataSet"></param>
        /// <param name="dataValueReversible"></param>
        public static JsonNode? EncodeDataSet(IServiceMessageContext context,
            DataSet dataSet, bool dataValueReversible)
        {
            var fieldContentMask = dataSet.DataSetFieldContentMask;
            var writeSingleValue = dataSet.DataSetFields.Count == 1 &&
                fieldContentMask.HasFlag(DataSetFieldContentFlags.SingleFieldDegradeToValue);

            Func<DataValue?, JsonNode?> encodeField;
            if (fieldContentMask.HasFlag(DataSetFieldContentFlags.RawData))
            {
                // Non reversible variant (raw data) encoding
                encodeField = v => EncodeRawVariant(context, v?.WrappedValue ?? Variant.Null);
            }
            else if (fieldContentMask == 0)
            {
                // Reversible variant encoding
                encodeField = v => EncodeVariant(context, v?.WrappedValue ?? Variant.Null, true);
            }
            else
            {
                // DataValue encoding
                encodeField = v => EncodeMaskedDataValue(context, v, fieldContentMask,
                    dataValueReversible);
            }

            if (writeSingleValue)
            {
                return encodeField(dataSet.DataSetFields.Count == 0
                    ? null : dataSet.DataSetFields[0].Value);
            }

            var payload = new JsonObject();
            foreach (var (name, value) in dataSet.DataSetFields)
            {
                payload[name] = encodeField(value);
            }
            return payload;
        }

        /// <summary>
        /// Decode a dataset payload from a json node. Note that dataset equality
        /// is defined over field name and field value only, so the reconstructed
        /// status/timestamps of the individual data values do not affect
        /// round-tripping.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        public static DataSet DecodeDataSet(IServiceMessageContext context,
            JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                var fields = new List<(string, DataValue?)>();
                foreach (var (name, value) in obj)
                {
                    fields.Add((name, DecodeField(context, value)));
                }
                return new DataSet(fields, (DataSetFieldContentFlags)0);
            }
            // Single degraded value
            var variant = DecodeVariant(context, node);
            return new DataSet(new[] { (string.Empty, (DataValue?)new DataValue(variant)) },
                DataSetFieldContentFlags.SingleFieldDegradeToValue);
        }

        /// <summary>
        /// Decode a dataset field which may be encoded either as a bare variant
        /// or as a DataValue object.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        internal static DataValue DecodeField(IServiceMessageContext context,
            JsonNode? value)
        {
            if (value is JsonObject o && o.ContainsKey("Value"))
            {
                return DecodeDataValue(context, value);
            }
            return new DataValue(DecodeVariant(context, value));
        }

        /// <summary>
        /// Construct a data value carrying only the masked components and encode
        /// it.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        /// <param name="fieldContentMask"></param>
        /// <param name="reversible"></param>
        private static JsonNode? EncodeMaskedDataValue(IServiceMessageContext context,
            DataValue? value, DataSetFieldContentFlags fieldContentMask, bool reversible)
        {
            var wrapped = value?.WrappedValue ?? Variant.Null;
            var status = fieldContentMask.HasFlag(DataSetFieldContentFlags.StatusCode)
                ? value?.StatusCode ?? default : default;
            var source = fieldContentMask.HasFlag(DataSetFieldContentFlags.SourceTimestamp)
                ? value?.SourceTimestamp ?? default : default;
            var server = fieldContentMask.HasFlag(DataSetFieldContentFlags.ServerTimestamp)
                ? value?.ServerTimestamp ?? default : default;
            var dv = new DataValue(wrapped, status, source, server);
            if (fieldContentMask.HasFlag(DataSetFieldContentFlags.SourcePicoSeconds))
            {
                dv = dv.WithSourcePicoseconds(value?.SourcePicoseconds ?? 0);
            }
            if (fieldContentMask.HasFlag(DataSetFieldContentFlags.ServerPicoSeconds))
            {
                dv = dv.WithServerPicoseconds(value?.ServerPicoseconds ?? 0);
            }
            return EncodeDataValue(context, dv, reversible);
        }

        /// <summary>
        /// Encode a single top level field and return its value node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="write"></param>
        private static JsonNode? EncodeField(IServiceMessageContext context,
            JsonEncoderOptions options, Action<Opc.Ua.JsonEncoder> write)
        {
            string text;
            using (var encoder = new Opc.Ua.JsonEncoder(context, options))
            {
                write(encoder);
                text = encoder.CloseAndReturnText();
            }
            // DeepClone detaches the node from the parsed document so it can be
            // re-parented into the network message envelope.
            return JsonNode.Parse(text)?[kField]?.DeepClone();
        }

        /// <summary>
        /// Create a decoder positioned to read a single field named
        /// <see cref="kField"/> holding the provided node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="context"></param>
        private static Opc.Ua.JsonDecoder DecoderFor(JsonNode? node,
            IServiceMessageContext context)
        {
            var json = new JsonObject
            {
                [kField] = node?.DeepClone()
            }.ToJsonString();
            return new Opc.Ua.JsonDecoder(json, context);
        }

        private const string kField = "f";
    }
}
