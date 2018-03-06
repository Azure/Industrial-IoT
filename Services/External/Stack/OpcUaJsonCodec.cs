// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Codec {
    using Opc.Ua;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// Json based variant codec
    /// </summary>
    public class OpcUaJsonCodec : IOpcUaVariantCodec {

        /// <summary>
        /// Formats a variant as string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string Encode(Variant value) {
            if (value == Variant.Null) {
                return "null";
            }
            var encoder = new JsonEncoder(ServiceMessageContext.GlobalContext, true);
            encoder.WriteVariant(nameof(value), value);
            var json = encoder.CloseAndReturnText();
            var o = JToken.Parse(json).SelectToken("value.Body");
            return o.ToString(Formatting.None);
        }

        /// <summary>
        /// Parse variant value from string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="builtinType"></param>
        /// <returns></returns>
        public Variant Decode(string value, BuiltInType builtinType, int? valueRank) {
            if (string.IsNullOrEmpty(value) || value == "null") {
                return Variant.Null;
            }
            if (valueRank.HasValue && valueRank.Value != ValueRanks.Scalar) {
                value = $"[{value.Trim().Trim('[', ']')}]";
            }
            else if (builtinType == BuiltInType.String) {
                value = SanitizeString(value);
            }
            var token = JToken.Parse(value);
            var json = new JObject {
                { nameof(value), token }
            };
            var decoder = new JsonDecoder(json.ToString(), ServiceMessageContext.GlobalContext);
            if (token.Type == JTokenType.Array) {
                return ReadVariantArrayBody(decoder, nameof(value), builtinType);
            }
            return ReadVariantBody(decoder, nameof(value), builtinType);
        }

        /// <summary>
        /// Read variant body (from jsondecoder, where it is private)
        /// </summary>
        /// <param name="decoder"></param>
        /// <param name="fieldName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private Variant ReadVariantBody(JsonDecoder decoder, string fieldName,
            BuiltInType type) {
            switch (type) {
                case BuiltInType.Boolean:
                    return new Variant(decoder.ReadBoolean(fieldName),
                        TypeInfo.Scalars.Boolean);
                case BuiltInType.SByte:
                    return new Variant(decoder.ReadSByte(fieldName),
                        TypeInfo.Scalars.SByte);
                case BuiltInType.Byte:
                    return new Variant(decoder.ReadByte(fieldName),
                        TypeInfo.Scalars.Byte);
                case BuiltInType.Int16:
                    return new Variant(decoder.ReadInt16(fieldName),
                        TypeInfo.Scalars.Int16);
                case BuiltInType.UInt16:
                    return new Variant(decoder.ReadUInt16(fieldName),
                        TypeInfo.Scalars.UInt16);
                case BuiltInType.Int32:
                    return new Variant(decoder.ReadInt32(fieldName),
                        TypeInfo.Scalars.Int32);
                case BuiltInType.UInt32:
                    return new Variant(decoder.ReadUInt32(fieldName),
                        TypeInfo.Scalars.UInt32);
                case BuiltInType.Int64:
                    return new Variant(decoder.ReadInt64(fieldName),
                        TypeInfo.Scalars.Int64);
                case BuiltInType.UInt64:
                    return new Variant(decoder.ReadUInt64(fieldName),
                        TypeInfo.Scalars.UInt64);
                case BuiltInType.Float:
                    return new Variant(decoder.ReadFloat(fieldName),
                        TypeInfo.Scalars.Float);
                case BuiltInType.Double:
                    return new Variant(decoder.ReadDouble(fieldName),
                        TypeInfo.Scalars.Double);
                case BuiltInType.String:
                    return new Variant(decoder.ReadString(fieldName),
                        TypeInfo.Scalars.String);
                case BuiltInType.ByteString:
                    return new Variant(decoder.ReadByteString(fieldName),
                        TypeInfo.Scalars.ByteString);
                case BuiltInType.DateTime:
                    return new Variant(decoder.ReadDateTime(fieldName),
                        TypeInfo.Scalars.DateTime);
                case BuiltInType.Guid:
                    return new Variant(decoder.ReadGuid(fieldName),
                        TypeInfo.Scalars.Guid);
                case BuiltInType.NodeId:
                    return new Variant(decoder.ReadNodeId(fieldName),
                        TypeInfo.Scalars.NodeId);
                case BuiltInType.ExpandedNodeId:
                    return new Variant(decoder.ReadExpandedNodeId(fieldName),
                        TypeInfo.Scalars.ExpandedNodeId);
                case BuiltInType.QualifiedName:
                    return new Variant(decoder.ReadQualifiedName(fieldName),
                        TypeInfo.Scalars.QualifiedName);
                case BuiltInType.LocalizedText:
                    return new Variant(decoder.ReadLocalizedText(fieldName),
                        TypeInfo.Scalars.LocalizedText);
                case BuiltInType.StatusCode:
                    return new Variant(decoder.ReadStatusCode(fieldName),
                        TypeInfo.Scalars.StatusCode);
                case BuiltInType.XmlElement:
                    return new Variant(decoder.ReadXmlElement(fieldName),
                        TypeInfo.Scalars.XmlElement);
                case BuiltInType.ExtensionObject:
                    return new Variant(decoder.ReadExtensionObject(fieldName),
                        TypeInfo.Scalars.ExtensionObject);
                case BuiltInType.Variant:
                    return new Variant(decoder.ReadVariant(fieldName),
                        TypeInfo.Scalars.Variant);
            }
            return Variant.Null;
        }

        private Variant ReadVariantArrayBody(JsonDecoder decoder, string fieldName,
            BuiltInType type) {
            switch (type) {
                case BuiltInType.Boolean:
                    return new Variant(decoder.ReadBooleanArray(fieldName),
                        TypeInfo.Arrays.Boolean);
                case BuiltInType.SByte:
                    return new Variant(decoder.ReadSByteArray(fieldName),
                        TypeInfo.Arrays.SByte);
                case BuiltInType.Byte:
                    return new Variant(decoder.ReadByteArray(fieldName),
                        TypeInfo.Arrays.Byte);
                case BuiltInType.Int16:
                    return new Variant(decoder.ReadInt16Array(fieldName),
                        TypeInfo.Arrays.Int16);
                case BuiltInType.UInt16:
                    return new Variant(decoder.ReadUInt16Array(fieldName),
                        TypeInfo.Arrays.UInt16);
                case BuiltInType.Int32:
                    return new Variant(decoder.ReadInt32Array(fieldName),
                        TypeInfo.Arrays.Int32);
                case BuiltInType.UInt32:
                    return new Variant(decoder.ReadUInt32Array(fieldName),
                        TypeInfo.Arrays.UInt32);
                case BuiltInType.Int64:
                    return new Variant(decoder.ReadInt64Array(fieldName),
                        TypeInfo.Arrays.Int64);
                case BuiltInType.UInt64:
                    return new Variant(decoder.ReadUInt64Array(fieldName),
                        TypeInfo.Arrays.UInt64);
                case BuiltInType.Float:
                    return new Variant(decoder.ReadFloatArray(fieldName),
                        TypeInfo.Arrays.Float);
                case BuiltInType.Double:
                    return new Variant(decoder.ReadDoubleArray(fieldName),
                        TypeInfo.Arrays.Double);
                case BuiltInType.String:
                    return new Variant(decoder.ReadStringArray(fieldName),
                        TypeInfo.Arrays.String);
                case BuiltInType.ByteString:
                    return new Variant(decoder.ReadByteStringArray(fieldName),
                        TypeInfo.Arrays.ByteString);
                case BuiltInType.DateTime:
                    return new Variant(decoder.ReadDateTimeArray(fieldName),
                        TypeInfo.Arrays.DateTime);
                case BuiltInType.Guid:
                    return new Variant(decoder.ReadGuidArray(fieldName),
                        TypeInfo.Arrays.Guid);
                case BuiltInType.NodeId:
                    return new Variant(decoder.ReadNodeIdArray(fieldName),
                        TypeInfo.Arrays.NodeId);
                case BuiltInType.ExpandedNodeId:
                    return new Variant(decoder.ReadExpandedNodeIdArray(fieldName),
                        TypeInfo.Arrays.ExpandedNodeId);
                case BuiltInType.QualifiedName:
                    return new Variant(decoder.ReadQualifiedNameArray(fieldName),
                        TypeInfo.Arrays.QualifiedName);
                case BuiltInType.LocalizedText:
                    return new Variant(decoder.ReadLocalizedTextArray(fieldName),
                        TypeInfo.Arrays.LocalizedText);
                case BuiltInType.StatusCode:
                    return new Variant(decoder.ReadStatusCodeArray(fieldName),
                        TypeInfo.Arrays.StatusCode);
                case BuiltInType.XmlElement:
                    return new Variant(decoder.ReadXmlElementArray(fieldName),
                        TypeInfo.Arrays.XmlElement);
                case BuiltInType.ExtensionObject:
                    return new Variant(decoder.ReadExtensionObjectArray(fieldName),
                        TypeInfo.Arrays.ExtensionObject);
                case BuiltInType.Variant:
                    return new Variant(decoder.ReadVariantArray(fieldName),
                        TypeInfo.Arrays.Variant);
            }
            return Variant.Null;
        }

        private static string SanitizeString(string value) {
            if (!value.StartsWith("\"", System.StringComparison.Ordinal)) {
                value = "\"" + value;
            }
            if (!value.EndsWith("\"", System.StringComparison.Ordinal)) {
                value += "\"";
            }
            return value;
        }
    }
}
