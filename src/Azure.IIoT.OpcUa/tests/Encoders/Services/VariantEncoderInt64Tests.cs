// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Opc.Ua;
    using Xunit;

    public class VariantEncoderInt64Tests
    {
        [Fact]
        public void DecodeEncodeInt64FromJValue()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(-123L);
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromJArray()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromArray(-123L, -124L, -125L);
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromJValueTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(-123L);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromJArrayTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromArray(-123L, -124L, -125L);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "-123";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "-123, -124, -125";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromString2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromString3()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[]";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(System.Array.Empty<long>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromStringTypeIntegerIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "-123";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromStringTypeIntegerIsInt641()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(new Variant[] {
                new(-123L), new(-124L), new(-125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromStringTypeIntegerIsInt642()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[]";
            var variant = codec.Decode(str, BuiltInType.Integer);
            var expected = new Variant(System.Array.Empty<Variant>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromStringTypeNumberIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "-123";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromStringTypeNumberIsInt641()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[] {
                new(-123L), new(-124L), new(-125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromStringTypeNumberIsInt642()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(System.Array.Empty<Variant>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromStringTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "-123";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }
        [Fact]
        public void DecodeEncodeInt64ArrayFromStringTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "-123, -124, -125";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromStringTypeNullIsInt642()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[-123, -124, -125]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromStringTypeNullIsNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeInt64FromQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "\"-123\"";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromSinglyQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "  '-123'";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(-123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(-123L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "\"-123\",'-124',\"-125\"";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64ArrayFromQuotedString2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = " [\"-123\",'-124',\"-125\"] ";
            var variant = codec.Decode(str, BuiltInType.Int64);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromVariantJsonTokenTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
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
        public void DecodeEncodeInt64ArrayFromVariantJsonTokenTypeVariant1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
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
        public void DecodeEncodeInt64ArrayFromVariantJsonTokenTypeVariant2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
                Type = "Int64",
                Body = System.Array.Empty<long>()
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(System.Array.Empty<long>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromVariantJsonStringTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
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
        public void DecodeEncodeInt64ArrayFromVariantJsonStringTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
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
        public void DecodeEncodeInt64FromVariantJsonTokenTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
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
        public void DecodeEncodeInt64ArrayFromVariantJsonTokenTypeNull1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
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
        public void DecodeEncodeInt64ArrayFromVariantJsonTokenTypeNull2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
                Type = "Int64",
                Body = System.Array.Empty<long>()
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(System.Array.Empty<long>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeInt64FromVariantJsonStringTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
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
        public void DecodeEncodeInt64ArrayFromVariantJsonStringTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
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
        public void DecodeEncodeInt64FromVariantJsonTokenTypeNullMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
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
        public void DecodeEncodeInt64FromVariantJsonStringTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
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
        public void DecodeEncodeInt64ArrayFromVariantJsonTokenTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
                dataType = "Int64",
                value = new long[] { -123, -124, -125 }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new long[] { -123, -124, -125 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(-123L, -124L, -125L), encoded);
        }

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
        [Fact]
        public void DecodeEncodeInt64MatrixFromStringJsonTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
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
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt64MatrixFromStringJsonTypeInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
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
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt64MatrixFromVariantJsonTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
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
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt64MatrixFromVariantJsonTokenTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
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
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt64MatrixFromVariantJsonTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
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
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeInt64MatrixFromVariantJsonTokenTypeNullMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
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
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional

        private readonly NewtonsoftJsonSerializer _serializer = new();
    }
}
