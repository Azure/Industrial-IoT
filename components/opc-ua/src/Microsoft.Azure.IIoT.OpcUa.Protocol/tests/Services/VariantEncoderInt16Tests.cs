// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using Xunit;

    public class VariantEncoderInt16Tests {

        [Fact]
        public void DecodeEncodeInt16FromJValue() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(-123);
            var variant = codec.Decode(str, BuiltInType.Int16);
            var expected = new Variant((short)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromJArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray((short)-123, (short)-124, (short)-125);
            var variant = codec.Decode(str, BuiltInType.Int16);
            var expected = new Variant(new short[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeInt16FromJValueTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(-123);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromJArrayTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray((short)-123, (short)-124, (short)-125);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeInt16FromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Int16);
            var expected = new Variant((short)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123, -124, -125";
            var variant = codec.Decode(str, BuiltInType.Int16);
            var expected = new Variant(new short[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((short)-123, (short)-124, (short)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Int16);
            var expected = new Variant(new short[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((short)-123, (short)-124, (short)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Int16);
            var expected = new Variant(new short[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16FromStringTypeIntegerIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromStringTypeIntegerIsInt641() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(new Variant[] {
                new Variant(-123L), new Variant(-124L), new Variant(-125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((short)-123, (short)-124, (short)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromStringTypeIntegerIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16FromStringTypeNumberIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromStringTypeNumberIsInt641() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[] {
                new Variant(-123L), new Variant(-124L), new Variant(-125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((short)-123, (short)-124, (short)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromStringTypeNumberIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16FromStringTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }
        [Fact]
        public void DecodeEncodeInt16ArrayFromStringTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123, -124, -125";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((short)-123, (short)-124, (short)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromStringTypeNullIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((short)-123, (short)-124, (short)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromStringTypeNullIsNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeInt16FromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"-123\"";
            var variant = codec.Decode(str, BuiltInType.Int16);
            var expected = new Variant((short)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16FromSinglyQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "  '-123'";
            var variant = codec.Decode(str, BuiltInType.Int16);
            var expected = new Variant((short)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"-123\",'-124',\"-125\"";
            var variant = codec.Decode(str, BuiltInType.Int16);
            var expected = new Variant(new short[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((short)-123, (short)-124, (short)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromQuotedString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = " [\"-123\",'-124',\"-125\"] ";
            var variant = codec.Decode(str, BuiltInType.Int16);
            var expected = new Variant(new short[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((short)-123, (short)-124, (short)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16FromVariantJsonTokenTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Int16",
                Body = -123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((short)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromVariantJsonTokenTypeVariant1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Int16",
                Body = new short[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new short[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((short)-123, (short)-124, (short)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromVariantJsonTokenTypeVariant2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Int16",
                Body = new short[0]
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new short[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16FromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "Int16",
                Body = -123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((short)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "Int16",
                Body = new short[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new short[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((short)-123, (short)-124, (short)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16FromVariantJsonTokenTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Int16",
                Body = (short)-123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((short)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromVariantJsonTokenTypeNull1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                TYPE = "INT16",
                BODY = new short[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new short[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((short)-123, (short)-124, (short)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromVariantJsonTokenTypeNull2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Int16",
                Body = new short[0]
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new short[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16FromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "int16",
                Body = (short)-123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((short)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Int16",
                body = new short[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new short[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((short)-123, (short)-124, (short)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16FromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                DataType = "Int16",
                Value = -123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((short)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16FromVariantJsonStringTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                DataType = "Int16",
                Value = (short)-123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((short)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16ArrayFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                dataType = "Int16",
                value = new short[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new short[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((short)-123, (short)-124, (short)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt16MatrixFromStringJsonTypeInt16() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new short[,,] {
                { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } }
            });
            var variant = codec.Decode(str, BuiltInType.Int16);
            var expected = new Variant(new short[,,] {
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt16MatrixFromVariantJsonTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Int16",
                body = new short[,,] {
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new short[,,] {
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt16MatrixFromVariantJsonTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "Int16",
                value = new short[,,] {
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new short[,,] {
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt16MatrixFromVariantJsonTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Int16",
                body = new short[,,] {
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new short[,,] {
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt16MatrixFromVariantJsonTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "Int16",
                value = new short[,,] {
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new short[,,] {
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } },
                    { { -123, 124, -125 }, { -123, 124, -125 }, { -123, 124, -125 } }
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
