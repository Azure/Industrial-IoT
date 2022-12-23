// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using Xunit;

    public class VariantEncoderByteTests {

        [Fact]
        public void DecodeEncodeByteFromJValue() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(123);
            var variant = codec.Decode(str, BuiltInType.Byte);
            var expected = new Variant((byte)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromJArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray((byte)123, (byte)124, (byte)125);
            var variant = codec.Decode(str, BuiltInType.Byte);
            var expected = new Variant(new byte[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayTypeByteStringFromJArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new byte[] { 123, 124, 125 });
            var variant = codec.Decode(str, BuiltInType.ByteString);
            var expected = new Variant(new byte[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeByteFromJValueTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(123);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromJArrayTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray((byte)123, (byte)124, (byte)125);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeByteFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123";
            var variant = codec.Decode(str, BuiltInType.Byte);
            var expected = new Variant((byte)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123, 124, 125";
            var variant = codec.Decode(str, BuiltInType.Byte);
            var expected = new Variant(new byte[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((byte)123, (byte)124, (byte)125), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[123, 124, 125]";
            var variant = codec.Decode(str, BuiltInType.Byte);
            var expected = new Variant(new byte[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((byte)123, (byte)124, (byte)125), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Byte);
            var expected = new Variant(System.Array.Empty<byte>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeByteFromStringTypeIntegerIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromStringTypeIntegerIsInt641() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[123, 124, 125]";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(new Variant[] {
                new Variant(123L), new Variant(124L), new Variant(125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((byte)123, (byte)124, (byte)125), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromStringTypeIntegerIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(System.Array.Empty<Variant>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeByteFromStringTypeNumberIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromStringTypeNumberIsInt641() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[123, 124, 125]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[] {
                new Variant(123L), new Variant(124L), new Variant(125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((byte)123, (byte)124, (byte)125), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromStringTypeNumberIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(System.Array.Empty<Variant>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeByteFromStringTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }
        [Fact]
        public void DecodeEncodeByteArrayFromStringTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123, 124, 125";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((byte)123, (byte)124, (byte)125), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromStringTypeNullIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[123, 124, 125]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((byte)123, (byte)124, (byte)125), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromStringTypeNullIsNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeByteFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"123\"";
            var variant = codec.Decode(str, BuiltInType.Byte);
            var expected = new Variant((byte)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeByteFromSinglyQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "  '123'";
            var variant = codec.Decode(str, BuiltInType.Byte);
            var expected = new Variant((byte)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"123\",'124',\"125\"";
            var variant = codec.Decode(str, BuiltInType.Byte);
            var expected = new Variant(new byte[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((byte)123, (byte)124, (byte)125), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromQuotedString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = " [\"123\",'124',\"125\"] ";
            var variant = codec.Decode(str, BuiltInType.Byte);
            var expected = new Variant(new byte[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((byte)123, (byte)124, (byte)125), encoded);
        }

        [Fact]
        public void DecodeEncodeByteFromVariantJsonTokenTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Byte",
                Body = 123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((byte)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromVariantJsonTokenTypeVariant1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Byte",
                Body = new byte[] { 123, 124, 125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new byte[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((byte)123, (byte)124, (byte)125), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromVariantJsonTokenTypeVariant2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Byte",
                Body = System.Array.Empty<byte>()
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(System.Array.Empty<byte>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromVariantJsonTokenTypeVariant3() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "ByteString",
                Body = new byte[] { 123, 124, 125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new byte[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(new byte[] { 123, 124, 125 }), encoded);
        }

        [Fact]
        public void DecodeEncodeByteFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "Byte",
                Body = 123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((byte)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromVariantJsonStringTypeVariant1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "Byte",
                Body = new byte[] { 123, 124, 125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new byte[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((byte)123, (byte)124, (byte)125), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromVariantJsonStringTypeVariant2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "ByteString",
                Body = new byte[] { 123, 124, 125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new byte[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(new byte[] { 123, 124, 125 }), encoded);
        }

        [Fact]
        public void DecodeEncodeByteFromVariantJsonTokenTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Byte",
                Body = (byte)123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((byte)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromVariantJsonTokenTypeNull1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                TYPE = "BYTE",
                BODY = new byte[] { 123, 124, 125 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new byte[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((byte)123, (byte)124, (byte)125), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromVariantJsonTokenTypeNull2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Byte",
                Body = System.Array.Empty<byte>()
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(System.Array.Empty<byte>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeByteFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "byte",
                Body = (byte)123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((byte)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Byte",
                body = new byte[] { 123, 124, 125 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new byte[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((byte)123, (byte)124, (byte)125), encoded);
        }

        [Fact]
        public void DecodeEncodeByteFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                DataType = "Byte",
                Value = 123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((byte)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeByteFromVariantJsonStringTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                DataType = "Byte",
                Value = (byte)123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((byte)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeByteArrayFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                dataType = "Byte",
                value = new byte[] { 123, 124, 125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new byte[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((byte)123, (byte)124, (byte)125), encoded);
        }

        [Fact]
        public void DecodeEncodeByteMatrixFromStringJsonTypeByte() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new byte[,,] {
                { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } }
            });
            var variant = codec.Decode(str, BuiltInType.Byte);
            var expected = new Variant(new byte[,,] {
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeByteMatrixFromVariantJsonTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Byte",
                body = new byte[,,] {
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new byte[,,] {
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeByteMatrixFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "Byte",
                value = new byte[,,] {
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new byte[,,] {
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeByteMatrixFromVariantJsonTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Byte",
                body = new byte[,,] {
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new byte[,,] {
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeByteMatrixFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "Byte",
                value = new byte[,,] {
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new byte[,,] {
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } }
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
