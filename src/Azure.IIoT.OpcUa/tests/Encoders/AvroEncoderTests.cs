// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Encoders.Schemas.Avro;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Xunit;

    /// <summary>
    /// Tests for the Json builder and decoder class.
    /// </summary>
    public sealed class AvroEncoderTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestBoolean(bool value)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteBoolean(null, value);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteBoolean(null, value);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(value, decoder.ReadBoolean(null));
        }

        [Theory]
        [InlineData(0u)]
        [InlineData(1000u)]
        [InlineData(long.MaxValue)]
        public void TestLong(long value)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteInt64(null, value);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteInt64(null, value);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(value, decoder.ReadInt64(null));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(Math.PI)]
        [InlineData(double.MaxValue)]
        public void TestDouble(double value)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteDouble(null, value);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteDouble(null, value);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(value, decoder.ReadDouble(null));
        }

        [Theory]
        [InlineData(0.0f)]
        [InlineData((float)Math.PI)]
        [InlineData(float.MaxValue)]
        public void TestFloat(float value)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteFloat(null, value);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteFloat(null, value);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(value, decoder.ReadFloat(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("test")]
        [InlineData("12345")]
        [InlineData("12345678901234567890123456789012345678901234567890123456789012345678901234567890")]
        public void TestString(string value)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteString(null, value);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteString(null, value);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(value, decoder.ReadString(null));
        }

        [Theory]
        [InlineData(1096)]
        [InlineData(4096)]
        [InlineData(65535)]
        public void TestStringLengths(int length)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            var buffer = new char[length];
            buffer.AsSpan().Fill('a');
            var value = new string(buffer);
            builder.WriteString(null, value);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteString(null, value);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(value, decoder.ReadString(null));
        }

        [Fact]
        public void TestLargeStringLengthThrows()
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            var buffer = new char[80000];
            buffer.AsSpan().Fill('a');
            var value = new string(buffer);
            builder.WriteString(null, value);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteString(null, value);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Throws<DecodingException>(() => decoder.ReadString(null));
        }

        [Fact]
        public void TestStringWithZeroTerminator()
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            const string value = "teststring";
            builder.WriteString(null, value + '\0');
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteString(null, value + '\0');
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(value, decoder.ReadString(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(127)]
        [InlineData(257)]
        [InlineData(5000)]
        [InlineData(100000)]
        public void TestByteString(int length)
        {
            var context = new ServiceMessageContext();
            var expected = new byte[length];
#pragma warning disable CA5394 // Do not use insecure randomness
            Random.Shared.NextBytes(expected);
#pragma warning restore CA5394 // Do not use insecure randomness
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteByteString(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteByteString(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadByteString(null));
        }

        [Theory]
        [InlineData(0u)]
        [InlineData(1000u)]
        [InlineData(long.MaxValue)]
        [InlineData(ulong.MaxValue)]
        public void TestULong(ulong value)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteUInt64(null, value);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteUInt64(null, value);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(value, decoder.ReadUInt64(null));
        }

        [Theory]
        [InlineData(StatusCodes.Good)]
        [InlineData(StatusCodes.Bad)]
        [InlineData(StatusCodes.Uncertain)]
        public void TestStatusCode(uint value)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteStatusCode(null, value);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteStatusCode(null, value);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(value, decoder.ReadStatusCode(null));
        }

        [Theory]
        [InlineData(false, StatusCodes.Good)]
        [InlineData(false, "test")]
        [InlineData(false, 12345)]
        [InlineData(true, StatusCodes.Good)]
        [InlineData(true, "test")]
        [InlineData(true, 12345)]
        public void TestVariant(bool concise, object value)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, concise);
            builder.WriteVariant(null, new Variant(value));
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteVariant(null, new Variant(value));
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(value, decoder.ReadVariant(null).Value);
        }

        [Theory]
        [InlineData(StatusCodes.Good)]
        [InlineData("test")]
        [InlineData(12345)]
        public void TestVariantWithNullableValue(object value)
        {
            var context = new ServiceMessageContext();
            var expected = new Variant(value);

            var schemas = new AvroBuiltInSchemas();
            var valueSchema = schemas.GetSchemaForBuiltInType(expected.TypeInfo.BuiltInType);
            var schema = valueSchema.AsNullable().CreateRoot();

            using (var stream = new MemoryStream())
            using (var encoder = new AvroEncoder(stream, schema, context, true))
            {
                encoder.WriteVariant(null, expected);
                stream.Position = 0;
                using var decoder = new AvroDecoder(stream, schema, context);
                var variant = decoder.ReadVariant(null);
                Assert.Equal(expected, variant);
            }

            using (var stream = new MemoryStream())
            using (var encoder = new AvroEncoder(stream, schema, context, true))
            {
                encoder.WriteVariant(null, Variant.Null);
                stream.Position = 0;
                using var decoder = new AvroDecoder(stream, schema, context);
                var variant = decoder.ReadVariant(null);
                Assert.Equal(Variant.Null, variant);
            }
        }

        public static TheoryData<VariantHolder> GetValues()
        {
            return new TheoryData<VariantHolder>(VariantVariants.GetValues().Select(v => new VariantHolder(v)));
        }

        [Theory]
        [MemberData(nameof(GetValues))]
        public void TestVariantVariants(VariantHolder value)
        {
            var expected = value.Variant;
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, false);
            builder.WriteVariant(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteVariant(null, expected);
            stream.Position = 0;
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected.Value, decoder.ReadVariant(null).Value);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestVariantWithComplexEncodeable(bool concise)
        {
            var expected = new Variant(VariantVariants.Complex);
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, concise);
            builder.WriteVariant(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteVariant(null, expected);
            stream.Position = 0;
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            using var decoder = new AvroDecoder(stream, builder.Schema, context);

            var value = decoder.ReadVariant(null).Value;
            Assert.True(value is ExtensionObject);
            var eo = (ExtensionObject)value;
            Assert.True(eo.Body is IEncodeable);
            var e = (IEncodeable)eo.Body;
            Assert.Equal(VariantVariants.Complex.AsJson(context), e.AsJson(context));
        }

        [Theory]
        [MemberData(nameof(GetValues))]
        public void TestVariantVariantsConcise(VariantHolder value)
        {
            var expected = value.Variant;
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, true);
            builder.WriteVariant(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteVariant(null, expected);
            stream.Position = 0;
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadVariant(null));
        }

        [Theory]
        [InlineData("test")]
        [InlineData(12345u)]
        public void TestNodeId(object value)
        {
            var context = new ServiceMessageContext();
            var ns = context.NamespaceUris.GetIndexOrAppend("test.org");
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            var expected = new NodeId(value, ns);
            builder.WriteNodeId(null, expected);
            stream.Position = 0;
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteNodeId(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadNodeId(null));
        }

        [Fact]
        public void TestNodeIdOpaque()
        {
            var context = new ServiceMessageContext();
            var ns = context.NamespaceUris.GetIndexOrAppend("test.org");
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            var expected = new NodeId(Guid.NewGuid().ToByteArray(), ns);
            builder.WriteNodeId(null, expected);
            stream.Position = 0;
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteNodeId(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadNodeId(null));
        }

        [Fact]
        public void TestNodeIdGuid()
        {
            var context = new ServiceMessageContext();
            var ns = context.NamespaceUris.GetIndexOrAppend("test.org");
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            var expected = new NodeId(Guid.NewGuid(), ns);
            builder.WriteNodeId(null, expected);
            stream.Position = 0;
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteNodeId(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadNodeId(null));
        }

        [Fact]
        public void TestExpandedNodeIdOpaque()
        {
            var context = new ServiceMessageContext();
            context.NamespaceUris.GetIndexOrAppend("test.org");
            var srv = context.ServerUris.GetIndexOrAppend("Super");
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            var expected = new ExpandedNodeId(Guid.NewGuid().ToByteArray(), 0, "test.org", srv);
            builder.WriteExpandedNodeId(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteExpandedNodeId(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadExpandedNodeId(null));
        }

        [Theory]
        [InlineData(false, StatusCodes.Good)]
        [InlineData(false, "test")]
        [InlineData(false, 12345)]
        [InlineData(true, StatusCodes.Good)]
        [InlineData(true, "test")]
        [InlineData(true, 12345)]
        public void TestDataValue(bool concise, object value)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, concise);
            var expected = new DataValue
            {
                Value = value,
                StatusCode = StatusCodes.Bad,
                ServerPicoseconds = 10,
                ServerTimestamp = DateTime.UtcNow
            };
            builder.WriteDataValue(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteDataValue(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadDataValue(null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestDataValueEmpty(bool concise)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, concise);
            var expected = new DataValue();
            builder.WriteDataValue(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteDataValue(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadDataValue(null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestDataValueNull(bool concise)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, concise);
            builder.WriteDataValue(null, null);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteDataValue(null, null);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.True(Opc.Ua.Utils.IsEqual(new DataValue(),
                decoder.ReadDataValue(null)));
        }

        [Fact]
        public void TestGuid()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteGuid(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteGuid(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadGuid(null));
        }
        [Fact]
        public void TestDateTime()
        {
            var context = new ServiceMessageContext();
            var expected = DateTime.UtcNow;
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteDateTime(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteDateTime(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadDateTime(null));
        }

        [Fact]
        public void TestXmlElement1()
        {
            var context = new ServiceMessageContext();
            var expected = new XmlDocument();
            expected.LoadXml("<test></test>");
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteXmlElement(null, expected.DocumentElement);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteXmlElement(null, expected.DocumentElement);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var actual = new XmlDocument();
            actual.Load(decoder.ReadXmlElement(null).CreateNavigator().ReadSubtree());
            Assert.Equal(expected.OuterXml, actual.OuterXml);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestXmlElement2(bool concise)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, concise);
            var expected = VariantVariants.XmlElement;
            builder.WriteXmlElement(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteXmlElement(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var actual = new XmlDocument();
            actual.Load(decoder.ReadXmlElement(null).CreateNavigator().ReadSubtree());
            Assert.Equal(expected.OuterXml, actual.OuterXml);
        }

        [Fact]
        public void TestQualifiedName()
        {
            var context = new ServiceMessageContext();
            var ns = context.NamespaceUris.GetIndexOrAppend("test.org");
            var expected = new QualifiedName("test", ns);
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteQualifiedName(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteQualifiedName(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadQualifiedName(null));
        }

        [Fact]
        public void TestLocalizedText()
        {
            var context = new ServiceMessageContext();
            var expected = new LocalizedText("test", "en");
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteLocalizedText(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteLocalizedText(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadLocalizedText(null));
        }

        [Fact]
        public void TestExtensionObject()
        {
            var context = new ServiceMessageContext();
            var expected = new ExtensionObject(new NodeId(1234), new byte[] { 0, 1, 2, 3 });
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteExtensionObject(null, expected);
            stream.Position = 0;
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteExtensionObject(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var actual = decoder.ReadExtensionObject(null);
            Assert.Equal(expected.TypeId, actual.TypeId);
            Assert.Equal(expected.Body, actual.Body);
        }

        [Fact]
        public void TestStatusCodeArray()
        {
            var context = new ServiceMessageContext();
            var expected = new StatusCode[] { StatusCodes.Good, StatusCodes.Bad };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteStatusCodeArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteStatusCodeArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var actual = decoder.ReadStatusCodeArray(null);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestNodeIdArray()
        {
            var context = new ServiceMessageContext();
            var ns = context.NamespaceUris.GetIndexOrAppend("test.org");
            var expected = new NodeId[] { new(123, ns), new(456, ns) };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteNodeIdArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteNodeIdArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var actual = decoder.ReadNodeIdArray(null);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestExpandedNodeIdArray()
        {
            var context = new ServiceMessageContext();
            context.NamespaceUris.GetIndexOrAppend("test.org");
            var srv = context.ServerUris.GetIndexOrAppend("Super");
            var expected = new ExpandedNodeId[] { new(123u, 0, "test.org", srv), new(456u, 0, "test.org", srv) };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteExpandedNodeIdArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteExpandedNodeIdArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var actual = decoder.ReadExpandedNodeIdArray(null);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestInt16()
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteInt16(null, 123);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteInt16(null, 123);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(123, decoder.ReadInt16(null));
        }

        [Fact]
        public void TestSByte()
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteSByte(null, 123);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteSByte(null, 123);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(123, decoder.ReadSByte(null));
        }

        [Fact]
        public void TestByte()
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteByte(null, 123);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteByte(null, 123);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(123, decoder.ReadByte(null));
        }

        [Fact]
        public void TestDiagnosticInfo()
        {
            var context = new ServiceMessageContext();
            var expected = new DiagnosticInfo { AdditionalInfo = "dd" };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteDiagnosticInfo(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteDiagnosticInfo(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var result = decoder.ReadDiagnosticInfo(null);
            AssertEqual(expected, result);
        }

        [Fact]
        public void TestDiagnosticInfoWithInner()
        {
            var context = new ServiceMessageContext();
            var expected = new DiagnosticInfo
            {
                AdditionalInfo = "outer",
                InnerDiagnosticInfo = new DiagnosticInfo { AdditionalInfo = "inner" }
            };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteDiagnosticInfo(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteDiagnosticInfo(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var result = decoder.ReadDiagnosticInfo(null);
            AssertEqual(expected, result);
        }

        [Fact]
        public void TestEnum()
        {
            var context = new ServiceMessageContext();
            const DiagnosticsLevel expected = DiagnosticsLevel.Basic;
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteEnumerated(null, expected);
            stream.Position = 0;
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteEnumerated(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var result = decoder.ReadEnumerated<DiagnosticsLevel>(null);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestEnumAsInteger()
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            const int expected = 1;
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteEnumerated(null, expected);
            stream.Position = 0;
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteEnumerated(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var result = decoder.ReadEnumerated<DiagnosticsLevel>(null);
            Assert.Equal((DiagnosticsLevel)expected, result);
        }

        [Fact]
        public void TestEnumArray()
        {
            var context = new ServiceMessageContext();
            var expected = new DiagnosticsLevel[] { DiagnosticsLevel.Basic, DiagnosticsLevel.Advanced };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteEnumeratedArray(null, expected, null);
            stream.Position = 0;
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteEnumeratedArray(null, expected, null);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var actual = decoder.ReadEnumeratedArray<DiagnosticsLevel>(null);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestDiagnosticInfoArray()
        {
            var context = new ServiceMessageContext();
            var expected = new DiagnosticInfo[] { new() { AdditionalInfo = "dd" }, new() { AdditionalInfo = string.Empty } };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteDiagnosticInfoArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteDiagnosticInfoArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var actual = decoder.ReadDiagnosticInfoArray(null);
            Assert.Equal(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                var result = actual[i];
                AssertEqual(expected[i], result);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestBooleanArray(int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).Select(v => v % 2 == 0).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteBooleanArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteBooleanArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadBooleanArray(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestSByteArray(int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).Select(v => (sbyte)v).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteSByteArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteSByteArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadSByteArray(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestByteArray(int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).Select(v => (byte)v).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteByteArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteByteArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadByteArray(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestInt16Array(int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).Select(v => (short)v).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteInt16Array(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteInt16Array(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadInt16Array(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestUInt16Array(int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).Select(v => (ushort)v).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteUInt16Array(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteUInt16Array(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadUInt16Array(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestInt32Array(int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteInt32Array(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteInt32Array(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadInt32Array(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestUInt32Array(int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).Select(v => (uint)v).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteUInt32Array(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteUInt32Array(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadUInt32Array(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestInt64Array(int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).Select(v => uint.MaxValue + v).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteInt64Array(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteInt64Array(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadInt64Array(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestUInt64Array(int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).Select(v => (ulong)(long.MaxValue + v)).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteUInt64Array(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteUInt64Array(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadUInt64Array(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestFloatArray(int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).Select(v => (float)v).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteFloatArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteFloatArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadFloatArray(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestDoubleArray(int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).Select(v => (double)v).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteDoubleArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteDoubleArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadDoubleArray(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestStringArray(int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length)
                .Select(v => v.ToString(CultureInfo.InvariantCulture)).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteStringArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteStringArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadStringArray(null));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(3, 0)]
        [InlineData(3, 3)]
        [InlineData(4096, 0)]
        [InlineData(4096, 4096)]
        public void TestByteStringArray(int length, int bytestringLength)
        {
            var context = new ServiceMessageContext();
            var buffer = new byte[bytestringLength];
#pragma warning disable CA5394 // Do not use insecure randomness
            Random.Shared.NextBytes(buffer);
#pragma warning restore CA5394 // Do not use insecure randomness
            var expected = Enumerable.Range(0, length).Select(_ => buffer).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteByteStringArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteByteStringArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadByteStringArray(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestDateTimeArray(int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).Select(_ => DateTime.UtcNow).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteDateTimeArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteDateTimeArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadDateTimeArray(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestUuidArray(int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).Select(_ => (Uuid)Guid.NewGuid()).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteGuidArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteGuidArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadGuidArray(null).ToArray());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestGuidArray(int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).Select(_ => Guid.NewGuid()).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteGuidArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteGuidArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadGuidArray(null).Select(g => (Guid)g).ToArray());
        }

        [Fact]
        public void TestXmlElementArray1()
        {
            var context = new ServiceMessageContext();
            var expected = new XmlDocument();
            expected.LoadXml("<test></test>");
            var expectedArray = new XmlElement[] { expected.DocumentElement, expected.DocumentElement };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteXmlElementArray(null, expectedArray);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteXmlElementArray(null, expectedArray);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var actualArray = decoder.ReadXmlElementArray(null);
            Assert.Equal(expectedArray.Length, actualArray.Count);
            for (var i = 0; i < expectedArray.Length; i++)
            {
                var actual = new XmlDocument();
                actual.Load(actualArray[i].CreateNavigator().ReadSubtree());
                Assert.Equal(expected.OuterXml, actual.OuterXml);
            }
        }

        [Fact]
        public void TestXmlElementArray2()
        {
            var context = new ServiceMessageContext();
            var expected = VariantVariants.XmlElement;
            var expectedArray = new XmlElement[] { expected, expected };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteXmlElementArray(null, expectedArray);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteXmlElementArray(null, expectedArray);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var actualArray = decoder.ReadXmlElementArray(null);
            Assert.Equal(expectedArray.Length, actualArray.Count);
            for (var i = 0; i < expectedArray.Length; i++)
            {
                var actual = new XmlDocument();
                actual.Load(actualArray[i].CreateNavigator().ReadSubtree());
                Assert.Equal(expected.OuterXml, actual.OuterXml);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4096)]
        public void TestQualifiedNameArray(int length)
        {
            var context = new ServiceMessageContext();
            var ns = context.NamespaceUris.GetIndexOrAppend("test.org");
            var expected = Enumerable.Range(0, length).Select(v => new QualifiedName("test" + v, ns)).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteQualifiedNameArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteQualifiedNameArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadQualifiedNameArray(null));
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(false, 1)]
        [InlineData(false, 3)]
        [InlineData(false, 4096)]
        [InlineData(true, 0)]
        [InlineData(true, 1)]
        [InlineData(true, 3)]
        [InlineData(true, 4096)]
        public void TestLocalizedTextArray1(bool concise, int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).Select(v => new LocalizedText("test" + v, "en")).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, concise);
            builder.WriteLocalizedTextArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteLocalizedTextArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            Assert.Equal(expected, decoder.ReadLocalizedTextArray(null));
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(false, 1)]
        [InlineData(false, 3)]
        [InlineData(false, 4096)]
        [InlineData(true, 0)]
        [InlineData(true, 1)]
        [InlineData(true, 3)]
        [InlineData(true, 4096)]
        public void TestLocalizedTextArray2(bool concise, int length)
        {
            var context = new ServiceMessageContext();
            var expected = Enumerable.Range(0, length).Select(v => new LocalizedText("test" + v, "en")).ToArray();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, concise);
            builder.WriteArray(null, expected, ValueRanks.OneDimension, BuiltInType.LocalizedText);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteArray(null, expected, ValueRanks.OneDimension, BuiltInType.LocalizedText);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var result = decoder.ReadArray(null, ValueRanks.OneDimension, BuiltInType.LocalizedText);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestExtensionObjectArray()
        {
            var context = new ServiceMessageContext();
            var expected = new ExtensionObject[] { new(new NodeId(1234), new byte[] { 0, 1, 2, 3 }), new(new NodeId(1234), new byte[] { 0, 1, 2, 3 }) };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true);
            builder.WriteExtensionObjectArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteExtensionObjectArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var actual = decoder.ReadExtensionObjectArray(null);
            Assert.Equal(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].TypeId, actual[i].TypeId);
                Assert.Equal(expected[i].Body, actual[i].Body);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestDataValueArray1(bool concise)
        {
            var context = new ServiceMessageContext();
            var expected = new DataValue[] { new(), new() };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, concise);
            builder.WriteDataValueArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteDataValueArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var actual = decoder.ReadDataValueArray(null);
            Assert.Equal(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestDataValueArray2(bool concise)
        {
            var context = new ServiceMessageContext();
            var expected = new DataValue[] {
                new() { Value = new Variant(100) },
                new() { Value = new Variant(200) }
            };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, concise);
            builder.WriteDataValueArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteDataValueArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var actual = decoder.ReadDataValueArray(null);
            Assert.Equal(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestVariantArrayWithDifferentTypes1(bool concise)
        {
            var context = new ServiceMessageContext();
            var expected = new Variant[] { new(123), new("test") };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, concise);
            builder.WriteVariantArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteVariantArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            var actual = decoder.ReadVariantArray(null);
            Assert.Equal(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].Value, actual[i].Value);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestVariantArrayWithDifferentTypes2(bool concise)
        {
            var context = new ServiceMessageContext();
            var expected = new VariantCollection {
                new Variant(4L),
                new Variant("test"),
                new Variant(new long[] {1, 2, 3, 4, 5 }),
                new Variant(new string[] {"1", "2", "3", "4", "5" })
            };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, concise);
            builder.WriteVariantArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteVariantArray(null, expected);
            stream.Position = 0;
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);

            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var actual = decoder.ReadVariantArray(null);
            Assert.Equal(expected.Count, actual.Count);
            for (var i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].Value, actual[i].Value);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestVariantArrayWithDifferentTypes3(bool concise)
        {
            var context = new ServiceMessageContext();
            var expected = new VariantCollection {
                new Variant(new VariantCollection {
                    new Variant(4L),
                    new Variant("test"),
                    new Variant(new long[] {1, 2, 3, 4, 5 }),
                    new Variant(new string[] {"1", "2", "3", "4", "5" })
                })
            };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, concise);
            builder.WriteVariantArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteVariantArray(null, expected);
            stream.Position = 0;
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);

            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var actual = decoder.ReadVariantArray(null);
            Assert.Equal(expected.Count, actual.Count);
            for (var i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].Value, actual[i].Value);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestVariantArrayWithSameTypes(bool concise)
        {
            var context = new ServiceMessageContext();
            var expected = new Variant[] { new(123), new(321), new(0) };
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, concise);
            builder.WriteVariantArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteVariantArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            var actual = decoder.ReadVariantArray(null);
            Assert.Equal(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].Value, actual[i].Value);
            }
        }

        public static TheoryData<VariantsHolder> GetVariantArrays()
        {
            return new TheoryData<VariantsHolder>(VariantVariants.GetValues()
                .Select(v => new VariantsHolder(Enumerable.Repeat(v, 3).ToArray(), v.TypeInfo)));
        }

        [Theory]
        [MemberData(nameof(GetVariantArrays))]
        public void TestVariantArrayVariants(VariantsHolder value)
        {
            var expected = value.Variants.ToArray();
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, false);
            builder.WriteVariantArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteVariantArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            var actual = decoder.ReadVariantArray(null);
            Assert.Equal(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].Value, actual[i].Value);
            }
        }

        [Theory]
        [MemberData(nameof(GetVariantArrays))]
        public void TestVariantArrayVariantsConcise(VariantsHolder value)
        {
            var expected = value.Variants.ToArray();
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, true);
            builder.WriteVariantArray(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteVariantArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            var actual = decoder.ReadVariantArray(null);
            Assert.Equal(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                var result = actual[i];
                if (result.Value is not null && expected[i].Value is null)
                {
                    switch (result.TypeInfo.BuiltInType)
                    {
                        case BuiltInType.String:
                            Assert.Equal(string.Empty, result.Value);
                            return;
                        case BuiltInType.ByteString:
                            Assert.Equal(Array.Empty<byte>(), result.Value);
                            return;
                        case BuiltInType.NodeId:
                            Assert.Equal(NodeId.Null, result.Value);
                            return;
                        case BuiltInType.ExpandedNodeId:
                            Assert.Equal(ExpandedNodeId.Null, result.Value);
                            return;
                        case BuiltInType.QualifiedName:
                            Assert.Equal(QualifiedName.Null, result.Value);
                            return;
                        case BuiltInType.LocalizedText:
                            Assert.Equal(new LocalizedText(string.Empty, string.Empty), result.Value);
                            return;
                        case BuiltInType.ExtensionObject:
                            Assert.Equal(ExtensionObject.Null, result.Value);
                            return;
                        case BuiltInType.DataValue:
                            Assert.Equal(new DataValue(), result.Value);
                            return;
                    }
                }
                Assert.Equal(expected[i].Value, result.Value);
            }
        }

        [Fact]
        public void TestVariantWithArray1()
        {
            var context = new ServiceMessageContext();
            var expected = new Variant(Enumerable.Repeat("test", 3).ToArray());

            var schemas = new AvroBuiltInSchemas();
            var valueSchema = schemas.GetSchemaForBuiltInType(expected.TypeInfo.BuiltInType,
                SchemaRank.Collection);
            var schema = valueSchema.AsNullable().CreateRoot();

            using (var stream = new MemoryStream())
            using (var encoder = new AvroEncoder(stream, schema, context, true))
            {
                encoder.WriteVariant(null, expected);
                stream.Position = 0;
                using var decoder = new AvroDecoder(stream, schema, context);
                var variant = decoder.ReadVariant(null);
                Assert.Equal(expected, variant);
            }

            using (var stream = new MemoryStream())
            using (var encoder = new AvroEncoder(stream, schema, context, true))
            {
                encoder.WriteVariant(null, Variant.Null);
                stream.Position = 0;
                using var decoder = new AvroDecoder(stream, schema, context);
                var variant = decoder.ReadVariant(null);
                Assert.Equal(Variant.Null, variant);
            }
        }

        [Fact]
        public void TestVariantWithArray2()
        {
            var context = new ServiceMessageContext();
            var expected = new Variant(Enumerable.Repeat("test", 3).ToArray());

            var schemas = new AvroBuiltInSchemas();
            var valueSchema = schemas.GetSchemaForBuiltInType(expected.TypeInfo.BuiltInType,
                SchemaRank.Collection);
            var schema = valueSchema.CreateRoot();

            using var stream = new MemoryStream();
            using var encoder = new AvroEncoder(stream, schema, context, true);
            encoder.WriteVariant(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, schema, context);
            var variant = decoder.ReadVariant(null);
            Assert.Equal(expected, variant);
        }

        [Theory]
        [InlineData(DiagnosticsLevel.Advanced)]
        [InlineData(BuiltInType.Int32)]
        public void TestVariantWithEnumeration(object value)
        {
            var context = new ServiceMessageContext();
            var expected = new Variant(value);
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, true);
            builder.WriteVariant(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteVariant(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            var result = decoder.ReadVariant(null);
            Assert.Equal(Convert.ToInt32(value, CultureInfo.InvariantCulture), result.Value);
        }

        [Fact]
        public void TestVariantWithEnumerations()
        {
            var context = new ServiceMessageContext();
            var expected = new Variant(new[] { DiagnosticsLevel.Advanced, DiagnosticsLevel.Advanced });
            using var stream = new MemoryStream();
            using var builder = new AvroSchemaBuilder(stream, context, true, true);
            builder.WriteVariant(null, expected);
            stream.Position = 0;
            using var encoder = new AvroEncoder(stream, builder.Schema, context, true);
            encoder.WriteVariant(null, expected);
            stream.Position = 0;
            using var decoder = new AvroDecoder(stream, builder.Schema, context);
            var json = builder.Schema.ToJson();
            Assert.NotNull(json);
            var result = decoder.ReadVariant(null);
            Assert.Equal(new int[] { 1, 1 }, result);
        }

        private static void AssertEqual(DiagnosticInfo x, DiagnosticInfo y)
        {
            if (x == y)
            {
                return;
            }
            Assert.NotNull(x);
            Assert.NotNull(y);
            Assert.Equal(x.NamespaceUri, y.NamespaceUri);
            Assert.Equal(x.LocalizedText, y.LocalizedText);
            Assert.Equal(x.Locale, y.Locale);
            Assert.Equal(x.AdditionalInfo, y.AdditionalInfo);
            Assert.Equal(x.InnerStatusCode, y.InnerStatusCode);
            Assert.Equal(x.NamespaceUri, y.NamespaceUri);
            AssertEqual(x.InnerDiagnosticInfo, y.InnerDiagnosticInfo);
        }
    }
}
