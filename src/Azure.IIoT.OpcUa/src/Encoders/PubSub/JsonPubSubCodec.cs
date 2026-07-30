// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
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
    /// decoded field-by-field with the 2.0 stack codec. Raw event dictionaries
    /// are normalized to the historical artifact-free payload shape.
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
        /// <param name="useAdvancedEncoding"></param>
        /// <param name="namespaceFormat"></param>
        public static JsonNode? EncodeVariant(IServiceMessageContext context,
            Variant value, bool reversible, bool useAdvancedEncoding = false,
            NamespaceFormat namespaceFormat = NamespaceFormat.Uri)
        {
            var encoded = EncodeField(context, reversible
                ? JsonEncoderOptions.Compact : JsonEncoderOptions.Verbose,
                e => e.WriteVariant(kField, value));
            return EncodeLegacyVariant(context, value.TypeInfo.BuiltInType,
                ExtractVariantBody(encoded), reversible, useAdvancedEncoding,
                namespaceFormat);
        }

        /// <summary>
        /// Encode a variant using the non-reversible raw data encoding.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        /// <param name="useAdvancedEncoding"></param>
        /// <param name="namespaceFormat"></param>
        public static JsonNode? EncodeRawVariant(IServiceMessageContext context,
            Variant value, bool useAdvancedEncoding = false,
            NamespaceFormat namespaceFormat = NamespaceFormat.Uri)
        {
            var encoded = EncodeField(context, JsonEncoderOptions.RawData,
                e => e.WriteVariantValue(kField, value));
            return EncodeLegacyVariant(context, value.TypeInfo.BuiltInType,
                ExtractVariantBody(encoded), reversible: false,
                useAdvancedEncoding, namespaceFormat);
        }

        private static JsonNode? EncodeLegacyVariant(
            IServiceMessageContext context,
            BuiltInType type,
            JsonNode? body,
            bool reversible,
            bool useAdvancedEncoding,
            NamespaceFormat namespaceFormat)
        {
            body = NormalizeVariantBody(context, body, type, reversible,
                useAdvancedEncoding, namespaceFormat);
            if (!reversible)
            {
                return body;
            }
            return new JsonObject
            {
                ["Type"] = useAdvancedEncoding
                    ? JsonValue.Create(type.ToString())
                    : JsonValue.Create((byte)type),
                ["Body"] = body
            };
        }

        private static JsonNode? NormalizeVariantBody(
            IServiceMessageContext context,
            JsonNode? body,
            BuiltInType type,
            bool reversible,
            bool useAdvancedEncoding,
            NamespaceFormat namespaceFormat)
        {
            if (body is JsonArray array)
            {
                var normalized = new JsonArray();
                foreach (var item in array)
                {
                    normalized.Add(NormalizeVariantBody(context, item, type,
                        reversible, useAdvancedEncoding, namespaceFormat));
                }
                return normalized;
            }

            switch (type)
            {
                case BuiltInType.Int64:
                case BuiltInType.UInt64:
                    return useAdvancedEncoding
                        ? NumberizeWideInteger(body, type)
                        : body?.DeepClone();
                case BuiltInType.LocalizedText:
                    if (!reversible && body is JsonObject localizedText &&
                        localizedText.TryGetPropertyValue("Text", out var text))
                    {
                        return text?.DeepClone();
                    }
                    return body?.DeepClone();
                case BuiltInType.NodeId:
                case BuiltInType.ExpandedNodeId:
                    return NormalizeNodeId(context, body, reversible,
                        useAdvancedEncoding, namespaceFormat);
                case BuiltInType.QualifiedName:
                    return NormalizeQualifiedName(context, body, reversible,
                        useAdvancedEncoding, namespaceFormat);
                case BuiltInType.ExtensionObject:
                    return NormalizeExtensionObject(context, body, reversible,
                        useAdvancedEncoding, namespaceFormat);
                default:
                    return NormalizeStructure(context, body, reversible,
                        useAdvancedEncoding, namespaceFormat);
            }
        }

        private static JsonNode? NormalizeStructure(
            IServiceMessageContext context,
            JsonNode? node,
            bool reversible,
            bool useAdvancedEncoding,
            NamespaceFormat namespaceFormat)
        {
            if (node is JsonArray array)
            {
                var normalized = new JsonArray();
                foreach (var item in array)
                {
                    normalized.Add(NormalizeStructure(context, item, reversible,
                        useAdvancedEncoding, namespaceFormat));
                }
                return normalized;
            }
            if (node is not JsonObject obj)
            {
                return node?.DeepClone();
            }
            if (obj.TryGetPropertyValue("UaType", out var typeNode) &&
                obj.TryGetPropertyValue("Value", out var valueNode) &&
                TryGetBuiltInType(typeNode, out var type))
            {
                var value = EncodeLegacyVariant(context, type, valueNode,
                    reversible, useAdvancedEncoding, namespaceFormat);
                if (obj.Count == 2)
                {
                    return value;
                }
                var dataValue = new JsonObject
                {
                    ["Value"] = value
                };
                foreach (var property in obj)
                {
                    if (property.Key is not ("UaType" or "Value"))
                    {
                        dataValue[property.Key] = NormalizeStructure(
                            context, property.Value, reversible,
                            useAdvancedEncoding, namespaceFormat);
                    }
                }
                return dataValue;
            }
            if (obj.Count == 1 &&
                obj.TryGetPropertyValue("Value", out var rawValue))
            {
                return NormalizeStructure(context, rawValue, reversible,
                    useAdvancedEncoding, namespaceFormat);
            }
            if (!reversible &&
                obj.TryGetPropertyValue("Text", out var localizedText) &&
                obj.All(property => property.Key is "Text" or "Locale"))
            {
                return localizedText?.DeepClone();
            }
            var result = new JsonObject();
            foreach (var property in obj)
            {
                result[property.Key] = NormalizeStructure(
                    context, property.Value, reversible,
                    useAdvancedEncoding, namespaceFormat);
            }
            return result;
        }

        private static JsonNode? NormalizeExtensionObject(
            IServiceMessageContext context,
            JsonNode? body,
            bool reversible,
            bool useAdvancedEncoding,
            NamespaceFormat namespaceFormat)
        {
            if (body is not JsonObject extension)
            {
                return body?.DeepClone();
            }

            extension.TryGetPropertyValue("UaTypeId", out var typeIdNode);
            var normalizedBody = new JsonObject();
            foreach (var property in extension)
            {
                if (property.Key != "UaTypeId")
                {
                    normalizedBody[property.Key] = NormalizeStructure(
                        context, property.Value, reversible,
                        useAdvancedEncoding, namespaceFormat);
                }
            }
            if (!reversible)
            {
                return normalizedBody;
            }

            var result = new JsonObject();
            if (TryParseExpandedNodeId(typeIdNode, out var typeId))
            {
                result["TypeId"] = useAdvancedEncoding
                    ? JsonValue.Create(typeId.AsString(context, namespaceFormat))
                    : EncodeNodeIdObject(context, typeId, reversible: true);
            }
            result["Encoding"] = useAdvancedEncoding
                ? JsonValue.Create(nameof(ExtensionObjectEncoding.Json))
                : JsonValue.Create(0);
            result["Body"] = normalizedBody;
            return result;
        }

        internal static JsonNode? NormalizeNodeId(
            IServiceMessageContext context,
            JsonNode? body,
            bool reversible,
            bool useAdvancedEncoding,
            NamespaceFormat namespaceFormat)
        {
            if (!TryParseExpandedNodeId(body, out var nodeId))
            {
                return body?.DeepClone();
            }
            return useAdvancedEncoding
                ? JsonValue.Create(nodeId.AsString(context, namespaceFormat))
                : EncodeNodeIdObject(context, nodeId, reversible);
        }

        private static JsonNode? NormalizeQualifiedName(
            IServiceMessageContext context,
            JsonNode? body,
            bool reversible,
            bool useAdvancedEncoding,
            NamespaceFormat namespaceFormat)
        {
            if (body is not JsonValue value ||
                value.GetValueKind() != System.Text.Json.JsonValueKind.String)
            {
                return body?.DeepClone();
            }
            var qualifiedName = value.GetValue<string>().ToQualifiedName(context);
            if (reversible && useAdvancedEncoding)
            {
                return JsonValue.Create(
                    qualifiedName.AsString(context, namespaceFormat));
            }
            var result = new JsonObject
            {
                ["Name"] = qualifiedName.Name
            };
            if (qualifiedName.NamespaceIndex > 0)
            {
                result[reversible ? "Uri" : "Namespace"] =
                    qualifiedName.NamespaceIndex;
            }
            return result;
        }

        private static JsonObject EncodeNodeIdObject(
            IServiceMessageContext context,
            ExpandedNodeId nodeId,
            bool reversible)
        {
            var result = new JsonObject();
            if (nodeId.IdType != IdType.Numeric)
            {
                result["IdType"] = (byte)nodeId.IdType;
            }
            result["Id"] = nodeId.IdType switch
            {
                IdType.Numeric when nodeId.TryGetValue(out uint numeric) =>
                    JsonValue.Create(numeric),
                IdType.String when nodeId.TryGetValue(out string? text) =>
                    JsonValue.Create(text),
                IdType.Guid when nodeId.TryGetValue(out Guid guid) =>
                    JsonValue.Create(guid),
                IdType.Opaque when nodeId.TryGetValue(out ByteString opaque) =>
                    JsonValue.Create(Convert.ToBase64String(opaque.ToArray())),
                _ => JsonValue.Create(nodeId.IdentifierAsString)
            };

            var namespaceIndex = nodeId.NamespaceIndex;
            if (namespaceIndex == 0 && !string.IsNullOrEmpty(nodeId.NamespaceUri))
            {
                namespaceIndex = (ushort)context.NamespaceUris
                    .GetIndexOrAppend(nodeId.NamespaceUri);
            }
            if (namespaceIndex == 1)
            {
                result["Namespace"] = namespaceIndex;
            }
            else if (namespaceIndex > 1)
            {
                var namespaceUri = reversible
                    ? null
                    : context.NamespaceUris.GetString(namespaceIndex);
                result["Namespace"] = namespaceUri is null
                    ? JsonValue.Create(namespaceIndex)
                    : JsonValue.Create(namespaceUri);
            }
            if (nodeId.ServerIndex != 0)
            {
                result["ServerUri"] =
                    context.ServerUris.GetString(nodeId.ServerIndex) is { } serverUri
                        ? JsonValue.Create(serverUri)
                        : JsonValue.Create(nodeId.ServerIndex);
            }
            return result;
        }

        private static JsonNode? ExtractVariantBody(JsonNode? encoded)
        {
            return encoded is JsonObject obj &&
                obj.ContainsKey("UaType") &&
                obj.TryGetPropertyValue("Value", out var body)
                    ? body
                    : encoded;
        }

        private static bool TryParseExpandedNodeId(
            JsonNode? node, out ExpandedNodeId nodeId)
        {
            nodeId = ExpandedNodeId.Null;
            return node is JsonValue value &&
                value.GetValueKind() == System.Text.Json.JsonValueKind.String &&
                ExpandedNodeId.TryParse(value.GetValue<string>(), out nodeId);
        }

        private static bool TryGetBuiltInType(JsonNode? node, out BuiltInType type)
        {
            type = BuiltInType.Null;
            if (node is not JsonValue value)
            {
                return false;
            }
            if (value.TryGetValue<byte>(out var raw) &&
                Enum.IsDefined(typeof(BuiltInType), (int)raw))
            {
                type = (BuiltInType)raw;
                return type != BuiltInType.Null;
            }
            return value.GetValueKind() ==
                    System.Text.Json.JsonValueKind.String &&
                Enum.TryParse(value.GetValue<string>(), true, out type) &&
                type != BuiltInType.Null;
        }

        private static JsonNode? NumberizeWideInteger(JsonNode? node,
            BuiltInType type)
        {
            if (node is not JsonValue value ||
                value.GetValueKind() != System.Text.Json.JsonValueKind.String)
            {
                return node?.DeepClone();
            }
            var text = value.GetValue<string>();
            if (type == BuiltInType.UInt64 &&
                ulong.TryParse(text, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var unsignedValue))
            {
                return JsonValue.Create(unsignedValue);
            }
            if (type == BuiltInType.Int64 &&
                long.TryParse(text, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var signedValue))
            {
                return JsonValue.Create(signedValue);
            }
            return node.DeepClone();
        }

        /// <summary>
        /// Encode a data value to a json node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        /// <param name="reversible"></param>
        /// <param name="useAdvancedEncoding"></param>
        /// <param name="namespaceFormat"></param>
        public static JsonNode? EncodeDataValue(IServiceMessageContext context,
            in DataValue value, bool reversible, bool useAdvancedEncoding = false,
            NamespaceFormat namespaceFormat = NamespaceFormat.Uri)
        {
            return EncodeDataValue(context, value, reversible,
                useAdvancedEncoding, namespaceFormat, forceEnvelope: false);
        }

        private static JsonNode? EncodeDataValue(
            IServiceMessageContext context,
            in DataValue value,
            bool reversible,
            bool useAdvancedEncoding,
            NamespaceFormat namespaceFormat,
            bool forceEnvelope)
        {
            var dv = value;
            var encoded = EncodeField(context, reversible
                ? JsonEncoderOptions.Compact : JsonEncoderOptions.Verbose,
                e => e.WriteDataValue(kField, dv));
            var encodedObject = encoded as JsonObject;
            var body = encodedObject?["Value"];
            if (body is null && !value.WrappedValue.IsNull)
            {
                var wrapped = value.WrappedValue;
                body = EncodeField(context, JsonEncoderOptions.RawData,
                    e => e.WriteVariantValue(kField, wrapped));
            }
            var variant = EncodeLegacyVariant(
                context,
                value.WrappedValue.TypeInfo.BuiltInType,
                body,
                reversible,
                useAdvancedEncoding,
                namespaceFormat);
            var hasMetadata = encodedObject?.Any(property =>
                property.Key is not ("UaType" or "Value")) == true;
            if (!forceEnvelope && !hasMetadata)
            {
                return variant;
            }
            var result = new JsonObject
            {
                ["Value"] = variant
            };
            if (encodedObject != null)
            {
                foreach (var property in encodedObject)
                {
                    if (property.Key is not ("UaType" or "Value"))
                    {
                        result[property.Key] = property.Value?.DeepClone();
                    }
                }
            }
            return result;
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
            node = NormalizeVariantForDecoder(context, node);
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
            node = NormalizeDataValueForDecoder(context, node);
            using var decoder = DecoderFor(node, context);
            return decoder.ReadDataValue(kField);
        }

        private static JsonNode? NormalizeVariantForDecoder(
            IServiceMessageContext context, JsonNode? node)
        {
            if (node is JsonObject obj &&
                obj.TryGetPropertyValue("Type", out var typeNode) &&
                obj.TryGetPropertyValue("Body", out var body) &&
                TryGetBuiltInType(typeNode, out var type))
            {
                return new JsonObject
                {
                    ["UaType"] = (byte)type,
                    ["Value"] = NormalizeVariantBodyForDecoder(
                        context, body, type)
                };
            }
            if (node is JsonObject normalized && normalized.ContainsKey("UaType"))
            {
                return node.DeepClone();
            }
            return ApplyDefaultTyping(node);
        }

        private static JsonNode? NormalizeDataValueForDecoder(
            IServiceMessageContext context, JsonNode? node)
        {
            if (node is not JsonObject dataValue ||
                !dataValue.TryGetPropertyValue("Value", out var value))
            {
                return node?.DeepClone();
            }
            var variant = NormalizeVariantForDecoder(context, value);
            if (variant is not JsonObject variantObject ||
                !variantObject.TryGetPropertyValue("UaType", out var type) ||
                !variantObject.TryGetPropertyValue("Value", out var body))
            {
                return node.DeepClone();
            }

            var result = new JsonObject
            {
                ["UaType"] = type?.DeepClone(),
                ["Value"] = body?.DeepClone()
            };
            if (variantObject.TryGetPropertyValue("Dimensions", out var dimensions))
            {
                result["Dimensions"] = dimensions?.DeepClone();
            }
            foreach (var property in dataValue)
            {
                if (property.Key != "Value")
                {
                    result[property.Key] = property.Value?.DeepClone();
                }
            }
            return result;
        }

        private static JsonNode? NormalizeVariantBodyForDecoder(
            IServiceMessageContext context, JsonNode? body, BuiltInType type)
        {
            if (body is JsonArray array)
            {
                var normalized = new JsonArray();
                foreach (var item in array)
                {
                    normalized.Add(NormalizeVariantBodyForDecoder(
                        context, item, type));
                }
                return normalized;
            }
            return type switch
            {
                BuiltInType.NodeId => NormalizeNodeIdForDecoder(
                    context, body, expanded: false),
                BuiltInType.ExpandedNodeId => NormalizeNodeIdForDecoder(
                    context, body, expanded: true),
                BuiltInType.ExtensionObject => NormalizeExtensionObjectForDecoder(
                    context, body),
                _ => body?.DeepClone()
            };
        }

        internal static JsonNode? NormalizeNodeIdForDecoder(
            IServiceMessageContext context, JsonNode? body, bool expanded)
        {
            if (!TryReadLegacyNodeId(context, body, out var nodeId))
            {
                return body?.DeepClone();
            }
            if (!expanded)
            {
                var local = nodeId.ToNodeId(context.NamespaceUris);
                return JsonValue.Create(local.AsString(
                    context, NamespaceFormat.Expanded));
            }
            return JsonValue.Create(nodeId.AsString(
                context, NamespaceFormat.Expanded));
        }

        private static JsonNode? NormalizeExtensionObjectForDecoder(
            IServiceMessageContext context, JsonNode? body)
        {
            if (body is not JsonObject extension ||
                !extension.TryGetPropertyValue("TypeId", out var typeIdNode) ||
                !TryReadLegacyNodeId(context, typeIdNode, out var typeId))
            {
                return body?.DeepClone();
            }

            extension.TryGetPropertyValue("Encoding", out var encodingNode);
            extension.TryGetPropertyValue("Body", out var extensionBody);
            var encoding = GetExtensionObjectEncoding(encodingNode);
            var result = new JsonObject
            {
                ["UaTypeId"] = typeId.AsString(
                    context, NamespaceFormat.Expanded)
            };
            if (encoding == ExtensionObjectEncoding.Binary)
            {
                result["UaEncoding"] = (byte)ExtensionObjectEncoding.Binary;
                result["UaBody"] = extensionBody?.DeepClone();
            }
            else if (encoding == ExtensionObjectEncoding.Xml)
            {
                result["UaEncoding"] = (byte)ExtensionObjectEncoding.Xml;
                result["UaBody"] = extensionBody?.DeepClone();
            }
            else if (extensionBody is JsonObject structure)
            {
                foreach (var property in structure)
                {
                    result[property.Key] = property.Value?.DeepClone();
                }
            }
            return result;
        }

        private static ExtensionObjectEncoding GetExtensionObjectEncoding(
            JsonNode? node)
        {
            if (node is JsonValue value)
            {
                if (value.TryGetValue<int>(out var numeric) &&
                    Enum.IsDefined(typeof(ExtensionObjectEncoding), numeric))
                {
                    return (ExtensionObjectEncoding)numeric;
                }
                if (value.GetValueKind() == System.Text.Json.JsonValueKind.String &&
                    Enum.TryParse(value.GetValue<string>(), true,
                        out ExtensionObjectEncoding encoding))
                {
                    return encoding;
                }
            }
            return ExtensionObjectEncoding.None;
        }

        private static bool TryReadLegacyNodeId(
            IServiceMessageContext context, JsonNode? node,
            out ExpandedNodeId nodeId)
        {
            if (node is JsonValue value &&
                value.GetValueKind() == System.Text.Json.JsonValueKind.String)
            {
                nodeId = value.GetValue<string>().ToExpandedNodeId(context);
                return !nodeId.IsNull;
            }
            if (node is not JsonObject obj ||
                !obj.TryGetPropertyValue("Id", out var identifier))
            {
                nodeId = ExpandedNodeId.Null;
                return false;
            }

            var idType = IdType.Numeric;
            if (obj.TryGetPropertyValue("IdType", out var idTypeNode) &&
                idTypeNode is JsonValue idTypeValue &&
                idTypeValue.TryGetValue<byte>(out var rawIdType))
            {
                idType = (IdType)rawIdType;
            }
            object? id = idType switch
            {
                IdType.Numeric when identifier is JsonValue numeric &&
                    numeric.TryGetValue<uint>(out var numericId) => numericId,
                IdType.String when identifier is JsonValue text =>
                    text.GetValue<string>(),
                IdType.Guid when identifier is JsonValue guid &&
                    Guid.TryParse(guid.GetValue<string>(), out var guidId) => guidId,
                IdType.Opaque when identifier is JsonValue opaque =>
                    ByteString.FromBase64(opaque.GetValue<string>()),
                _ => null
            };
            if (id is null)
            {
                nodeId = ExpandedNodeId.Null;
                return false;
            }

            ushort namespaceIndex = 0;
            string? namespaceUri = null;
            if (obj.TryGetPropertyValue("Namespace", out var ns) &&
                ns is JsonValue namespaceValue)
            {
                if (!namespaceValue.TryGetValue<ushort>(out namespaceIndex) &&
                    namespaceValue.GetValueKind() ==
                        System.Text.Json.JsonValueKind.String)
                {
                    namespaceUri = namespaceValue.GetValue<string>();
                }
            }
            uint serverIndex = 0;
            if (obj.TryGetPropertyValue("ServerUri", out var server) &&
                server is JsonValue serverValue)
            {
                if (!serverValue.TryGetValue<uint>(out serverIndex) &&
                    serverValue.GetValueKind() ==
                        System.Text.Json.JsonValueKind.String)
                {
                    serverIndex = context.ServerUris.GetIndexOrAppend(
                        serverValue.GetValue<string>());
                }
            }
            nodeId = new ExpandedNodeId(
                id, namespaceIndex, namespaceUri, serverIndex);
            return true;
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
        /// <param name="useAdvancedEncoding"></param>
        /// <param name="namespaceFormat"></param>
        public static JsonNode? EncodeDataSet(IServiceMessageContext context,
            DataSet dataSet, bool dataValueReversible,
            bool useAdvancedEncoding = false,
            NamespaceFormat namespaceFormat = NamespaceFormat.Uri)
        {
            var fieldContentMask = dataSet.DataSetFieldContentMask;
            var writeSingleValue = dataSet.DataSetFields.Count == 1 &&
                fieldContentMask.HasFlag(DataSetFieldContentFlags.SingleFieldDegradeToValue);

            Func<DataValue?, JsonNode?> encodeField;
            if (fieldContentMask.HasFlag(DataSetFieldContentFlags.RawData))
            {
                // Non reversible variant (raw data) encoding
                encodeField = v => EncodeRawVariant(
                    context, v?.WrappedValue ?? Variant.Null,
                    useAdvancedEncoding, namespaceFormat);
            }
            else if (fieldContentMask == 0)
            {
                // Reversible variant encoding
                encodeField = v => EncodeVariant(
                    context, v?.WrappedValue ?? Variant.Null, true,
                    useAdvancedEncoding, namespaceFormat);
            }
            else
            {
                // DataValue encoding
                encodeField = v => EncodeMaskedDataValue(context, v, fieldContentMask,
                    dataValueReversible, useAdvancedEncoding, namespaceFormat);
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
                DataSetFieldContentFlags fieldContentMask = 0;
                foreach (var (name, value) in obj)
                {
                    fields.Add((name, DecodeField(context, value)));
                    fieldContentMask |= GetFieldContentMask(value);
                }
                return new DataSet(fields, fieldContentMask);
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
        /// <remarks>
        /// Part 6 §5.4.2.18 Table 42 names the status member <c>Status</c>, and
        /// the stack encoder was corrected to match. Messages produced before
        /// that correction spell it <c>StatusCode</c>, so both are recognised
        /// here: decoding tolerantly costs nothing and keeps historic payloads
        /// readable.
        /// </remarks>
        /// <param name="context"></param>
        /// <param name="value"></param>
        internal static DataValue DecodeField(IServiceMessageContext context,
            JsonNode? value)
        {
            if (value is JsonObject o &&
                (o.ContainsKey("Value") ||
                    o.ContainsKey("Status") ||
                    o.ContainsKey("StatusCode") ||
                    o.ContainsKey("SourceTimestamp") ||
                    o.ContainsKey("ServerTimestamp")))
            {
                return DecodeDataValue(context, value);
            }
            return new DataValue(DecodeVariant(context, value));
        }

        private static DataSetFieldContentFlags GetFieldContentMask(JsonNode? value)
        {
            if (value is not JsonObject obj)
            {
                return 0;
            }
            DataSetFieldContentFlags mask = 0;
            if (obj.ContainsKey("Status") || obj.ContainsKey("StatusCode"))
            {
                mask |= DataSetFieldContentFlags.StatusCode;
            }
            if (obj.ContainsKey("SourceTimestamp"))
            {
                mask |= DataSetFieldContentFlags.SourceTimestamp;
            }
            if (obj.ContainsKey("SourcePicoseconds"))
            {
                mask |= DataSetFieldContentFlags.SourcePicoSeconds;
            }
            if (obj.ContainsKey("ServerTimestamp"))
            {
                mask |= DataSetFieldContentFlags.ServerTimestamp;
            }
            if (obj.ContainsKey("ServerPicoseconds"))
            {
                mask |= DataSetFieldContentFlags.ServerPicoSeconds;
            }
            return mask;
        }

        /// <summary>
        /// Construct a data value carrying only the masked components and encode
        /// it.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        /// <param name="fieldContentMask"></param>
        /// <param name="reversible"></param>
        /// <param name="useAdvancedEncoding"></param>
        /// <param name="namespaceFormat"></param>
        private static JsonNode? EncodeMaskedDataValue(IServiceMessageContext context,
            DataValue? value, DataSetFieldContentFlags fieldContentMask,
            bool reversible, bool useAdvancedEncoding,
            NamespaceFormat namespaceFormat)
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
            return EncodeDataValue(context, dv, reversible,
                useAdvancedEncoding, namespaceFormat, forceEnvelope: true);
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

        /// <summary>
        /// Default type a bare value (one written without a { UaType, Value }
        /// envelope, as happens in the raw data encoding) so that the strict
        /// 2.0 decoder can decode it. Objects (already carrying an envelope)
        /// are returned unchanged. Mirrors the value api behavior of promoting
        /// integral numbers to Int64 and real numbers to Double.
        /// </summary>
        /// <param name="value"></param>
        private static JsonNode? ApplyDefaultTyping(JsonNode? value)
        {
            switch (value)
            {
                case JsonArray array:
                    if (array.Count == 0)
                    {
                        return null;
                    }
                    if (!TryDefaultElementType(array[0], out var elementType))
                    {
                        return value;
                    }
                    return new JsonObject
                    {
                        ["UaType"] = (byte)elementType,
                        ["Value"] = CoerceForWire(array.DeepClone(), elementType)
                    };
                case JsonValue jsonValue:
                    if (!TryDefaultElementType(jsonValue, out var scalarType))
                    {
                        return value;
                    }
                    return new JsonObject
                    {
                        ["UaType"] = (byte)scalarType,
                        ["Value"] = CoerceForWire(jsonValue.DeepClone(), scalarType)
                    };
                default:
                    return value;
            }
        }

        /// <summary>
        /// Determine the default built in type for a bare json value.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="builtInType"></param>
        private static bool TryDefaultElementType(JsonNode? node,
            out BuiltInType builtInType)
        {
            builtInType = BuiltInType.Null;
            if (node is not JsonValue value)
            {
                return false;
            }
            switch (value.GetValueKind())
            {
                case System.Text.Json.JsonValueKind.True:
                case System.Text.Json.JsonValueKind.False:
                    builtInType = BuiltInType.Boolean;
                    return true;
                case System.Text.Json.JsonValueKind.String:
                    builtInType = BuiltInType.String;
                    return true;
                case System.Text.Json.JsonValueKind.Number:
                    builtInType = value.TryGetValue<long>(out _)
                        ? BuiltInType.Int64
                        : BuiltInType.Double;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Coerce 64 bit integers into their on the wire string form which is
        /// what the OPC UA JSON encoding (and hence the 2.0 decoder) expects.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        private static JsonNode? CoerceForWire(JsonNode? value, BuiltInType type)
        {
            if (value is null ||
                type is not (BuiltInType.Int64 or BuiltInType.UInt64))
            {
                return value;
            }
            if (value is JsonArray array)
            {
                return new JsonArray(System.Linq.Enumerable.ToArray(
                    System.Linq.Enumerable.Select(array, StringifyNumber)));
            }
            return StringifyNumber(value);
        }

        /// <summary>
        /// Represent a numeric json value as its string form.
        /// </summary>
        /// <param name="node"></param>
        private static JsonNode? StringifyNumber(JsonNode? node)
        {
            if (node is JsonValue value &&
                value.GetValueKind() == System.Text.Json.JsonValueKind.Number)
            {
                return JsonValue.Create(value.ToJsonString());
            }
            return node?.DeepClone();
        }

        private const string kField = "f";
    }
}
