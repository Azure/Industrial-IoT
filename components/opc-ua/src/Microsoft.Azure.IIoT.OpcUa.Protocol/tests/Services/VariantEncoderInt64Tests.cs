// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using Xunit;

    public class VariantEncoderInt64Tests {

        [Fact]
        public void DecodeEncodeInt64FromJValue() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(-123L);
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromJArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray(-123L, -124L, -125L);
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromJValueTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(-123L);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromJArrayTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromArray(-123L, -124L, -125L);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123, -124, -125";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(new long[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromStringTypeIntegerIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromStringTypeIntegerIsInt641() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(new Variant[] {
                new Variant(-123L), new Variant(-124L), new Variant(-125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromStringTypeIntegerIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromStringTypeNumberIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromStringTypeNumberIsInt641() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[] {
                new Variant(-123L), new Variant(-124L), new Variant(-125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromStringTypeNumberIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromStringTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }
        [Fact]
        public void DecodeEncodeInt64ArrayFromStringTypeNullIsInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-123, -124, -125";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromStringTypeNullIsInt642() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromStringTypeNullIsNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeInt64FromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"-123\"";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromSinglyQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "  '-123'";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromQuotedString() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"-123\",'-124',\"-125\"";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromQuotedString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = " [\"-123\",'-124',\"-125\"] ";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromVariantJsonTokenTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Int64",
                Body = -123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromVariantJsonTokenTypeVariant1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Int64",
                Body = new long[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromVariantJsonTokenTypeVariant2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Int64",
                Body = new long[0]
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new long[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "Int64",
                Body = -123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromVariantJsonStringTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "Int64",
                Body = new long[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromVariantJsonTokenTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Int64",
                Body = -123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromVariantJsonTokenTypeNull1() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                TYPE = "INT64",
                BODY = new long[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromVariantJsonTokenTypeNull2() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                Type = "Int64",
                Body = new long[0]
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                Type = "int64",
                Body = -123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromVariantJsonStringTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Int64",
                body = new long[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                DataType = "Int64",
                Value = -123
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromVariantJsonStringTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                DataType = "Int64",
                Value = -123
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.FromObject(new {
                dataType = "Int64",
                value = new long[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64MatrixFromStringJsonTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new long[,,] {
                { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[,,] {
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt64MatrixFromStringJsonTypeInt64() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new long[,,] {
                { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } }
            });
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(new long[,,] {
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt64MatrixFromVariantJsonTypeVariant() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Int64",
                body = new long[,,] {
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new long[,,] {
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt64MatrixFromVariantJsonTokenTypeVariantMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "Int64",
                value = new long[,,] {
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new long[,,] {
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt64MatrixFromVariantJsonTypeNull() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                type = "Int64",
                body = new long[,,] {
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[,,] {
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } }
                });
            var encoded = codec.Encode(variant);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt64MatrixFromVariantJsonTokenTypeNullMsftEncoding() {
            var codec = new VariantEncoderFactory().Default;
            var str = _serializer.SerializeToString(new {
                dataType = "Int64",
                value = new long[,,] {
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[,,] {
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } },
                    { { 123L, -124L, -125L }, { 123L, -124L, -125L }, { 123L, -124L, -125L } }
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
