// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Opc.Ua;
    using Xunit;
    using Newtonsoft.Json.Linq;

    public class VariantEncoderBooleanTests {

        [Fact]
        public void DecodeEncodeBooleanFromJValue() {
            var codec = new JsonVariantEncoder();
            var str = new JValue(true);
            var variant = codec.Decode(str, BuiltInType.Boolean, null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromJArray() {
            var codec = new JsonVariantEncoder();
            var str = new JArray(true, true, false);
            var variant = codec.Decode(str, BuiltInType.Boolean, null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromJValueTypeNull() {
            var codec = new JsonVariantEncoder();
            var str = new JValue(true);
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromJArrayTypeNull() {
            var codec = new JsonVariantEncoder();
            var str = new JArray(true, true, false);
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromString() {
            var codec = new JsonVariantEncoder();
            var str = "true";
            var variant = codec.Decode(str, BuiltInType.Boolean, null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromString() {
            var codec = new JsonVariantEncoder();
            var str = "true, true, false";
            var variant = codec.Decode(str, BuiltInType.Boolean, null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromString2() {
            var codec = new JsonVariantEncoder();
            var str = "[true, true, false]";
            var variant = codec.Decode(str, BuiltInType.Boolean, null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromString3() {
            var codec = new JsonVariantEncoder();
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Boolean, null);
            var expected = new Variant(new bool[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromStringTypeNull() {
            var codec = new JsonVariantEncoder();
            var str = "true";
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(true), encoded);
        }
        [Fact]
        public void DecodeEncodeBooleanArrayFromStringTypeNull1() {
            var codec = new JsonVariantEncoder();
            var str = "true, true, false";
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromStringTypeNull2() {
            var codec = new JsonVariantEncoder();
            var str = "[true, true, false]";
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromStringTypeNullIsNull() {
            var codec = new JsonVariantEncoder();
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeBooleanFromQuotedString() {
            var codec = new JsonVariantEncoder();
            var str = "\"true\"";
            var variant = codec.Decode(str, BuiltInType.Boolean, null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromSinglyQuotedString() {
            var codec = new JsonVariantEncoder();
            var str = "  'true'";
            var variant = codec.Decode(str, BuiltInType.Boolean, null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromQuotedString() {
            var codec = new JsonVariantEncoder();
            var str = "\"true\",'true',\"false\"";
            var variant = codec.Decode(str, BuiltInType.Boolean, null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromQuotedString2() {
            var codec = new JsonVariantEncoder();
            var str = " [\"true\",'true',\"false\"] ";
            var variant = codec.Decode(str, BuiltInType.Boolean, null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonTokenTypeVariant() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "Boolean",
                Body = true
            });
            var variant = codec.Decode(str, BuiltInType.Variant, null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonTokenTypeVariant1() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "Boolean",
                Body = new bool[] { true, true, false }
            });
            var variant = codec.Decode(str, BuiltInType.Variant, null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonTokenTypeVariant2() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "Boolean",
                Body = new bool[0]
            });
            var variant = codec.Decode(str, BuiltInType.Variant, null);
            var expected = new Variant(new bool[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonStringTypeVariant() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "Boolean",
                Body = true
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Variant, null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonStringTypeVariant() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "Boolean",
                Body = new bool[] { true, true, false }
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Variant, null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonTokenTypeNull() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "Boolean",
                Body = true
            });
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonTokenTypeNull1() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                TYPE = "BOOLEAN",
                BODY = new bool[] { true, true, false }
            });
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonTokenTypeNull2() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "Boolean",
                Body = new bool[0]
            });
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(new bool[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonStringTypeNull() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                Type = "boolean",
                Body = true
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonStringTypeNull() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                type = "Boolean",
                body = new bool[] { true, true, false }
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                DataType = "Boolean",
                Value = true
            });
            var variant = codec.Decode(str, BuiltInType.Null, null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonStringTypeVariantMsftEncoding() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                DataType = "Boolean",
                Value = true
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Variant, null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new JsonVariantEncoder();
            var str = JToken.FromObject(new {
                dataType = "Boolean",
                value = new bool[] { true, true, false }
            });
            var variant = codec.Decode(str, BuiltInType.Variant, null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(true, true, false), encoded);
        }
    }
}
