// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Opc.Ua;
    using Xunit;

    public class VariantEncoderBooleanTests {

        [Fact]
        public void DecodeEncodeBooleanFromJValue() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(true);
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromJArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray(true, true, false);
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromJValueTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str =  _serializer.FromObject(true);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromJArrayTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str =  _serializer.FromArray(true, true, false);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "true";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str =  "true, true, false";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[true, true, false]";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(System.Array.Empty<bool>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = "true";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(true), encoded);
        }
        [Fact]
        public void DecodeEncodeBooleanArrayFromStringTypeNull1() {
            var codec = new VariantEncoderFactory().Default;
            var str = "true, true, false";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromStringTypeNull2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[true, true, false]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromStringTypeNullIsNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeBooleanFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"true\"";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromSinglyQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "  'true'";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"true\",'true',\"false\"";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromQuotedString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = " [\"true\",'true',\"false\"] ";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonTokenTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Boolean",
                Body = true
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonTokenTypeVariant1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Boolean",
                Body = new bool[] { true, true, false }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonTokenTypeVariant2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Boolean",
                Body = System.Array.Empty<bool>()
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(System.Array.Empty<bool>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "Boolean",
                Body = true
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "Boolean",
                Body = new bool[] { true, true, false }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonTokenTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Boolean",
                Body = true
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonTokenTypeNull1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                TYPE = "BOOLEAN",
                BODY = new bool[] { true, true, false }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonTokenTypeNull2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Boolean",
                Body = System.Array.Empty<bool>()
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(System.Array.Empty<bool>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "boolean",
                Body = true
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Boolean",
                body = new bool[] { true, true, false }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                DataType = "Boolean",
                Value = true
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonStringTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                DataType = "Boolean",
                Value = true
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(true), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "Boolean",
                value = new bool[] { true, true, false }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new bool[] { true, true, false });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(true, true, false), encoded);
        }

        [Fact]
        public void DecodeEncodeBooleanMatrixFromStringJsonTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new bool[,,] {
                { { true, false, true }, { true, false, true }, { true, false, true } },
                { { true, false, true }, { true, false, true }, { true, false, true } },
                { { true, false, true }, { true, false, true }, { true, false, true } },
                { { true, false, true }, { true, false, true }, { true, false, true } }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new bool[,,] {
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeBooleanMatrixFromStringJsonTypeBoolean() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new bool[,,] {
                { { true, false, true }, { true, false, true }, { true, false, true } },
                { { true, false, true }, { true, false, true }, { true, false, true } },
                { { true, false, true }, { true, false, true }, { true, false, true } },
                { { true, false, true }, { true, false, true }, { true, false, true } }
            });
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(new bool[,,] {
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeBooleanMatrixFromVariantJsonTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Boolean",
                body = new bool[,,] {
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new bool[,,] {
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeBooleanMatrixFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "Boolean",
                value = new bool[,,] {
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new bool[,,] {
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeBooleanMatrixFromVariantJsonTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Boolean",
                body = new bool[,,] {
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new bool[,,] {
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeBooleanMatrixFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "Boolean",
                value = new bool[,,] {
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new bool[,,] {
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } },
                    { { true, false, true }, { true, false, true }, { true, false, true } }
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
