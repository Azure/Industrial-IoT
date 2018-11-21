// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Opc.Ua;
    using Xunit;
    using Newtonsoft.Json.Linq;

    public class VariantEncoderUInt64Tests {

        [Fact]
        public void DecodeEncodeUInt64FromJValue() {
            var codec = new JsonVariantEncoder();
            var str = new JValue(123Lu);
            var variant = codec.Decode(str, BuiltInType.UInt64, null);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromJArray() {
            var codec = new JsonVariantEncoder();
            var str = new JArray(123Lu, 124Lu, 125Lu);
            var variant = codec.Decode(str, BuiltInType.UInt64, null);
            var expected = new Variant(new ulong[] { 123Lu, 124Lu, 125Lu });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromJValueTypeNullIsInt64() {
            var codec = new JsonVariantEncoder();
            var str = new JValue(123Lu);
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromJArrayTypeNullIsInt64() {
            var codec = new JsonVariantEncoder();
            var str = new JArray(123Lu, 124Lu, 125Lu);
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(new long[] { 123L, 124L, 125L });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromString() {
            var codec = new JsonVariantEncoder();
            var str = "123";
            var variant = codec.Decode(str, BuiltInType.UInt64, null);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromString() {
            var codec = new JsonVariantEncoder();
            var str = "123, 124, 125";
            var variant = codec.Decode(str, BuiltInType.UInt64, null);
            var expected = new Variant(new ulong[] { 123Lu, 124Lu, 125Lu });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromString2() {
            var codec = new JsonVariantEncoder();
            var str = "[123, 124, 125]";
            var variant = codec.Decode(str, BuiltInType.UInt64, null);
            var expected = new Variant(new ulong[] { 123Lu, 124Lu, 125Lu });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromString3() {
            var codec = new JsonVariantEncoder();
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.UInt64, null);
            var expected = new Variant(new ulong[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromStringTypeIntegerIsInt64() {
            var codec = new JsonVariantEncoder();
            var str = "123";
            var variant = codec.Decode(str, BuiltInType.Integer, null);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromStringTypeIntegerIsInt641() {
            var codec = new JsonVariantEncoder();
            var str = "[123, 124, 125]";
            var variant = codec.Decode(str, BuiltInType.Integer, null);
            var expected = new Variant(new Variant[] {
                new Variant(123L), new Variant(124L), new Variant(125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromStringTypeIntegerIsInt642() {
            var codec = new JsonVariantEncoder();
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Integer, null);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromStringTypeNumberIsInt64() {
            var codec = new JsonVariantEncoder();
            var str = "123";
            var variant = codec.Decode(str, BuiltInType.Number, null);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromStringTypeNumberIsInt641() {
            var codec = new JsonVariantEncoder();
            var str = "[123, 124, 125]";
            var variant = codec.Decode(str, BuiltInType.Number, null);
            var expected = new Variant(new Variant[] {
                new Variant(123L), new Variant(124L), new Variant(125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromStringTypeNumberIsInt642() {
            var codec = new JsonVariantEncoder();
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Number, null);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromStringTypeNullIsInt64() {
            var codec = new JsonVariantEncoder();
            var str = "123";
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(123Lu), encoded);
        }
        [Fact]
        public void DecodeEncodeUInt64ArrayFromStringTypeNullIsInt64() {
            var codec = new JsonVariantEncoder();
            var str = "123, 124, 125";
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(new long[] { 123L, 124L, 125L });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromStringTypeNullIsInt642() {
            var codec = new JsonVariantEncoder();
            var str = "[123, 124, 125]";
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(new long[] { 123L, 124L, 125L });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromStringTypeNullIsNull() {
            var codec = new JsonVariantEncoder();
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeUInt64FromQuotedString() {
            var codec = new JsonVariantEncoder();
            var str = "\"123\"";
            var variant = codec.Decode(str, BuiltInType.UInt64, null);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromSinglyQuotedString() {
            var codec = new JsonVariantEncoder();
            var str = "  '123'";
            var variant = codec.Decode(str, BuiltInType.UInt64, null);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromQuotedString() {
            var codec = new JsonVariantEncoder();
            var str = "\"123\",'124',\"125\"";
            var variant = codec.Decode(str, BuiltInType.UInt64, null);
            var expected = new Variant(new ulong[] { 123Lu, 124Lu, 125Lu });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromQuotedString2() {
            var codec = new JsonVariantEncoder();
            var str = " [\"123\",'124',\"125\"] ";
            var variant = codec.Decode(str, BuiltInType.UInt64, null);
            var expected = new Variant(new ulong[] { 123Lu, 124Lu, 125Lu });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromVariantJsonTokenTypeVariant() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "UInt64",
                Body = 123Lu
            });
            var variant = codec.Decode(str, BuiltInType.Variant, null);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromVariantJsonTokenTypeVariant1() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "UInt64",
                Body = new ulong[] { 123Lu, 124Lu, 125Lu }
            });
            var variant = codec.Decode(str, BuiltInType.Variant, null);
            var expected = new Variant(new ulong[] { 123Lu, 124Lu, 125Lu });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromVariantJsonTokenTypeVariant2() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "UInt64",
                Body = new ulong[0]
            });
            var variant = codec.Decode(str, BuiltInType.Variant, null);
            var expected = new Variant(new ulong[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromVariantJsonStringTypeVariant() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "UInt64",
                Body = 123Lu
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Variant, null);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromVariantJsonStringTypeVariant() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "UInt64",
                Body = new ulong[] { 123Lu, 124Lu, 125Lu }
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Variant, null);
            var expected = new Variant(new ulong[] { 123Lu, 124Lu, 125Lu });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromVariantJsonTokenTypeNull() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "UInt64",
                Body = 123Lu
            });
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromVariantJsonTokenTypeNull1() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                TYPE = "UINT64",
                BODY = new ulong[] { 123Lu, 124Lu, 125Lu }
            });
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(new ulong[] { 123Lu, 124Lu, 125Lu });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromVariantJsonTokenTypeNull2() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "UInt64",
                Body = new ulong[0]
            });
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(new ulong[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromVariantJsonStringTypeNull() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "uint64",
                Body = 123Lu
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromVariantJsonStringTypeNull() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                type = "UInt64",
                body = new ulong[] { 123Lu, 124Lu, 125Lu }
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(new ulong[] { 123Lu, 124Lu, 125Lu });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                DataType = "UInt64",
                Value = 123Lu
            });
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromVariantJsonStringTypeVariantMsftEncoding() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                DataType = "UInt64",
                Value = 123Lu
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Variant, null);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                dataType = "UInt64",
                value = new ulong[] { 123Lu, 124Lu, 125Lu }
            });
            var variant = codec.Decode(str, BuiltInType.Variant, null);
            var expected = new Variant(new ulong[] { 123Lu, 124Lu, 125Lu });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(123Lu, 124Lu, 125Lu), encoded);
        }
    }
}
