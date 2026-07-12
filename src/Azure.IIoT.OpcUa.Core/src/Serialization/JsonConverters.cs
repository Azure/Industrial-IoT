// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Numerics;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Json.Serialization.Metadata;
    using System.Xml;

    /// <summary>
    /// Converts a read-only set with a statically known element type.
    /// </summary>
    /// <typeparam name="TElement">The set element type.</typeparam>
    internal sealed class ReadOnlySetConverter<TElement> :
        JsonConverter<IReadOnlySet<TElement>?>
    {
        /// <inheritdoc/>
        public override IReadOnlySet<TElement>? Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            var elementTypeInfo = (JsonTypeInfo<TElement>)options.GetTypeInfo(
                typeof(TElement));
            var set = new HashSet<TElement>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                set.Add(JsonSerializer.Deserialize(ref reader, elementTypeInfo)!);
            }
            return set;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, IReadOnlySet<TElement>? value,
            JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            var elementTypeInfo = (JsonTypeInfo<TElement>)options.GetTypeInfo(
                typeof(TElement));
            writer.WriteStartArray();
            foreach (var item in value)
            {
                JsonSerializer.Serialize(writer, item, elementTypeInfo);
            }
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Converts a two-dimensional matrix with a statically known element type.
    /// </summary>
    /// <typeparam name="TElement">The matrix element type.</typeparam>
    internal sealed class MatrixConverter<TElement> : JsonConverter<TElement[,]>
    {
        /// <inheritdoc/>
        [SuppressMessage("Performance", "CA1814",
            Justification = "The converter's public contract is a rectangular matrix.")]
        public override TElement[,] Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected beginning of matrix array.");
            }

            var elementTypeInfo = (JsonTypeInfo<TElement>)options.GetTypeInfo(
                typeof(TElement));
            var rows = new List<List<TElement>>();
            var width = 0;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException("Expected matrix row.");
                }

                var row = new List<TElement>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    row.Add(JsonSerializer.Deserialize(ref reader, elementTypeInfo)!);
                }
                width = Math.Max(width, row.Count);
                rows.Add(row);
            }

            var result = new TElement[rows.Count, width];
            for (var row = 0; row < rows.Count; row++)
            {
                for (var column = 0; column < rows[row].Count; column++)
                {
                    result[row, column] = rows[row][column];
                }
            }
            return result;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TElement[,] value,
            JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            var elementTypeInfo = (JsonTypeInfo<TElement>)options.GetTypeInfo(
                typeof(TElement));
            writer.WriteStartArray();
            for (var row = 0; row < value.GetLength(0); row++)
            {
                writer.WriteStartArray();
                for (var column = 0; column < value.GetLength(1); column++)
                {
                    JsonSerializer.Serialize(writer, value[row, column], elementTypeInfo);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Byte array converter allowing a list of integers.
    /// </summary>
    internal sealed class ByteArrayConverter : JsonConverter<byte[]>
    {
        /// <inheritdoc/>
        public override byte[]? Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var list = new List<byte>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    list.Add(reader.GetByte());
                }
                return list.ToArray();
            }
            return reader.GetBytesFromBase64();
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer,
            byte[]? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteBase64StringValue(value);
            }
        }
    }

    /// <summary>
    /// Xml element converter.
    /// </summary>
    internal sealed class XmlElementConverter : JsonConverter<XmlElement>
    {
        /// <inheritdoc/>
        public override XmlElement? Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }

            var xml = Encoding.UTF8.GetString(reader.GetBytesFromBase64());
            var document = new XmlDocument();
            document.LoadXml(xml);
            return document.DocumentElement;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer,
            XmlElement? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteBase64StringValue(Encoding.UTF8.GetBytes(value.OuterXml));
            }
        }
    }

    /// <summary>
    /// Big integer converter.
    /// </summary>
    internal sealed class BigIntegerConverter : JsonConverter<BigInteger>
    {
        /// <inheritdoc/>
        public override BigInteger Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is not JsonTokenType.Number and
                not JsonTokenType.String)
            {
                throw new JsonException();
            }

            using var document = JsonDocument.ParseValue(ref reader);
            var value = document.RootElement.ValueKind == JsonValueKind.String
                ? document.RootElement.GetString()
                : document.RootElement.GetRawText();
            return BigInteger.Parse(value ?? throw new JsonException(),
                NumberFormatInfo.InvariantInfo);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, BigInteger value,
            JsonSerializerOptions options)
        {
            writer.WriteRawValue(value.ToString(NumberFormatInfo.InvariantInfo));
        }
    }
}
