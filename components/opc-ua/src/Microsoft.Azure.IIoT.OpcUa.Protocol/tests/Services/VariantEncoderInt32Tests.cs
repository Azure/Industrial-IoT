// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using Xunit;

    public class VariantEncoderInt32Tests {

        [Fact]
        public void DecodeEncodeInt32FromJValue() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(-123);
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant(-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromJArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray(-123, -124, -125);
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant(new int[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeInt32FromJValueTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(-123);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromJArrayTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray(-123, -124, -125);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeInt32FromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant(-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123, -124, -125";
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant(new int[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123, -124, -125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant(new int[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123, -124, -125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant(new int[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32FromStringTypeIntegerIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromStringTypeIntegerIsInt641() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(new Variant[] {
                new Variant(-123L), new Variant(-124L), new Variant(-125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123, -124, -125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromStringTypeIntegerIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32FromStringTypeNumberIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromStringTypeNumberIsInt641() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[] {
                new Variant(-123L), new Variant(-124L), new Variant(-125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123, -124, -125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromStringTypeNumberIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32FromStringTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }
        [Fact]
        public void DecodeEncodeInt32ArrayFromStringTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123, -124, -125";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123, -124, -125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromStringTypeNullIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123, -124, -125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromStringTypeNullIsNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeInt32FromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"-123\"";
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant(-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32FromSinglyQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "  '-123'";
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant(-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"-123\",'-124',\"-125\"";
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant(new int[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123, -124, -125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromQuotedString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = " [\"-123\",'-124',\"-125\"] ";
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant(new int[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123, -124, -125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32FromVariantJsonTokenTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Int32",
                Body = -123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromVariantJsonTokenTypeVariant1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Int32",
                Body = new int[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new int[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123, -124, -125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromVariantJsonTokenTypeVariant2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Int32",
                Body = new int[0]
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new int[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32FromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "Int32",
                Body = -123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "Int32",
                Body = new int[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new int[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123, -124, -125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32FromVariantJsonTokenTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Int32",
                Body = -123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromVariantJsonTokenTypeNull1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                TYPE = "INT32",
                BODY = new int[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new int[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123, -124, -125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromVariantJsonTokenTypeNull2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Int32",
                Body = new int[0]
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new int[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32FromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "int32",
                Body = -123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Int32",
                body = new int[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new int[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123, -124, -125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32FromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                DataType = "Int32",
                Value = -123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32FromVariantJsonStringTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                DataType = "Int32",
                Value = -123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32ArrayFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                dataType = "Int32",
                value = new int[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new int[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123, -124, -125), encoded);
        }

        [Fact]
        public void DecodeEncodeInt32MatrixFromStringJsonStringTypeInt32() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new int[,,] {
                { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } }
            });
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant(new int[,,] {
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt32MatrixFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Int32",
                body = new int[,,] {
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new int[,,] {
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt32MatrixFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "Int32",
                value = new int[,,] {
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new int[,,] {
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt32MatrixFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Int32",
                body = new int[,,] {
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new int[,,] {
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt32MatrixFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "Int32",
                value = new int[,,] {
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new int[,,] {
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } },
                    { { 123, -124, 125 }, { 123, -124, 125 }, { 123, -124, 125 } }
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
