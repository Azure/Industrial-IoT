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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteBoolean(null, value);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteInt64(null, value);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(value, decoder.ReadInt64(null));
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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteUInt64(null, value);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteStatusCode(null, value);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteVariant(null, new Variant(value));
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            var expected = new NodeId(value, ns);
            encoder.WriteNodeId(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            var expected = new ExpandedNodeId(value, 0, "test.org", srv);
            encoder.WriteExpandedNodeId(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadExpandedNodeId(null));
        }

        [Fact]
        public void TestDataValue()
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            var expected = new DataValue();
            encoder.WriteDataValue(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadDataValue(null));
        }

        [Fact]
        public void TestDataValueNull()
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteDataValue(null, null);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.True(Opc.Ua.Utils.IsEqual(new DataValue(),
                decoder.ReadDataValue(null)));
        }
    }
}
