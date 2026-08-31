// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization.Metadata;
    using System.Xml;
    using Xunit;

    public sealed class JsonConvertersTests
    {
        [Fact]
        public void ReadOnlySetConverterReadsNullAndArrays()
        {
            var nullSet = JsonSerializer.Deserialize<IReadOnlySet<string>>(
                "null", Json.Options);
            var set = JsonSerializer.Deserialize<IReadOnlySet<string>>(
                """["a","b","a"]""", Json.Options);

            Assert.Null(nullSet);
            Assert.NotNull(set);
            Assert.Equal(new[] { "a", "b" }, set.OrderBy(v => v));
        }

        [Fact]
        public void ReadOnlySetConverterRejectsNonArrayToken()
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<IReadOnlySet<string>>(
                    """"not-array"""", Json.Options));
        }

        [Fact]
        public void MatrixConverterPadsJaggedRowsWithDefaultValues()
        {
            var matrix = JsonSerializer.Deserialize<int[,]>(
                "[[1,2,3],[4]]", Json.Options);

            Assert.NotNull(matrix);
            Assert.Equal(2, matrix.GetLength(0));
            Assert.Equal(3, matrix.GetLength(1));
            Assert.Equal(1, matrix[0, 0]);
            Assert.Equal(3, matrix[0, 2]);
            Assert.Equal(4, matrix[1, 0]);
            Assert.Equal(0, matrix[1, 1]);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("[1,2]")]
        public void MatrixConverterRejectsMalformedMatrix(string json)
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<int[,]>(json, Json.Options));
        }

        [Fact]
        public void MatrixConverterWritesNullMatrix()
        {
            var json = JsonSerializer.Serialize<int[,]>(null!, Json.Options);

            Assert.Equal("null", json);
        }

        [Fact]
        public void ByteArrayConverterReadsBase64ArrayAndNull()
        {
            var fromBase64 = JsonSerializer.Deserialize<byte[]>(
                "\"AQID\"", Json.Options);
            var fromArray = JsonSerializer.Deserialize<byte[]>(
                "[1,2,3]", Json.Options);
            var fromNull = JsonSerializer.Deserialize<byte[]>(
                "null", Json.Options);

            Assert.Equal(new byte[] { 1, 2, 3 }, fromBase64);
            Assert.Equal(new byte[] { 1, 2, 3 }, fromArray);
            Assert.Null(fromNull);
        }

        [Fact]
        public void XmlElementConverterRoundTripsBase64XmlAndNull()
        {
            var document = new XmlDocument();
            document.LoadXml("<root attr=\"value\"><child /></root>");

            var json = JsonSerializer.Serialize(document.DocumentElement,
                ReflectionOptions);
            var restored = JsonSerializer.Deserialize<XmlElement>(json,
                ReflectionOptions);
            var restoredNull = JsonSerializer.Deserialize<XmlElement>("null",
                ReflectionOptions);

            Assert.NotNull(restored);
            Assert.Equal("root", restored.Name);
            Assert.Equal("value", restored.GetAttribute("attr"));
            Assert.Null(restoredNull);
        }

        [Fact]
        public void XmlElementConverterRejectsNonStringToken()
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<XmlElement>("123", ReflectionOptions));
        }

        [Theory]
        [InlineData("123456789012345678901234567890")]
        [InlineData("\"123456789012345678901234567890\"")]
        public void BigIntegerConverterReadsNumberAndStringTokens(string json)
        {
            var value = JsonSerializer.Deserialize<BigInteger>(json, ReflectionOptions);

            Assert.Equal(BigInteger.Parse("123456789012345678901234567890"), value);
        }

        [Fact]
        public void BigIntegerConverterWritesRawNumber()
        {
            var value = BigInteger.Parse("123456789012345678901234567890");

            var json = JsonSerializer.Serialize(value, ReflectionOptions);

            Assert.Equal("123456789012345678901234567890", json);
        }

        [Fact]
        public void BigIntegerConverterRejectsNonScalarToken()
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<BigInteger>("[]", ReflectionOptions));
        }

        private static JsonSerializerOptions ReflectionOptions => new(Json.Options)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
    }
}
