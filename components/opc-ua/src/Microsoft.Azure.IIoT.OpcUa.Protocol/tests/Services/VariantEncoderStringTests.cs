// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using Xunit;

    public class VariantEncoderStringTests {

        [Fact]
        public void DecodeEncodeStringFromJValue() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject("");
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant("");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromJArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray("", "", "");
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(new string[] { "", "", "" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeStringFromJValueTypeNullIsString() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject("");
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant("");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(""), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromJArrayTypeNullIsString() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray("", "", "");
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new string[] { "", "", "" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeStringFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant("123");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject("123"), encoded);
        }

        [Fact]
        public void DecodeEncodeStringFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123, 124, 125";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant("123, 124, 125");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeStringFromString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[test, test, test]";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(str);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[123, 124, 125]";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(new string[] { "123", "124", "125" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray("123", "124", "125"), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromString4() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(System.Array.Empty<string>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeStringFromStringTypeNullIsString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "test";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant("test");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject("test"), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromStringTypeNullIsString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "test, test, test";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new string[] { "test", "test", "test" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray("test", "test", "test"), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromStringTypeNullIsString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[\"test\", \"test\", \"test\"]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new string[] { "test", "test", "test" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray("test", "test", "test"), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromStringTypeNullIsString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[test, test, test]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new string[] { "[test", "test", "test]" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray("[test", "test", "test]"), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromStringTypeNullIsNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeStringFromQuotedString1() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"test\"";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant("test");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject("test"), encoded);
        }

        [Fact]
        public void DecodeEncodeStringFromQuotedString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"\\\"test\\\"\"";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant("\"test\"");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject("\"test\""), encoded);
        }

        [Fact]
        public void DecodeEncodeStringFromSinglyQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "  'test'";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant("test");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject("test"), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromQuotedString1() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"test\",'test',\"test\"";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(new string[] { "test", "test", "test" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray("test", "test", "test"), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromQuotedString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = " [\"test\",'test',\"test\"] ";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(new string[] { "test", "test", "test" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray("test", "test", "test"), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromQuotedString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = " [\"\\\"test\\\"\",'\\\"test\\\"',\"\\\"test\\\"\"] ";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(new string[] { "test", "test", "test" });
            // TODO: var expected = new Variant(new string[] { "\"test\"", "\"test\"", "\"test\"" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            // TODO: Assert.Equal(_serializer.FromArray("\"test\"", "\"test\"", "\"test\""), encoded);
            Assert.Equal(_serializer.FromArray("test", "test", "test"), encoded);
        }

        [Fact]
        public void DecodeEncodeStringFromVariantJsonTokenTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "String",
                Body = ""
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant("");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(""), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromVariantJsonTokenTypeVariant1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "String",
                Body = new string[] { "", "", "" }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new string[] { "", "", "" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray("", "", ""), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromVariantJsonTokenTypeVariant2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "String",
                Body = System.Array.Empty<string>()
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(System.Array.Empty<string>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeStringFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "String",
                Body = ""
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant("");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(""), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "String",
                Body = new string[] { "", "", "" }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new string[] { "", "", "" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray("", "", ""), encoded);
        }

        [Fact]
        public void DecodeEncodeStringFromVariantJsonTokenTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "String",
                Body = ""
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant("");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(""), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromVariantJsonTokenTypeNull1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                TYPE = "STRING",
                BODY = new string[] { "", "", "" }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new string[] { "", "", "" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray("", "", ""), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromVariantJsonTokenTypeNull2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "String",
                Body = System.Array.Empty<string>()
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(System.Array.Empty<string>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeStringFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "string",
                Body = ""
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant("");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(""), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "String",
                body = new string[] { "", "", "" }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new string[] { "", "", "" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray("", "", ""), encoded);
        }

        [Fact]
        public void DecodeEncodeStringFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                DataType = "String",
                Value = ""
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant("");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(""), encoded);
        }

        [Fact]
        public void DecodeEncodeStringFromVariantJsonStringTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                DataType = "String",
                Value = ""
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant("");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(""), encoded);
        }

        [Fact]
        public void DecodeEncodeStringArrayFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                dataType = "String",
                value = new string[] { "", "", "" }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new string[] { "", "", "" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray("", "", ""), encoded);
        }

        [Fact]
        public void DecodeEncodeStringMatrixFromStringJsonTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new string[,,] {
                { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new string[,,] {
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeStringMatrixFromStringJsonTypeString() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new string[,,] {
                { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } }
            });
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(new string[,,] {
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeStringMatrixFromVariantJsonTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "String",
                body = new string[,,] {
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new string[,,] {
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeStringMatrixFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "String",
                value = new string[,,] {
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new string[,,] {
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeStringMatrixFromVariantJsonTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "String",
                body = new string[,,] {
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new string[,,] {
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeStringMatrixFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "String",
                value = new string[,,] {
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new string[,,] {
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } },
                    { { "test", "zhf", "33" }, { "test", "zhf", "33" }, { "test", "zhf", "33" } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        private readonly IJsonSerializer _serializer = new NewtonSoftJsonSerializer();
    }
}
