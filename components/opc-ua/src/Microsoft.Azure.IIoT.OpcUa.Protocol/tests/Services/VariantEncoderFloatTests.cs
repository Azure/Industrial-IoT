// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Opc.Ua;
    using Xunit;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;

    public class VariantEncoderFloatTests {

        [Fact]
        public void DecodeEncodeFloatFromJValue() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(-123.123f);
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str,
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromJArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray(-123.123f, 124.124f, 0.0f);
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str,
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromJArrayTypeNullIsDouble() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray(-123.123f, 124.124f, 0.0f);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str,
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatFromString1() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123.123";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(-123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123.123, 124.124, 0.0";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123f, 124.124f, 0.0f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123.123, 124.124, 0.0]";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123f, 124.124f, 0.0f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(System.Array.Empty<float>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeFloatFromStringTypeNumberIsDouble() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123.123";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromStringTypeNumberIsDouble1() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123.123, 124.124, 0.0]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[] {
                new Variant(-123.123), new Variant(124.124), new Variant(0.0)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123, 124.124, 0.0),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromStringTypeNumberIsDouble2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(System.Array.Empty<Variant>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeFloatFromStringTypeNullIsDouble() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123.123";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123),
                encoded);
        }
        [Fact]
        public void DecodeEncodeFloatArrayFromStringTypeNullIsDouble() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123.123, 124.124, 0.0";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123, 124.124, 0.0),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromStringTypeNullIsDouble2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123.123, 124.124, 0.0]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123, 124.124, 0.0),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromStringTypeNullIsNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeFloatFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"-123.123\"";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123f),
                encoded);

        }

        [Fact]
        public void DecodeEncodeFloatFromSinglyQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "  '-123.123'";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"-123.123\",'124.124',\"0.0\"";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123f, 124.124f, 0.0f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromQuotedString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = " [\"-123.123\",'124.124',\"0.0\"] ";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123f, 124.124f, 0.0f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonTokenTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Float",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonTokenTypeVariant1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Float",
                Body = new float[] { -123.123f, 124.124f, 0.0f }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123f, 124.124f, 0.0f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonTokenTypeVariant2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Float",
                Body = System.Array.Empty<float>()
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(System.Array.Empty<float>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "Float",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "Float",
                Body = new float[] { -123.123f, 124.124f, 0.0f }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123f, 124.124f, 0.0f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonTokenTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Float",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonTokenTypeNull1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                TYPE = "FLOAT",
                BODY = new float[] { -123.123f, 124.124f, 0.0f }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123f, 124.124f, 0.0f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonTokenTypeNull2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Float",
                Body = System.Array.Empty<float>()
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(System.Array.Empty<float>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "float",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Float",
                body = new float[] { -123.123f, 124.124f, 0.0f }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123f, 124.124f, 0.0f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                DataType = "Float",
                Value = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonStringTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                DataType = "Float",
                Value = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                dataType = "Float",
                value = new float[] { -123.123f, 124.124f, 0.0f }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123f, 124.124f, 0.0f),
                encoded);
        }

        [Fact]
        public void DecodeEncodeFloatMatrixFromStringJsonTypeFloat() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new float[,,] {
                { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
            });
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeFloatMatrixFromVariantJsonTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Float",
                body = new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeFloatMatrixFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "Float",
                value = new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeFloatMatrixFromVariantJsonTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Float",
                body = new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeFloatMatrixFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "Float",
                value = new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
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
