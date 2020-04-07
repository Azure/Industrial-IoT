// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using Xunit;

    public class VariantEncoderUInt32Tests {

        [Fact]
        public void DecodeEncodeUInt32FromJValue() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(123u);
            var variant = codec.Decode(str, BuiltInType.UInt32);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromJArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray(123u, 124u, 125u);
            var variant = codec.Decode(str, BuiltInType.UInt32);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromJValueTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(123u);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromJArrayTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray(123u, 124u, 125u);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123, 124, 125";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[123, 124, 125]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(new uint[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromStringTypeIntegerIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Integer);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromStringTypeIntegerIsInt641() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[123, 124, 125]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Integer);
            var expected = new Variant(new Variant[] {
                new Variant(123L), new Variant(124L), new Variant(125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromStringTypeIntegerIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Integer);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromStringTypeNumberIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Number);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromStringTypeNumberIsInt641() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[123, 124, 125]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Number);
            var expected = new Variant(new Variant[] {
                new Variant(123L), new Variant(124L), new Variant(125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromStringTypeNumberIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Number);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromStringTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }
        [Fact]
        public void DecodeEncodeUInt32ArrayFromStringTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123, 124, 125";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(new long[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromStringTypeNullIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[123, 124, 125]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(new long[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromStringTypeNullIsNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeUInt32FromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"123\"";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromSinglyQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "  '123'";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"123\",'124',\"125\"";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromQuotedString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = " [\"123\",'124',\"125\"] ";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromVariantJsonTokenTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "UInt32",
                Body = 123u
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromVariantJsonTokenTypeVariant1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "UInt32",
                Body = new uint[] { 123u, 124u, 125u }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromVariantJsonTokenTypeVariant2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "UInt32",
                Body = new uint[0]
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new uint[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "UInt32",
                Body = 123u
            }).ToString();
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "UInt32",
                Body = new uint[] { 123u, 124u, 125u }
            }).ToString();
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromVariantJsonTokenTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "UInt32",
                Body = 123u
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromVariantJsonTokenTypeNull1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                TYPE = "UINT32",
                BODY = new uint[] { 123u, 124u, 125u }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromVariantJsonTokenTypeNull2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "UInt32",
                Body = new uint[0]
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new uint[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "uint32",
                Body = 123u
            }).ToString();
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "UInt32",
                body = new uint[] { 123u, 124u, 125u }
            }).ToString();
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                DataType = "UInt32",
                Value = 123u
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromVariantJsonStringTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                DataType = "UInt32",
                Value = 123u
            }).ToString();
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                dataType = "UInt32",
                value = new uint[] { 123u, 124u, 125u }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32MatrixFromStringJsonStringTypeUInt32() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new uint[,,] {
                { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
            }).ToString();
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeUInt32MatrixFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "UInt32",
                body = new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                }
            }).ToString();
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant(new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeUInt32MatrixFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "UInt32",
                value = new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                }
            }).ToString();
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant(new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeUInt32MatrixFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "UInt32",
                body = new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                }
            }).ToString();
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeUInt32MatrixFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "UInt32",
                value = new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                }
            }).ToString();
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
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
