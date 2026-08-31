// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Opc.Ua;
    using System.Text.Json.Nodes;
    using Xunit;

    public class VariantEncoderSByteTests
    {
        [Fact]
        public void DecodeEncodeSByteFromJValue()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(-123);
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(str, encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromJArray()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125);
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(str, encoded));
        }

        [Fact]
        public void DecodeEncodeSByteFromJValueTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(-123);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromJArrayTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(str, encoded));
        }

        [Fact]
        public void DecodeEncodeSByteFromString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "-123";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "-123, -124, -125";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromString2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromString3()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[]";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant(System.Array.Empty<sbyte>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteFromStringTypeIntegerIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "-123";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteFromStringTypeNumberIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "-123";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteFromStringTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "-123";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123), encoded));
        }
        [Fact]
        public void DecodeEncodeSByteArrayFromStringTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "-123, -124, -125";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromStringTypeNullIsInt642()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromStringTypeNullIsNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Null(encoded);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeSByteFromQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "\"-123\"";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteFromSinglyQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "  '-123'";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "\"-123\",'-124',\"-125\"";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromQuotedString2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = " [\"-123\",'-124',\"-125\"] ";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteFromVariantJsonTokenTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                Type = "SByte",
                Body = -123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromVariantJsonTokenTypeVariant1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                Type = "SByte",
                Body = new sbyte[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromVariantJsonTokenTypeVariant2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                Type = "SByte",
                Body = System.Array.Empty<sbyte>()
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(System.Array.Empty<sbyte>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteFromVariantJsonStringTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                Type = "SByte",
                Body = -123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromVariantJsonStringTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                Type = "SByte",
                Body = new sbyte[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteFromVariantJsonTokenTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                Type = "SByte",
                Body = (sbyte)-123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromVariantJsonTokenTypeNull1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                TYPE = "SBYTE",
                BODY = new sbyte[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromVariantJsonTokenTypeNull2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                Type = "SByte",
                Body = System.Array.Empty<sbyte>()
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(System.Array.Empty<sbyte>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteFromVariantJsonStringTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                Type = "sbyte",
                Body = (sbyte)-123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromVariantJsonStringTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                type = "SByte",
                body = new sbyte[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteFromVariantJsonTokenTypeNullMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                DataType = "SByte",
                Value = -123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteFromVariantJsonStringTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                DataType = "SByte",
                Value = (sbyte)-123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123), encoded));
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromVariantJsonTokenTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                dataType = "SByte",
                value = new sbyte[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded));
        }

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
    }
}
