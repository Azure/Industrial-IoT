// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using Xunit;

    public class VariantEncoderDoubleTests {

        [Fact]
        public void DecodeEncodeDoubleFromJValue() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(-123.123);
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromJArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray(-123.123, 124.124, 0.0);
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleFromJValueTypeNullIsDouble() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(-123.123);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromJArrayTypeNullIsDouble() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray(-123.123, 124.124, 0.0);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleFromString1() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123.123";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123), encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(-123.0);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.0),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123.123, 124.124, 0.0";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123, 124.124, 0.0),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123.123, 124.124, 0.0]";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123, 124.124, 0.0),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(System.Array.Empty<double>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleFromStringTypeNumberIsDouble() {
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
        public void DecodeEncodeDoubleArrayFromStringTypeNumberIsDouble1() {
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
        public void DecodeEncodeDoubleArrayFromStringTypeNumberIsDouble2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(System.Array.Empty<Variant>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleFromStringTypeNullIsDouble() {
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
        public void DecodeEncodeDoubleArrayFromStringTypeNullIsDouble() {
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
        public void DecodeEncodeDoubleArrayFromStringTypeNullIsDouble2() {
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
        public void DecodeEncodeDoubleArrayFromStringTypeNullIsNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeDoubleFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"-123.123\"";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123),
                encoded);

        }

        [Fact]
        public void DecodeEncodeDoubleFromSinglyQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "  '-123.123'";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"-123.123\",'124.124',\"0.0\"";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123, 124.124, 0.0),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromQuotedString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = " [\"-123.123\",'124.124',\"0.0\"] ";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123, 124.124, 0.0),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleFromVariantJsonTokenTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Double",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromVariantJsonTokenTypeVariant1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Double",
                Body = new double[] { -123.123, 124.124, 0.0 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123, 124.124, 0.0),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromVariantJsonTokenTypeVariant2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Double",
                Body = System.Array.Empty<double>()
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(System.Array.Empty<double>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "Double",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "Double",
                Body = new double[] { -123.123, 124.124, 0.0 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123, 124.124, 0.0),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleFromVariantJsonTokenTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Double",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromVariantJsonTokenTypeNull1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                TYPE = "DOUBLE",
                BODY = new double[] { -123.123, 124.124, 0.0 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123, 124.124, 0.0),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromVariantJsonTokenTypeNull2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Double",
                Body = System.Array.Empty<double>()
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(System.Array.Empty<double>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "double",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Double",
                body = new double[] { -123.123, 124.124, 0.0 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123, 124.124, 0.0),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                DataType = "Double",
                Value = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleFromVariantJsonStringTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                DataType = "Double",
                Value = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123.123),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                dataType = "Double",
                value = new double[] { -123.123, 124.124, 0.0 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123.123, 124.124, 0.0),
                encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleMatrixFromStringJsonTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new double[,,] {
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[,,] {
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeDoubleMatrixFromStringJsonTypeDouble() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new double[,,] {
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
            });
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(new double[,,] {
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeDoubleMatrixFromVariantJsonTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Double",
                body = new double[,,] {
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new double[,,] {
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeDoubleMatrixFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "Double",
                value = new double[,,] {
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new double[,,] {
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeDoubleMatrixFromVariantJsonTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Double",
                body = new double[,,] {
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[,,] {
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeDoubleMatrixFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "Double",
                value = new double[,,] {
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[,,] {
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
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
