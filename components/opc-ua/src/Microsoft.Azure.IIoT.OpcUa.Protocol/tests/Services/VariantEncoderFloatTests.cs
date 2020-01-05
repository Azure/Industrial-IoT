// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Opc.Ua;
    using Xunit;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    public class VariantEncoderFloatTests {

        [Fact]
        public void DecodeEncodeFloatFromJValue() {
            var codec = new VariantEncoderFactory().Default;
            var str = new JValue(-123.123f);
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str.ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromJArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = new JArray(-123.123f, 124.124f, 0.0f);
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str.ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatFromJValueTypeNullIsDouble() {
            var codec = new VariantEncoderFactory().Default;
            var str = new JValue(-123.123f);
            var variant = codec.Decode(str, BuiltInType.Null);
            //
            // TODO: See if we can preserve float precision even though we are passing null!
            //
            var expected = new Variant(Convert.ToDouble(str.Value));
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(Convert.ToDouble(str.Value)).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromJArrayTypeNullIsDouble() {
            var codec = new VariantEncoderFactory().Default;
            var str = new JArray(-123.123f, 124.124f, 0.0f);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str.ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatFromString1() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123.123";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(-123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123.123, 124.124, 0.0";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123f, 124.124f, 0.0f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123.123, 124.124, 0.0]";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123f, 124.124f, 0.0f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(new float[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeFloatFromStringTypeNumberIsDouble() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123.123";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
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
            Assert.Equal(new JArray(-123.123, 124.124, 0.0).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromStringTypeNumberIsDouble2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeFloatFromStringTypeNullIsDouble() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123.123";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }
        [Fact]
        public void DecodeEncodeFloatArrayFromStringTypeNullIsDouble() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123.123, 124.124, 0.0";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123, 124.124, 0.0).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromStringTypeNullIsDouble2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123.123, 124.124, 0.0]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123, 124.124, 0.0).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
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
            Assert.Equal(new JValue(-123.123f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));

        }

        [Fact]
        public void DecodeEncodeFloatFromSinglyQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "  '-123.123'";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"-123.123\",'124.124',\"0.0\"";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123f, 124.124f, 0.0f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromQuotedString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = " [\"-123.123\",'124.124',\"0.0\"] ";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123f, 124.124f, 0.0f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonTokenTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "Float",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonTokenTypeVariant1() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "Float",
                Body = new float[] { -123.123f, 124.124f, 0.0f }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123f, 124.124f, 0.0f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonTokenTypeVariant2() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "Float",
                Body = new float[0]
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new float[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "Float",
                Body = -123.123f
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "Float",
                Body = new float[] { -123.123f, 124.124f, 0.0f }
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123f, 124.124f, 0.0f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonTokenTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "Float",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonTokenTypeNull1() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                TYPE = "FLOAT",
                BODY = new float[] { -123.123f, 124.124f, 0.0f }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123f, 124.124f, 0.0f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonTokenTypeNull2() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "Float",
                Body = new float[0]
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new float[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "float",
                Body = -123.123f
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                type = "Float",
                body = new float[] { -123.123f, 124.124f, 0.0f }
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123f, 124.124f, 0.0f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                DataType = "Float",
                Value = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonStringTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                DataType = "Float",
                Value = -123.123f
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                dataType = "Float",
                value = new float[] { -123.123f, 124.124f, 0.0f }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new float[] { -123.123f, 124.124f, 0.0f });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123f, 124.124f, 0.0f).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeFloatMatrixFromStringJsonTypeFloat() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new float[,,] {
                { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
            }).ToString();
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
            var str = JToken.FromObject(new {
                type = "Float",
                body = new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                }
            }).ToString();
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
            var str = JToken.FromObject(new {
                dataType = "Float",
                value = new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                }
            }).ToString();
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
            var str = JToken.FromObject(new {
                type = "Float",
                body = new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                }
            }).ToString();
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
            var str = JToken.FromObject(new {
                dataType = "Float",
                value = new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                }
            }).ToString();
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


    }
}
