// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Models
{
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System.IO;
    using System.Text.Json.Nodes;
    using Xunit;

    public sealed class EncodeableVariantValueTests
    {
        [Fact]
        public void EncodingIds_ReturnDeclaredIds()
        {
            var value = new EncodeableVariantValue();

            Assert.Equal((ExpandedNodeId)"s=EncodeableVariantValue", value.TypeId);
            Assert.Equal((ExpandedNodeId)"s=EncodeableVariantValue_Encoding_DefaultBinary",
                value.BinaryEncodingId);
            Assert.Equal((ExpandedNodeId)"s=EncodeableVariantValue_Encoding_DefaultXml",
                value.XmlEncodingId);
            Assert.Equal((ExpandedNodeId)"s=EncodeableVariantValue_Encoding_DefaultJson",
                value.JsonEncodingId);
        }

        [Fact]
        public void EncodeDecode_Binary_RoundTripsJsonValue()
        {
            var context = new ServiceMessageContext();
            var expected = new EncodeableVariantValue(JsonNode.Parse("""{"value":42}"""));
            var encoded = expected.AsBinary(context);
            var actual = new EncodeableVariantValue();

            using (var stream = new MemoryStream(encoded))
            using (var decoder = new BinaryDecoder(stream, context, true))
            {
                actual.Decode(decoder);
            }

            Assert.True(expected.IsEqual(actual));
            Assert.Equal("""{"value":42}""", actual.Value?.ToJsonString());
        }

        [Fact]
        public void EncodeDecode_Json_RoundTripsJsonValue()
        {
            var context = new ServiceMessageContext();
            var expected = new EncodeableVariantValue(JsonNode.Parse("""[1,2,3]"""));
            var encoded = expected.AsJson(context);
            var actual = new EncodeableVariantValue();

            using (var decoder = new JsonDecoder(encoded, context))
            {
                actual.Decode(decoder);
            }

            Assert.True(expected.IsEqual(actual));
            Assert.Equal("""[1,2,3]""", actual.Value?.ToJsonString());
        }

        [Fact]
        public void EncodeDecode_NullValue_RoundTripsNull()
        {
            var context = new ServiceMessageContext();
            var expected = new EncodeableVariantValue();
            var encoded = expected.AsBinary(context);
            var actual = new EncodeableVariantValue(JsonNode.Parse("1"));

            using (var stream = new MemoryStream(encoded))
            using (var decoder = new BinaryDecoder(stream, context, true))
            {
                actual.Decode(decoder);
            }

            Assert.Null(actual.Value);
            Assert.True(expected.IsEqual(actual));
        }

        [Fact]
        public void IsEqual_NonWrapper_ReturnsFalse()
        {
            var value = new EncodeableVariantValue(JsonNode.Parse("1"));

            Assert.False(value.IsEqual(new KeyDataValuePair()));
        }

        [Fact]
        public void IsEqual_DifferentJson_ReturnsFalse()
        {
            var left = new EncodeableVariantValue(JsonNode.Parse("""{"value":1}"""));
            var right = new EncodeableVariantValue(JsonNode.Parse("""{"value":2}"""));

            Assert.False(left.IsEqual(right));
        }

        [Fact]
        public void Clone_DeepClonesJsonValue()
        {
            var source = new EncodeableVariantValue(JsonNode.Parse("""{"value":1}"""));

            var clone = Assert.IsType<EncodeableVariantValue>(source.Clone());
            var sourceObject = Assert.IsType<JsonObject>(source.Value);
            sourceObject["value"] = 2;

            Assert.NotSame(source, clone);
            Assert.Equal("""{"value":1}""", clone.Value?.ToJsonString());
            Assert.False(source.IsEqual(clone));
        }
    }
}
