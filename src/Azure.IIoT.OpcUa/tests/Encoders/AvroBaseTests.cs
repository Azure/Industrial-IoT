// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using Xunit;

    /// <summary>
    /// Tests for the Json encoder and decoder class.
    /// </summary>
    public sealed class AvroBaseTests
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
        [InlineData(0.0)]
        [InlineData(Math.PI)]
        [InlineData(double.MaxValue)]
        public void TestDouble(double value)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteDouble(null, value);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteFloat(null, value);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteString(null, value);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteByteString(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
        [InlineData(DiagnosticsLevel.Advanced)]
        [InlineData(BuiltInType.Int32)]
        public void TestVariantWithEnumeration(object value)
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteVariant(null, new Variant(value));
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(Convert.ToInt32(value, CultureInfo.InvariantCulture), decoder.ReadVariant(null).Value);
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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteVariant(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
            context.NamespaceUris.GetIndexOrAppend("test.org");
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

        [Fact]
        public void TestGuid()
        {
            var context = new ServiceMessageContext();
            var expected = Guid.NewGuid();
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteGuid(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadGuid(null));
        }
        [Fact]
        public void TestDateTime()
        {
            var context = new ServiceMessageContext();
            var expected = DateTime.UtcNow;
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteDateTime(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadDateTime(null));
        }
        [Fact]
        public void TestXmlElement()
        {
            var context = new ServiceMessageContext();
            var expected = new XmlDocument();
            expected.LoadXml("<test></test>");
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteXmlElement(null, expected.DocumentElement);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteQualifiedName(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadQualifiedName(null));
        }

        [Fact]
        public void TestLocalizedText()
        {
            var context = new ServiceMessageContext();
            var expected = new LocalizedText("test", "en");
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteLocalizedText(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadLocalizedText(null));
        }

        [Fact]
        public void TestExtensionObject()
        {
            var context = new ServiceMessageContext();
            var expected = new ExtensionObject(new NodeId(1234), new byte[] { 0, 1, 2, 3 });
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteExtensionObject(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteStatusCodeArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteNodeIdArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteExpandedNodeIdArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            var actual = decoder.ReadExpandedNodeIdArray(null);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestInt16()
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteInt16(null, 123);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(123, decoder.ReadInt16(null));
        }

        [Fact]
        public void TestSByte()
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteSByte(null, 123);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(123, decoder.ReadSByte(null));
        }

        [Fact]
        public void TestByte()
        {
            var context = new ServiceMessageContext();
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteByte(null, 123);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(123, decoder.ReadByte(null));
        }

        [Fact]
        public void TestDiagnosticInfo()
        {
            var context = new ServiceMessageContext();
            var expected = new DiagnosticInfo() { AdditionalInfo = "dd" };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteDiagnosticInfo(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            var result = decoder.ReadDiagnosticInfo(null);
            AssertEqual(expected, result);
        }

        [Fact]
        public void TestEnum()
        {
            var context = new ServiceMessageContext();
            const DiagnosticsLevel expected = DiagnosticsLevel.Basic;
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteEnumerated(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            var result = decoder.ReadEnumerated<DiagnosticsLevel>(null);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestEnumArray()
        {
            var context = new ServiceMessageContext();
            var expected = new DiagnosticsLevel[] { DiagnosticsLevel.Basic, DiagnosticsLevel.Advanced };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteEnumeratedArray(null, expected, null);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            var actual = decoder.ReadEnumeratedArray<DiagnosticsLevel>(null);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestDiagnosticInfoArray()
        {
            var context = new ServiceMessageContext();
            var expected = new DiagnosticInfo[] { new() { AdditionalInfo = "dd" }, new() { AdditionalInfo = string.Empty } };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteDiagnosticInfoArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            var actual = decoder.ReadDiagnosticInfoArray(null);
            Assert.Equal(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                var result = actual[i];
                AssertEqual(expected[i], result);
            }
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

        [Fact]
        public void TestBooleanArray()
        {
            var context = new ServiceMessageContext();
            var expected = new bool[] { true, false };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteBooleanArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadBooleanArray(null));
        }

        [Fact]
        public void TestSByteArray()
        {
            var context = new ServiceMessageContext();
            var expected = new sbyte[] { 1, 2, 3 };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteSByteArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadSByteArray(null));
        }

        [Fact]
        public void TestByteArray()
        {
            var context = new ServiceMessageContext();
            var expected = new byte[] { 1, 2, 3 };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteByteArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadByteArray(null));
        }

        [Fact]
        public void TestInt16Array()
        {
            var context = new ServiceMessageContext();
            var expected = new short[] { 1, 2, 3 };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteInt16Array(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadInt16Array(null));
        }

        [Fact]
        public void TestUInt16Array()
        {
            var context = new ServiceMessageContext();
            var expected = new ushort[] { 1, 2, 3 };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteUInt16Array(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadUInt16Array(null));
        }

        [Fact]
        public void TestInt32Array()
        {
            var context = new ServiceMessageContext();
            var expected = new int[] { 1, 2, 3 };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteInt32Array(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadInt32Array(null));
        }

        [Fact]
        public void TestUInt32Array()
        {
            var context = new ServiceMessageContext();
            var expected = new uint[] { 1, 2, 3 };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteUInt32Array(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadUInt32Array(null));
        }

        [Fact]
        public void TestInt64Array()
        {
            var context = new ServiceMessageContext();
            var expected = new long[] { 1, 2, 3 };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteInt64Array(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadInt64Array(null));
        }

        [Fact]
        public void TestUInt64Array()
        {
            var context = new ServiceMessageContext();
            var expected = new ulong[] { 1, 2, 3 };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteUInt64Array(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadUInt64Array(null));
        }

        [Fact]
        public void TestFloatArray()
        {
            var context = new ServiceMessageContext();
            var expected = new float[] { 1, 2, 3 };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteFloatArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadFloatArray(null));
        }

        [Fact]
        public void TestDoubleArray()
        {
            var context = new ServiceMessageContext();
            var expected = new double[] { 1, 2, 3 };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteDoubleArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadDoubleArray(null));
        }

        [Fact]
        public void TestStringArray()
        {
            var context = new ServiceMessageContext();
            var expected = new string[] { "1", "2", "3" };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteStringArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadStringArray(null));
        }

        [Fact]
        public void TestDateTimeArray()
        {
            var context = new ServiceMessageContext();
            var expected = new DateTime[] { DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteDateTimeArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadDateTimeArray(null));
        }

        [Fact]
        public void TestGuidArray()
        {
            var context = new ServiceMessageContext();
            var expected = new Uuid[] { (Uuid)Guid.NewGuid(), (Uuid)Guid.NewGuid(), (Uuid)Guid.NewGuid() };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteGuidArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadGuidArray(null).ToArray());
        }

        [Fact]
        public void TestXmlElementArray()
        {
            var context = new ServiceMessageContext();
            var expected = new XmlDocument();
            expected.LoadXml("<test></test>");
            var expectedArray = new XmlElement[] { expected.DocumentElement, expected.DocumentElement };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteXmlElementArray(null, expectedArray);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
        public void TestQualifiedNameArray()
        {
            var context = new ServiceMessageContext();
            var ns = context.NamespaceUris.GetIndexOrAppend("test.org");
            var expected = new QualifiedName[] { new("test", ns), new("test", ns) };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteQualifiedNameArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadQualifiedNameArray(null));
        }

        [Fact]
        public void TestLocalizedTextArray1()
        {
            var context = new ServiceMessageContext();
            var expected = new LocalizedText[] { new("test", "en"), new("test", "en") };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteLocalizedTextArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadLocalizedTextArray(null));
        }

        [Fact]
        public void TestLocalizedTextArray2()
        {
            var context = new ServiceMessageContext();
            var expected = new LocalizedText[] { new("test", "en"), new("test", "en") };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteArray(null, expected, ValueRanks.OneDimension, BuiltInType.LocalizedText);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            var result = decoder.ReadArray(null, ValueRanks.OneDimension, BuiltInType.LocalizedText);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestExtensionObjectArray()
        {
            var context = new ServiceMessageContext();
            var expected = new ExtensionObject[] { new(new NodeId(1234), new byte[] { 0, 1, 2, 3 }), new(new NodeId(1234), new byte[] { 0, 1, 2, 3 }) };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteExtensionObjectArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            var actual = decoder.ReadExtensionObjectArray(null);
            Assert.Equal(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].TypeId, actual[i].TypeId);
                Assert.Equal(expected[i].Body, actual[i].Body);
            }
        }

        [Fact]
        public void TestDataValueArray()
        {
            var context = new ServiceMessageContext();
            var expected = new DataValue[] { new(), new() };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteDataValueArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            var actual = decoder.ReadDataValueArray(null);
            Assert.Equal(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        [Fact]
        public void TestVariantArray()
        {
            var context = new ServiceMessageContext();
            var expected = new Variant[] { new(123), new("test") };
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteVariantArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
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
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteVariantArray(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            Assert.Equal(expected, decoder.ReadVariantArray(null));
        }

        [Fact]
        public void TestVariantWithEnumerations()
        {
            var context = new ServiceMessageContext();
            var expected = new Variant(new[] { DiagnosticsLevel.Advanced, DiagnosticsLevel.Advanced });
            using var stream = new MemoryStream();
            using var encoder = new SchemalessAvroEncoder(stream, context, true);
            encoder.WriteVariant(null, expected);
            stream.Position = 0;
            using var decoder = new SchemalessAvroDecoder(stream, context);
            var result = decoder.ReadVariant(null).Value;
            Assert.Equal(new int[] { 1, 1 }, result);
        }
    }
}
