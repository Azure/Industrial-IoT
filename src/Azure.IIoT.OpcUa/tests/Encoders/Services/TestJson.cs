// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using System;
    using System.Globalization;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Test helper that produces <see cref="JsonNode"/> instances from
    /// objects and arrays, matching the JSON representation the variant
    /// encoder produces so they can be compared with
    /// <see cref="JsonNode.DeepEquals(JsonNode, JsonNode)"/>. Floating
    /// point values are rendered with a decimal point (for example
    /// <c>0.0</c> instead of <c>0</c>) so that integral values retain
    /// their floating point token type, matching the encoder output.
    /// </summary>
    internal static class TestJson
    {
        /// <summary>
        /// Create a json node from an object
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static JsonNode? FromObject(object? value)
        {
            if (value is JsonNode node)
            {
                return node.DeepClone();
            }
            return JsonNode.Parse(JsonSerializer.Serialize(value, kOptions));
        }

        /// <summary>
        /// Create a json array node from the passed values
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static JsonNode? FromArray(params object?[] values)
        {
            return JsonNode.Parse(JsonSerializer.Serialize(values, kOptions));
        }

        private static readonly JsonSerializerOptions kOptions = CreateOptions();

        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                AllowTrailingCommas = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString |
                    JsonNumberHandling.AllowNamedFloatingPointLiterals,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Insert(0, new FloatingPointConverter<double>(
                d => double.IsFinite(d) && d == Math.Floor(d),
                d => d.ToString("R", CultureInfo.InvariantCulture)));
            options.Converters.Insert(0, new FloatingPointConverter<float>(
                f => float.IsFinite(f) && f == MathF.Floor(f),
                f => f.ToString("R", CultureInfo.InvariantCulture)));
            return options;
        }

        /// <summary>
        /// Emits integral floating point values with a trailing ".0" so the
        /// JSON number token keeps a floating point representation.
        /// </summary>
        private sealed class FloatingPointConverter<T> : JsonConverter<T>
            where T : struct
        {
            public FloatingPointConverter(Func<T, bool> isIntegral,
                Func<T, string> toRoundTrip)
            {
                _isIntegral = isIntegral;
                _toRoundTrip = toRoundTrip;
            }

            public override T Read(ref Utf8JsonReader reader, Type typeToConvert,
                JsonSerializerOptions options)
            {
                return (T)Convert.ChangeType(reader.GetDouble(), typeof(T),
                    CultureInfo.InvariantCulture);
            }

            public override void Write(Utf8JsonWriter writer, T value,
                JsonSerializerOptions options)
            {
                if (_isIntegral(value))
                {
                    writer.WriteRawValue(_toRoundTrip(value) + ".0");
                }
                else
                {
                    writer.WriteRawValue(_toRoundTrip(value));
                }
            }

            private readonly Func<T, bool> _isIntegral;
            private readonly Func<T, string> _toRoundTrip;
        }
    }
}
