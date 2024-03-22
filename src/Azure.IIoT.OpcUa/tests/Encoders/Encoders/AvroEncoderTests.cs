// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using Xunit;

    /// <summary>
    /// Tests for the Json encoder and decoder class.
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
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteBoolean(null, value);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
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
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteInt64(null, value);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
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
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteDouble(null, value);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
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
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteFloat(null, value);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
            Assert.Equal(value, decoder.ReadFloat(null));
        }

        [Theory]
        [InlineData("test")]
        [InlineData("12345")]
        [InlineData("12345678901234567890123456789012345678901234567890123456789012345678901234567890")]
        public void TestString(string value)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteString(null, value);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
            Assert.Equal(value, decoder.ReadString(null));
        }

        [Theory]
        [InlineData("test")]
        [InlineData("12345")]
        public void TestByteString(string value)
        {
            var context = new ServiceMessageContext();
            var expected = Encoding.UTF8.GetBytes(value);
            using var stream = new MemoryStream();
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteByteString(null, expected);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
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
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteUInt64(null, value);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
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
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteStatusCode(null, value);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
            Assert.Equal(value, decoder.ReadStatusCode(null));
        }

        [Theory]
        [InlineData(StatusCodes.Good)]
        [InlineData("test")]
        [InlineData(12345)]
        public void TestVariant(object value)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteVariant(null, new Variant(value));
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
            Assert.Equal(value, decoder.ReadVariant(null).Value);
        }

        [Theory]
        [InlineData("test")]
        [InlineData(12345u)]
        public void TestNodeId(object value)
        {
            var context = new ServiceMessageContext();
            var ns = context.NamespaceUris.GetIndexOrAppend("test.org");
            using var stream = new MemoryStream();
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            var expected = new NodeId(value, ns);
            encoder.WriteNodeId(null, expected);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadNodeId(null));
        }

        [Theory]
        [InlineData("test")]
        [InlineData(12345u)]
        public void TestExpandedNodeId(object value)
        {
            var context = new ServiceMessageContext();
            var ns = context.NamespaceUris.GetIndexOrAppend("test.org");
            var srv = context.ServerUris.GetIndexOrAppend("Super");
            using var stream = new MemoryStream();
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            var expected = new ExpandedNodeId(value, 0, "test.org", srv);
            encoder.WriteExpandedNodeId(null, expected);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadExpandedNodeId(null));
        }

        [Fact]
        public void TestDataValue()
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            var expected = new DataValue();
            encoder.WriteDataValue(null, expected);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadDataValue(null));
        }

        [Fact]
        public void TestDataValueNull()
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteDataValue(null, null);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
            Assert.True(Opc.Ua.Utils.IsEqual(new DataValue(),
                decoder.ReadDataValue(null)));
        }

        [Fact]
        public void TestGuid()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            using var stream = new MemoryStream();
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteGuid(null, expected);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadGuid(null));
        }
        [Fact]
        public void TestDateTime()
        {
            var context = new ServiceMessageContext();
            var expected = DateTime.UtcNow;
            using var stream = new MemoryStream();
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteDateTime(null, expected);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadDateTime(null));
        }
        [Fact]
        public void TestXmlElement()
        {
            var context = new ServiceMessageContext();
            var expected = new XmlDocument();
            expected.LoadXml("<test></test>");
            using var stream = new MemoryStream();
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteXmlElement(null, expected.DocumentElement);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
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
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteQualifiedName(null, expected);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadQualifiedName(null));
        }

        [Fact]
        public void TestLocalizedText()
        {
            var context = new ServiceMessageContext();
            var expected = new LocalizedText("test", "en");
            using var stream = new MemoryStream();
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteLocalizedText(null, expected);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadLocalizedText(null));
        }

        [Fact]
        public void TestExtensionObject()
        {
            var context = new ServiceMessageContext();
            var expected = new ExtensionObject(new NodeId(1234), new byte[] { 0, 1, 2, 3 });
            using var stream = new MemoryStream();
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteExtensionObject(null, expected);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
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
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteStatusCodeArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
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
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteNodeIdArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
            var actual = decoder.ReadNodeIdArray(null);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestExpandedNodeIdArray()
        {
            var context = new ServiceMessageContext();
            var ns = context.NamespaceUris.GetIndexOrAppend("test.org");
            var srv = context.ServerUris.GetIndexOrAppend("Super");
            var expected = new ExpandedNodeId[] { new (123u, 0, "test.org", srv), new (456u, 0, "test.org", srv) };
            using var stream = new MemoryStream();
            using var encoder = new AvroSchemalessEncoder(stream, context, true);
            encoder.WriteExpandedNodeIdArray(null, expected);
            stream.Position = 0;
            using var decoder = new AvroSchemalessDecoder(stream, context);
            var actual = decoder.ReadExpandedNodeIdArray(null);
            Assert.Equal(expected, actual);
        }
    }
}
