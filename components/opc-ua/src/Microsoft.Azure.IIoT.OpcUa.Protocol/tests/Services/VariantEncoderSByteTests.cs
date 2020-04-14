// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using Xunit;

    public class VariantEncoderSByteTests {

        [Fact]
        public void DecodeEncodeSByteFromJValue() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(-123);
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromJArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125);
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeSByteFromJValueTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(-123);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromJArrayTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeSByteFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123, -124, -125";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant(new sbyte[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteFromStringTypeIntegerIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromStringTypeIntegerIsInt641() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(new Variant[] {
                new Variant(-123L), new Variant(-124L), new Variant(-125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromStringTypeIntegerIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteFromStringTypeNumberIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromStringTypeNumberIsInt641() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[] {
                new Variant(-123L), new Variant(-124L), new Variant(-125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromStringTypeNumberIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteFromStringTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }
        [Fact]
        public void DecodeEncodeSByteArrayFromStringTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123, -124, -125";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromStringTypeNullIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromStringTypeNullIsNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeSByteFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"-123\"";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteFromSinglyQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "  '-123'";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"-123\",'-124',\"-125\"";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromQuotedString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = " [\"-123\",'-124',\"-125\"] ";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteFromVariantJsonTokenTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "SByte",
                Body = -123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromVariantJsonTokenTypeVariant1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "SByte",
                Body = new sbyte[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromVariantJsonTokenTypeVariant2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "SByte",
                Body = new sbyte[0]
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new sbyte[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "SByte",
                Body = -123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "SByte",
                Body = new sbyte[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteFromVariantJsonTokenTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "SByte",
                Body = (sbyte)-123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromVariantJsonTokenTypeNull1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                TYPE = "SBYTE",
                BODY = new sbyte[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromVariantJsonTokenTypeNull2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "SByte",
                Body = new sbyte[0]
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new sbyte[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "sbyte",
                Body = (sbyte)-123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "SByte",
                body = new sbyte[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                DataType = "SByte",
                Value = -123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteFromVariantJsonStringTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                DataType = "SByte",
                Value = (sbyte)-123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((sbyte)-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteArrayFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                dataType = "SByte",
                value = new sbyte[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new sbyte[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((sbyte)-123, (sbyte)-124, (sbyte)-125), encoded);
        }

        [Fact]
        public void DecodeEncodeSByteMatrixFromStringJsonTypeSByte() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new sbyte[,,] {
                { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } }
            });
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant(new sbyte[,,] {
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeSByteMatrixFromVariantJsonTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "SByte",
                body = new sbyte[,,] {
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new sbyte[,,] {
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeSByteMatrixFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "SByte",
                value = new sbyte[,,] {
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new sbyte[,,] {
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeSByteMatrixFromVariantJsonTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "SByte",
                body = new sbyte[,,] {
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new sbyte[,,] {
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeSByteMatrixFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "SByte",
                value = new sbyte[,,] {
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new sbyte[,,] {
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } },
                    { { 123, -124, -125 }, { 123, -124, -125 }, { 123, -124, -125 } }
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
