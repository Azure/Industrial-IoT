// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System.Xml;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using Xunit;

    public class VariantEncoderMiscTests
    {
        [Fact]
        public void DecodeEncodeStringAsUInt32()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "123";
            var variant = codec.Decode(str, BuiltInType.UInt32);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(str), encoded));
        }

        [Fact]
        public void DecodeEncodeStringAsInt32()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "-1";
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant(-1);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(str), encoded));
        }

        [Fact]
        public void DecodeEncodeStringAsSbyte()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "-12";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant((sbyte)-12);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(str), encoded));
        }

        [Fact]
        public void DecodeEncodeStringAsByte()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "1";
            var variant = codec.Decode(str, BuiltInType.Byte);
            var expected = new Variant((byte)1);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(str), encoded));
        }

        [Fact]
        public void DecodeEncodeString1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "\\\"fffffffff\\\"";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(str);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(str, encoded));
        }

        [Fact]
        public void DecodeEncodeString2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "fffffffff";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(str);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(str, encoded));
        }

        [Fact]
        public void DecodeEncodeString3()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "\"fffffffff\"";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant("fffffffff");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals("fffffffff", encoded));
        }

        [Fact]
        public void DecodeEncodeIntArray1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "1,2,3,4,5,6";
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant([1, 2, 3, 4, 5, 6]);
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeIntArray2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[1,2,3,4,5,6]";
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant([1, 2, 3, 4, 5, 6]);
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeStringArray()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "\"test1\", \"test2\"";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(["test1", "test2"]);
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEmptyStringArray()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[]";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(System.Array.Empty<string>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(encoded, JsonNode.Parse(str)));
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(str), encoded));
        }

        [Fact]
        public void DecodeEmptyShortArray()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[]";
            var variant = codec.Decode(str, BuiltInType.Int16);
            var expected = new Variant(System.Array.Empty<short>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(encoded, JsonNode.Parse(str)));
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(str), encoded));
        }

        [Fact]
        public void EncodeDecodeXmlElement()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var doc = new XmlDocument();
            doc.LoadXml(
          """
<?xml version="1.0" encoding="UTF-8"?>
            <note>
                <to>Tove</to>
                <from>Jani</from>
                <heading test="1.0">Reminder</heading>
                <author><nothing/></author>
                <body>Don't forget me this weekend!</body>
            </note>
"""
            );
            var expected = new Variant(doc.DocumentElement);
            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.XmlElement);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeLocalizedText()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var expected = new Variant(new LocalizedText("en-US", "text"));
            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.LocalizedText);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeLocalizedTextFromString2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "text";
            var expected = new Variant(new LocalizedText("text"));
            var variant = codec.Decode(str, BuiltInType.LocalizedText);
            var encoded = codec.Encode(expected);
            Assert.NotNull(encoded);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeNodeId()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());

            var expected = new Variant(new NodeId(2354));

            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.NodeId);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeId1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());

            var expected = new Variant(new ExpandedNodeId(2354u, 0, "http://test.org/test", 0));

            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.ExpandedNodeId);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeId2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());

            var expected = new Variant(new ExpandedNodeId(2354u, 0, "http://test/", 0));

            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.ExpandedNodeId);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeId3()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());

            var expected1 = new Variant(new ExpandedNodeId(2354u, 0, "http://test/", 0));
            var expected2 = new Variant(new ExpandedNodeId(2354u, 0, "http://test/UA", 0));
            var expected3 = new Variant(new ExpandedNodeId(2355u, 0, "http://test/", 0));
            var expected4 = new Variant(new ExpandedNodeId(2355u, 0, null, 0));
            var expected5 = new Variant(new ExpandedNodeId(new NodeId(2355u, 1), "http://test/", 0));

            var encoded1 = codec.Encode(expected1);
            var encoded2 = codec.Encode(expected2);
            var encoded3 = codec.Encode(expected3);
            var encoded4 = codec.Encode(expected4);
            var encoded5 = codec.Encode(expected5);

            var variant1 = codec.Decode(encoded1, BuiltInType.ExpandedNodeId);
            var variant2 = codec.Decode(encoded2, BuiltInType.ExpandedNodeId);
            var variant3 = codec.Decode(encoded3, BuiltInType.ExpandedNodeId);
            var variant4 = codec.Decode(encoded4, BuiltInType.ExpandedNodeId);
            var variant5 = codec.Decode(encoded5, BuiltInType.ExpandedNodeId);

            Assert.Equal(expected1, variant1);
            Assert.Equal(expected2, variant2);
            Assert.Equal(expected3, variant3);
            Assert.Equal(expected4, variant4);
            Assert.Equal(expected5, variant5);
        }

        [Fact]
        public void EncodeDefaultsToReversibleEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var value = new Variant(42);

            var defaulted = codec.Encode(value, out var defaultedType);
            var reversible = codec.Encode(value, out var reversibleType, ValueEncoding.Reversible);

            Assert.Equal(reversibleType, defaultedType);
            Assert.Equal((int)reversible, (int)defaulted);
        }

        [Fact]
        public void EncodeNullVariantReturnsNullAndNullBuiltInType()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());

            var encoded = codec.Encode(null, out var builtInType);

            Assert.Null(encoded);
            Assert.Equal(BuiltInType.Null, builtInType);
        }

        [Theory]
        [InlineData(9223372036854775807L)]
        [InlineData(-9223372036854775808L)]
        public void EncodeInt64KeepsJsonNumberContract(long value)
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());

            var encoded = codec.Encode(new Variant(value), out var builtInType);

            Assert.Equal(BuiltInType.Int64, builtInType);
            Assert.NotNull(encoded);
            var jsonValue = Assert.IsType<JsonValue>(encoded);
            Assert.Equal(JsonValueKind.Number, jsonValue.GetValueKind());
            Assert.Equal(value, jsonValue.GetValue<long>());
        }

        [Fact]
        public void EncodeUInt64ArrayKeepsJsonNumberContract()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());

            var encoded = codec.Encode(new Variant(new ulong[] { 1, ulong.MaxValue }),
                out var builtInType);

            var array = Assert.IsType<JsonArray>(encoded);
            Assert.Equal(BuiltInType.UInt64, builtInType);
            Assert.Equal(1ul, array[0]!.GetValue<ulong>());
            Assert.Equal(ulong.MaxValue, array[1]!.GetValue<ulong>());
        }

        [Fact]
        public void EncodeNonReversibleReturnsBodyWithoutVariantEnvelope()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());

            var encoded = codec.Encode(new Variant(42), out var builtInType,
                ValueEncoding.NonReversible);

            Assert.Equal(BuiltInType.Int32, builtInType);
            Assert.NotNull(encoded);
            Assert.Equal(42, encoded!.GetValue<int>());
        }

        [Theory]
        [InlineData("""{"Type":"Int64","Body":9223372036854775807}""",
            9223372036854775807L)]
        [InlineData("""{"DataType":"Int64","Value":"9223372036854775807"}""",
            9223372036854775807L)]
        [InlineData("""{"type":8,"body":-12}""", -12L)]
        [InlineData("""{"TYPE":"8","BODY":"-12"}""", -12L)]
        public void DecodeAcceptsLegacyVariantEnvelopeShapes(string json, long expected)
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());

            var variant = codec.Decode(JsonNode.Parse(json), BuiltInType.Variant);

            Assert.Equal(new Variant(expected), variant);
        }

        [Fact]
        public void DecodeLeavesAlreadyNormalizedVariantEnvelopeToDecoder()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var json = JsonNode.Parse("""{"UaType":6,"Value":123}""");

            var variant = codec.Decode(json, BuiltInType.Variant);

            Assert.Equal(new Variant(123), variant);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void DecodeBareBooleanDefaultTypesVariant(string json, bool expected)
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());

            var variant = codec.Decode(JsonNode.Parse(json), BuiltInType.Variant);

            Assert.Equal(new Variant(expected), variant);
        }

        [Fact]
        public void DecodeBareStringArrayDefaultTypesVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());

            var variant = codec.Decode(JsonNode.Parse("""["a","b"]"""),
                BuiltInType.Variant);

            Assert.Equal(new Variant(new[] { "a", "b" }), variant);
        }
    }
}
