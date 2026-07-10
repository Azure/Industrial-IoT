// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Xml;

    /// <summary>
    /// Test-only compatibility helpers that provide the small subset of the
    /// former Legacy <c>VariantValue</c> convenience API on top of
    /// <see cref="JsonNode"/>. These exist purely so the existing integration
    /// test bodies continue to compile after the migration from
    /// <c>VariantValue</c> to <see cref="JsonNode"/>. They are behaviour
    /// preserving translations of the corresponding <c>VariantValue</c> members.
    /// </summary>
    internal static class JsonNodeValueExtensions
    {
        /// <summary>
        /// Build a <see cref="JsonNode"/> from an object using the same json
        /// conventions as the former <c>VariantValue</c> based serializer.
        /// </summary>
        public static JsonNode? FromObject(object? value)
            => value is null ? null : Json.FromObject(value);

        /// <summary>
        /// Build a <see cref="JsonArray"/> from the provided values using the
        /// same json conventions as the former <c>VariantValue</c> serializer.
        /// </summary>
        public static JsonNode? FromArray(params object?[] values)
            => Json.FromObject(values);

        /// <summary>
        /// Whether the node represents a null (missing) json value.
        /// </summary>
        public static bool IsNull(this JsonNode? node)
            => node is null || node.GetValueKind() == JsonValueKind.Null;

        /// <summary>
        /// Whether the node is null or the json null token.
        /// </summary>
        public static bool IsNullOrNullValue(JsonNode? node) => node.IsNull();

        /// <summary>
        /// Whether the node is a json object.
        /// </summary>
        public static bool IsObject(this JsonNode? node) => node is JsonObject;

        /// <summary>
        /// Whether the node is a json array.
        /// </summary>
        public static bool IsArray(this JsonNode? node) => node is JsonArray;

        /// <summary>
        /// Whether the node is a list of values (json array).
        /// </summary>
        public static bool IsListOfValues(this JsonNode? node) => node is JsonArray;

        /// <summary>
        /// Whether the node is a json string.
        /// </summary>
        public static bool IsString(this JsonNode? node)
            => node?.GetValueKind() == JsonValueKind.String;

        /// <summary>
        /// Whether the node is a byte string (base64 encoded json string).
        /// </summary>
        public static bool IsBytes(this JsonNode? node) => node.IsString();

        /// <summary>
        /// Whether the node is a date time (encoded as json string).
        /// </summary>
        public static bool IsDateTime(this JsonNode? node) => node.IsString();

        /// <summary>
        /// Whether the node is a guid (encoded as json string).
        /// </summary>
        public static bool IsGuid(this JsonNode? node) => node.IsString();

        /// <summary>
        /// Whether the node is a boolean.
        /// </summary>
        public static bool IsBoolean(this JsonNode? node)
        {
            var kind = node?.GetValueKind();
            return kind == JsonValueKind.True || kind == JsonValueKind.False;
        }

        /// <summary>
        /// Whether the node is an integral number.
        /// </summary>
        public static bool IsInteger(this JsonNode? node)
        {
            if (node?.GetValueKind() != JsonValueKind.Number)
            {
                return false;
            }
            var raw = node.ToJsonString();
            return raw.IndexOfAny(['.', 'e', 'E']) < 0;
        }

        /// <summary>
        /// Whether the node is a floating point number.
        /// </summary>
        public static bool IsDouble(this JsonNode? node)
            => node?.GetValueKind() == JsonValueKind.Number;

        /// <summary>
        /// Whether the node is a floating point number.
        /// </summary>
        public static bool IsFloat(this JsonNode? node) => node.IsDouble();

        /// <summary>
        /// Whether the node is a decimal number.
        /// </summary>
        public static bool IsDecimal(this JsonNode? node) => node.IsDouble();

        /// <summary>
        /// Enumerate the values of an array node.
        /// </summary>
        public static IReadOnlyList<JsonNode?> Values(this JsonNode? node)
            => node is JsonArray array ? array.ToList() : [];

        /// <summary>
        /// Count of the elements of an array or the members of an object.
        /// </summary>
        public static int Count(this JsonNode? node) => node switch
        {
            JsonArray array => array.Count,
            JsonObject obj => obj.Count,
            _ => 0
        };

        /// <summary>
        /// Convert the node to the requested type.
        /// </summary>
        public static T? ConvertTo<T>(this JsonNode? node)
        {
            if (node is null)
            {
                return default;
            }
            if (typeof(T) == typeof(XmlElement))
            {
                var xml = node.GetValueKind() == JsonValueKind.String
                    ? node.GetValue<string>() : node.ToJsonString();
                var document = new XmlDocument();
                document.LoadXml(xml);
                return (T)(object)document.DocumentElement!;
            }
            return Json.Deserialize<T>(node.ToJsonString());
        }
    }
}
