// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Core.Utils;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Variant encoder implementation
    /// </summary>
    public sealed class JsonVariantEncoder : IVariantEncoder
    {
        /// <inheritdoc/>
        public IServiceMessageContext Context { get; }

        /// <summary>
        /// Create encoder
        /// </summary>
        /// <param name="context"></param>
        public JsonVariantEncoder(IServiceMessageContext context)
        {
            Context = context;
        }

        /// <inheritdoc/>
        public JsonNode? Encode(Variant? value, out BuiltInType builtinType,
            ValueEncoding encoding = ValueEncoding.Reversible)
        {
            if (value is null || value.Value.IsNull)
            {
                builtinType = BuiltInType.Null;
                return null;
            }
            var useReversibleEncoding = encoding != ValueEncoding.NonReversible;
            string text;
            if (useReversibleEncoding)
            {
                using var encoder = new Opc.Ua.JsonEncoder(
                    Context, Opc.Ua.JsonEncoderOptions.RawData);
                encoder.WriteVariantValue(nameof(value), value.Value);
                text = encoder.CloseAndReturnText();

                builtinType = value.Value.TypeInfo.BuiltInType;
                var rawToken = JsonNode.Parse(text);
                return NumberizeWideIntegers(
                    rawToken?["value"]?.DeepClone(), builtinType);
            }

            using (var encoder = new Opc.Ua.JsonEncoder(
                Context, Opc.Ua.JsonEncoderOptions.Verbose))
            {
                encoder.WriteVariant(nameof(value), value.Value);
                text = encoder.CloseAndReturnText();
            }

            //
            // The non-reversible encoding writes the value contents directly
            // without the Type/Body envelope, so derive the built in type
            // from the variant type information instead.
            //
            builtinType = value.Value.TypeInfo.BuiltInType;
            var token = JsonNode.Parse(text);
            return NumberizeWideIntegers(
                token?["value"]?.DeepClone(), builtinType);
        }

        /// <summary>
        /// The 2.0 OPC UA JSON encoding represents 64 bit integers as strings.
        /// The REST value api historically surfaced them as json numbers, so
        /// convert them back to numbers here to keep that contract (and to
        /// round-trip with <see cref="Decode"/> which accepts numeric input).
        /// </summary>
        /// <param name="node"></param>
        /// <param name="builtinType"></param>
        /// <returns></returns>
        private static JsonNode? NumberizeWideIntegers(JsonNode? node,
            BuiltInType builtinType)
        {
            if (node is null ||
                builtinType is not (BuiltInType.Int64 or BuiltInType.UInt64))
            {
                return node;
            }
            if (node is JsonArray array)
            {
                return new JsonArray(array
                    .Select(e => NumberizeWideInteger(e, builtinType)).ToArray());
            }
            return NumberizeWideInteger(node, builtinType);
        }

        /// <summary>
        /// Convert a single 64 bit integer value rendered as a json string
        /// back to a json number.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="builtinType"></param>
        /// <returns></returns>
        private static JsonNode? NumberizeWideInteger(JsonNode? node,
            BuiltInType builtinType)
        {
            if (node is not JsonValue value ||
                value.GetValueKind() != JsonValueKind.String)
            {
                return node?.DeepClone();
            }
            var text = value.GetValue<string>();
            if (builtinType == BuiltInType.UInt64)
            {
                if (ulong.TryParse(text, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var u))
                {
                    return JsonValue.Create(u);
                }
            }
            else if (long.TryParse(text, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var l))
            {
                return JsonValue.Create(l);
            }
            return node.DeepClone();
        }

        /// <inheritdoc/>
        public Variant Decode(JsonNode? value, BuiltInType builtinType)
        {
            if (value is null)
            {
                return Variant.Null;
            }

            //
            // Sanitize json input from user
            //
            value = Sanitize(value, builtinType == BuiltInType.String);

            //
            // Normalize a variant shaped object into the shape the 2.0
            // decoder understands. Accepts the historical envelope shapes
            // ({ Type, Body }, { DataType, Value }, case insensitive, type
            // as a name or number) and remaps them onto the 2.0
            // { UaType, Value } envelope.
            //
            if (value is JsonObject obj &&
                TryNormalizeVariantObject(obj, out var normalized))
            {
                value = normalized;
                builtinType = BuiltInType.Variant;
            }

            string json;
            if (builtinType is BuiltInType.Null or BuiltInType.Variant
                or BuiltInType.Integer or BuiltInType.UInteger
                or BuiltInType.Number or BuiltInType.Enumeration)
            {
                //
                // No concrete type hint - either the value already carries
                // its own { UaType, Value } envelope, or we default type a
                // bare value the same way the previous implementation did.
                //
                if (value is not JsonObject)
                {
                    value = ApplyDefaultTyping(value);
                }
                json = new JsonObject
                {
                    ["value"] = value?.DeepClone()
                }.ToJsonString();
            }
            else
            {
                //
                // Give decoder a hint as to the type to use to decode.
                //
                json = new JsonObject
                {
                    ["value"] = new JsonObject
                    {
                        ["Value"] = CoerceForWire(value?.DeepClone(), builtinType),
                        ["UaType"] = (byte)builtinType
                    }
                }.ToJsonString();
            }

            //
            // Decode json to a real variant
            //
            using var decoder = new Opc.Ua.JsonDecoder(json, Context);
            return decoder.ReadVariant(nameof(value));
        }

        /// <summary>
        /// Try to normalize a variant shaped object that carries its type
        /// alongside its value (e.g. { Type, Body } or { DataType, Value })
        /// into the 2.0 decoder's { UaType, Value } envelope. Returns false
        /// when the object is not a recognizable variant envelope so that
        /// the caller leaves it untouched.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="normalized"></param>
        /// <returns></returns>
        private static bool TryNormalizeVariantObject(JsonObject obj,
            out JsonObject? normalized)
        {
            normalized = null;
            JsonNode? typeNode = null;
            JsonNode? valueNode = null;
            var hasValue = false;
            var alreadyNormalized = false;
            foreach (var (key, node) in obj)
            {
                switch (key.ToUpperInvariant())
                {
                    case "UATYPE":
                        typeNode = node;
                        alreadyNormalized = true;
                        break;
                    case "TYPE":
                    case "DATATYPE":
                        typeNode = node;
                        break;
                    case "VALUE":
                    case "BODY":
                        valueNode = node;
                        hasValue = true;
                        break;
                }
            }
            if (typeNode is null || !hasValue || alreadyNormalized)
            {
                //
                // Not a legacy envelope (or already in 2.0 shape) - leave as
                // is and let the decoder deal with it.
                //
                return false;
            }
            if (!TryResolveBuiltInType(typeNode, out var uaType))
            {
                return false;
            }
            normalized = new JsonObject
            {
                ["UaType"] = (byte)uaType,
                ["Value"] = CoerceForWire(valueNode?.DeepClone(), uaType)
            };
            return true;
        }

        /// <summary>
        /// Resolve a built in type from either its numeric value or its
        /// (case insensitive) name.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="builtInType"></param>
        /// <returns></returns>
        private static bool TryResolveBuiltInType(JsonNode? node,
            out BuiltInType builtInType)
        {
            builtInType = BuiltInType.Null;
            if (node is not JsonValue value)
            {
                return false;
            }
            if (value.GetValueKind() == JsonValueKind.String)
            {
                var name = value.GetValue<string>();
                if (Enum.TryParse(name, true, out builtInType))
                {
                    return true;
                }
                return byte.TryParse(name, out var raw) &&
                    ResolveNumericBuiltInType(raw, out builtInType);
            }
            return value.TryGetValue<byte>(out var numeric) &&
                ResolveNumericBuiltInType(numeric, out builtInType);
        }

        /// <summary>
        /// Resolve a numeric built in type value.
        /// </summary>
        /// <param name="raw"></param>
        /// <param name="builtInType"></param>
        /// <returns></returns>
        private static bool ResolveNumericBuiltInType(byte raw,
            out BuiltInType builtInType)
        {
            if (Enum.IsDefined(typeof(BuiltInType), (int)raw))
            {
                builtInType = (BuiltInType)raw;
                return true;
            }
            builtInType = BuiltInType.Null;
            return false;
        }

        /// <summary>
        /// Default type a bare value (one that does not carry an explicit
        /// type) so that the strict 2.0 decoder can decode it. Mirrors the
        /// previous implementation's behavior of promoting integral numbers
        /// to Int64, real numbers to Double, and defaulting empty arrays to
        /// a null variant.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static JsonNode? ApplyDefaultTyping(JsonNode? value)
        {
            switch (value)
            {
                case null:
                    return null;
                case JsonArray array:
                    if (array.Count == 0)
                    {
                        return null;
                    }
                    if (!TryDefaultElementType(array[0], out var elementType))
                    {
                        return null;
                    }
                    return new JsonObject
                    {
                        ["UaType"] = (byte)elementType,
                        ["Value"] = CoerceForWire(array.DeepClone(), elementType)
                    };
                case JsonValue jsonValue:
                    if (!TryDefaultElementType(jsonValue, out var scalarType))
                    {
                        return null;
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
        /// <returns></returns>
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
                case JsonValueKind.True:
                case JsonValueKind.False:
                    builtInType = BuiltInType.Boolean;
                    return true;
                case JsonValueKind.String:
                    builtInType = BuiltInType.String;
                    return true;
                case JsonValueKind.Number:
                    builtInType = value.TryGetValue<long>(out _)
                        ? BuiltInType.Int64
                        : BuiltInType.Double;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Coerce a value into the on the wire shape the 2.0 decoder expects
        /// for the given type. In the OPC UA JSON encoding 64 bit integers
        /// are represented as strings, so a numeric value (scalar or array
        /// element) is converted to its string form.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static JsonNode? CoerceForWire(JsonNode? value, BuiltInType type)
        {
            if (value is null ||
                type is not (BuiltInType.Int64 or BuiltInType.UInt64))
            {
                return value;
            }
            if (value is JsonArray array)
            {
                return new JsonArray(array
                    .Select(e => StringifyNumber(e)).ToArray());
            }
            return StringifyNumber(value);
        }

        /// <summary>
        /// Represent a numeric json value as its string form (leaving
        /// non numeric values untouched).
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static JsonNode? StringifyNumber(JsonNode? node)
        {
            if (node is JsonValue value &&
                value.GetValueKind() == JsonValueKind.Number)
            {
                return JsonValue.Create(value.ToJsonString());
            }
            return node?.DeepClone();
        }

        /// <summary>
        /// Sanitizes user input by removing quotes around non strings,
        /// or adding array brackets to comma seperated values that are
        /// not string type and recursing through arrays to do the same.
        /// The output is a pure json token that can be passed to the
        /// json decoder.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="isString"></param>
        /// <returns></returns>
        internal JsonNode? Sanitize(JsonNode? value, bool isString)
        {
            if (value is null)
            {
                return value;
            }

            string asString;
            if (IsJsonString(value))
            {
                asString = value.GetValue<string>();
            }
            else
            {
                asString = value.ToJsonString();
            }

            if (value is not JsonObject && value is not JsonArray && !IsJsonString(value))
            {
                //
                // If this should be a string - return as such
                //
                return isString ? JsonValue.Create(asString) : value;
            }

            if (string.IsNullOrWhiteSpace(asString))
            {
                return value;
            }

            //
            // Try to parse string as json
            //
            if (!IsJsonString(value))
            {
                asString = asString.Replace("\\\"", "\"", StringComparison.Ordinal);
            }
            var token = Try.Op(() => ParseLenient(asString));
            if (token is not null)
            {
                value = token;
            }

            if (IsJsonString(value))
            {
                //
                // try to split the string as comma seperated list
                //
                var elements = asString.Split(',');
                if (isString)
                {
                    //
                    // If all elements are quoted, then this is a
                    // string array
                    //
                    if (elements.Length > 1)
                    {
                        var array = new List<string>();
                        foreach (var element in elements)
                        {
                            var trimmed = element.Trim().TrimQuotes();
                            if (trimmed == element)
                            {
                                // Treat entire string as value
                                return value;
                            }
                            array.Add(trimmed);
                        }
                        // No need to sanitize contents
                        return new JsonArray(array
                            .Select(s => (JsonNode?)JsonValue.Create(s)).ToArray());
                    }
                }
                else
                {
                    //
                    // First trim any quotes from string before splitting.
                    //
                    if (elements.Length > 1)
                    {
                        //
                        // Parse as array
                        //
                        var trimmed = elements.Select(e => e.TrimQuotes()).ToArray();
                        try
                        {
                            value = ParseLenient(
                                "[" + trimmed.Aggregate((x, y) => x + "," + y) + "]");
                        }
                        catch
                        {
                            value = ParseLenient(
                                "[\"" + trimmed.Aggregate((x, y) => x + "\",\"" + y) + "\"]");
                        }
                    }
                    else
                    {
                        //
                        // Try to remove next layer of quotes and try again.
                        //
                        var trimmed = asString.Trim().TrimQuotes();
                        if (trimmed != asString)
                        {
                            return Sanitize(JsonValue.Create(trimmed), isString);
                        }
                    }
                }
            }

            if (value is JsonArray list)
            {
                //
                // Sanitize each element accordingly
                //
                return new JsonArray(list
                    .Select(t => Sanitize(t, isString)?.DeepClone()).ToArray());
            }
            return value;
        }

        /// <summary>
        /// Value is a json string
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static bool IsJsonString(JsonNode? node)
        {
            return node is JsonValue value &&
                value.GetValueKind() == JsonValueKind.String;
        }

        /// <summary>
        /// Parse a json string leniently. Unlike
        /// <see cref="JsonNode.Parse(string, JsonNodeOptions?, JsonDocumentOptions)"/>
        /// this also accepts the single quoted strings historically accepted by
        /// the value API.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static JsonNode? ParseLenient(string value)
        {
            try
            {
                return JsonNode.Parse(value, documentOptions: kLenientDocumentOptions);
            }
            catch (JsonException) when (value.Contains('\'', StringComparison.Ordinal))
            {
                return JsonNode.Parse(NormalizeSingleQuotedStrings(value),
                    documentOptions: kLenientDocumentOptions);
            }
        }

        private static string NormalizeSingleQuotedStrings(string value)
        {
            var result = new System.Text.StringBuilder(value.Length);
            var quote = '\0';
            var escaped = false;
            foreach (var character in value)
            {
                if (quote == '\0')
                {
                    if (character == '\'')
                    {
                        quote = character;
                        result.Append('"');
                    }
                    else if (character == '"')
                    {
                        quote = character;
                        result.Append(character);
                    }
                    else
                    {
                        result.Append(character);
                    }
                    continue;
                }

                if (escaped)
                {
                    if (quote == '\'' && character == '\'')
                    {
                        result.Append('\'');
                    }
                    else
                    {
                        result.Append('\\').Append(character);
                    }
                    escaped = false;
                    continue;
                }
                if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == quote)
                {
                    quote = '\0';
                    result.Append('"');
                }
                else
                {
                    if (quote == '\'' && character == '"')
                    {
                        result.Append('\\');
                    }
                    result.Append(character);
                }
            }
            if (escaped || quote != '\0')
            {
                throw new JsonException("Unterminated single-quoted JSON string.");
            }
            return result.ToString();
        }

        /// <summary>
        /// Coerce a json node to its string representation
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static string? ToStringValue(JsonNode? node)
        {
            if (node is null)
            {
                return null;
            }
            if (node is JsonValue value && value.TryGetValue<string>(out var s))
            {
                return s;
            }
            return node.ToJsonString();
        }

        private static readonly JsonDocumentOptions kLenientDocumentOptions = new()
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };
    }
}
