// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using Xunit;

    public class VariantEncoderUInt16Tests {

        [Fact]
        public void DecodeEncodeUInt16FromJValue() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(123);
            var variant = codec.Decode(str, BuiltInType.UInt16);
            var expected = new Variant((ushort)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromJArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray((ushort)123, (ushort)124, (ushort)125);
            var variant = codec.Decode(str, BuiltInType.UInt16);
            var expected = new Variant(new ushort[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16FromJValueTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(123);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromJArrayTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray((ushort)123, (ushort)124, (ushort)125);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16FromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123";
            var variant = codec.Decode(str, BuiltInType.UInt16);
            var expected = new Variant((ushort)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123, 124, 125";
            var variant = codec.Decode(str, BuiltInType.UInt16);
            var expected = new Variant(new ushort[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((ushort)123, (ushort)124, (ushort)125), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[123, 124, 125]";
            var variant = codec.Decode(str, BuiltInType.UInt16);
            var expected = new Variant(new ushort[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((ushort)123, (ushort)124, (ushort)125), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.UInt16);
            var expected = new Variant(new ushort[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16FromStringTypeIntegerIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromStringTypeIntegerIsInt641() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[123, 124, 125]";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(new Variant[] {
                new Variant(123L), new Variant(124L), new Variant(125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((ushort)123, (ushort)124, (ushort)125), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromStringTypeIntegerIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16FromStringTypeNumberIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromStringTypeNumberIsInt641() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[123, 124, 125]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[] {
                new Variant(123L), new Variant(124L), new Variant(125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((ushort)123, (ushort)124, (ushort)125), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromStringTypeNumberIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16FromStringTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }
        [Fact]
        public void DecodeEncodeUInt16ArrayFromStringTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123, 124, 125";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((ushort)123, (ushort)124, (ushort)125), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromStringTypeNullIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[123, 124, 125]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((ushort)123, (ushort)124, (ushort)125), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromStringTypeNullIsNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeUInt16FromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"123\"";
            var variant = codec.Decode(str, BuiltInType.UInt16);
            var expected = new Variant((ushort)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16FromSinglyQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "  '123'";
            var variant = codec.Decode(str, BuiltInType.UInt16);
            var expected = new Variant((ushort)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"123\",'124',\"125\"";
            var variant = codec.Decode(str, BuiltInType.UInt16);
            var expected = new Variant(new ushort[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((ushort)123, (ushort)124, (ushort)125), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromQuotedString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = " [\"123\",'124',\"125\"] ";
            var variant = codec.Decode(str, BuiltInType.UInt16);
            var expected = new Variant(new ushort[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((ushort)123, (ushort)124, (ushort)125), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16FromVariantJsonTokenTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "UInt16",
                Body = 123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((ushort)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromVariantJsonTokenTypeVariant1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "UInt16",
                Body = new ushort[] { 123, 124, 125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new ushort[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((ushort)123, (ushort)124, (ushort)125), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromVariantJsonTokenTypeVariant2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "UInt16",
                Body = new ushort[0]
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new ushort[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16FromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "UInt16",
                Body = 123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((ushort)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "UInt16",
                Body = new ushort[] { 123, 124, 125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new ushort[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((ushort)123, (ushort)124, (ushort)125), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16FromVariantJsonTokenTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "UInt16",
                Body = (ushort)123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((ushort)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromVariantJsonTokenTypeNull1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                TYPE = "UINT16",
                BODY = new ushort[] { 123, 124, 125 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new ushort[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((ushort)123, (ushort)124, (ushort)125), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromVariantJsonTokenTypeNull2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "UInt16",
                Body = new ushort[0]
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new ushort[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16FromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "uint16",
                Body = (ushort)123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((ushort)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "UInt16",
                body = new ushort[] { 123, 124, 125 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new ushort[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((ushort)123, (ushort)124, (ushort)125), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16FromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                DataType = "UInt16",
                Value = 123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((ushort)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16FromVariantJsonStringTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                DataType = "UInt16",
                Value = (ushort)123
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant((ushort)123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16ArrayFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                dataType = "UInt16",
                value = new ushort[] { 123, 124, 125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new ushort[] { 123, 124, 125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray((ushort)123, (ushort)124, (ushort)125), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt16MatrixFromStringJsonStringTypeUInt16() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new ushort[,,] {
                { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt16);
            var expected = new Variant(new ushort[,,] {
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
        public void DecodeEncodeUInt16MatrixFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "UInt16",
                body = new ushort[,,] {
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } }
                }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant(new ushort[,,] {
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
        public void DecodeEncodeUInt16MatrixFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "UInt16",
                value = new ushort[,,] {
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } }
                }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant(new ushort[,,] {
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
        public void DecodeEncodeUInt16MatrixFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "UInt16",
                body = new ushort[,,] {
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } }
                }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(new ushort[,,] {
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
        public void DecodeEncodeUInt16MatrixFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "UInt16",
                value = new ushort[,,] {
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } },
                    { { 123, 124, 125 }, { 123, 124, 125 }, { 123, 124, 125 } }
                }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(new ushort[,,] {
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
