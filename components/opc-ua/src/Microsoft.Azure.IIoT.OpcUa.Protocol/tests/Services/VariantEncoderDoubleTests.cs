// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Opc.Ua;
    using Xunit;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class VariantEncoderDoubleTests {

        [Fact]
        public void DecodeEncodeDoubleFromJValue() {
            var codec = new VariantEncoderFactory().Default;
            var str = new JValue(-123.123);
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str.ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromJArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = new JArray(-123.123, 124.124, 0.0);
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str.ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleFromJValueTypeNullIsDouble() {
            var codec = new VariantEncoderFactory().Default;
            var str = new JValue(-123.123);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str.ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromJArrayTypeNullIsDouble() {
            var codec = new VariantEncoderFactory().Default;
            var str = new JArray(-123.123, 124.124, 0.0);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str.ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleFromString1() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123.123";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(-123.0);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.0).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123.123, 124.124, 0.0";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123, 124.124, 0.0).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123.123, 124.124, 0.0]";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123, 124.124, 0.0).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(new double[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleFromStringTypeNumberIsDouble() {
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
        public void DecodeEncodeDoubleArrayFromStringTypeNumberIsDouble1() {
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
        public void DecodeEncodeDoubleArrayFromStringTypeNumberIsDouble2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleFromStringTypeNullIsDouble() {
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
        public void DecodeEncodeDoubleArrayFromStringTypeNullIsDouble() {
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
        public void DecodeEncodeDoubleArrayFromStringTypeNullIsDouble2() {
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
            Assert.Equal(new JValue(-123.123).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));

        }

        [Fact]
        public void DecodeEncodeDoubleFromSinglyQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "  '-123.123'";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"-123.123\",'124.124',\"0.0\"";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123, 124.124, 0.0).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromQuotedString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = " [\"-123.123\",'124.124',\"0.0\"] ";
            var variant = codec.Decode(str, BuiltInType.Double);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123, 124.124, 0.0).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleFromVariantJsonTokenTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "Double",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromVariantJsonTokenTypeVariant1() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "Double",
                Body = new double[] { -123.123, 124.124, 0.0 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123, 124.124, 0.0).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromVariantJsonTokenTypeVariant2() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "Double",
                Body = new double[0]
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new double[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "Double",
                Body = -123.123f
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "Double",
                Body = new double[] { -123.123, 124.124, 0.0 }
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123, 124.124, 0.0).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleFromVariantJsonTokenTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "Double",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromVariantJsonTokenTypeNull1() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                TYPE = "DOUBLE",
                BODY = new double[] { -123.123, 124.124, 0.0 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123, 124.124, 0.0).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromVariantJsonTokenTypeNull2() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "Double",
                Body = new double[0]
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeDoubleFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                Type = "double",
                Body = -123.123f
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                type = "Double",
                body = new double[] { -123.123, 124.124, 0.0 }
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123, 124.124, 0.0).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                DataType = "Double",
                Value = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleFromVariantJsonStringTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                DataType = "Double",
                Value = -123.123f
            }).ToString();
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JValue(-123.123).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleArrayFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new {
                dataType = "Double",
                value = new double[] { -123.123, 124.124, 0.0 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new double[] { -123.123, 124.124, 0.0 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(new JArray(-123.123, 124.124, 0.0).ToString(Formatting.Indented),
                encoded.ToString(Formatting.Indented));
        }

        [Fact]
        public void DecodeEncodeDoubleMatrixFromStringJsonTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = JToken.FromObject(new double[,,] {
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
            }).ToString();
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
            var str = JToken.FromObject(new double[,,] {
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
            }).ToString();
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
            var str = JToken.FromObject(new {
                type = "Double",
                body = new double[,,] {
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
                }
            }).ToString();
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
            var str = JToken.FromObject(new {
                dataType = "Double",
                value = new double[,,] {
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
                }
            }).ToString();
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
            var str = JToken.FromObject(new {
                type = "Double",
                body = new double[,,] {
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
                }
            }).ToString();
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
            var str = JToken.FromObject(new {
                dataType = "Double",
                value = new double[,,] {
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } },
                    { { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 }, { 123.456, 124.567, 125.0 } }
                }
            }).ToString();
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


    }
}
