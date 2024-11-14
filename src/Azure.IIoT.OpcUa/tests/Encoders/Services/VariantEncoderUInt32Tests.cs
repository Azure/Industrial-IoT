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

    public class VariantEncoderUInt32Tests
    {
        [Fact]
        public void DecodeEncodeUInt32FromJValue()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(123u);
            var variant = codec.Decode(str, BuiltInType.UInt32);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromJArray()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromArray(123u, 124u, 125u);
            var variant = codec.Decode(str, BuiltInType.UInt32);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromJValueTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(123u);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromJArrayTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromArray(123u, 124u, 125u);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(new long[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "123";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "123, 124, 125";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromString2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[123, 124, 125]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromString3()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(System.Array.Empty<uint>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromStringTypeIntegerIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "123";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Integer);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromStringTypeIntegerIsInt641()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[123, 124, 125]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Integer);
            var expected = new Variant(new Variant[] {
                new(123L), new(124L), new(125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromStringTypeIntegerIsInt642()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Integer);
            var expected = new Variant(System.Array.Empty<Variant>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromStringTypeNumberIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "123";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Number);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromStringTypeNumberIsInt641()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[123, 124, 125]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Number);
            var expected = new Variant(new Variant[] {
                new(123L), new(124L), new(125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromStringTypeNumberIsInt642()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Number);
            var expected = new Variant(System.Array.Empty<Variant>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromStringTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "123";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }
        [Fact]
        public void DecodeEncodeUInt32ArrayFromStringTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "123, 124, 125";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(new long[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromStringTypeNullIsInt642()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[123, 124, 125]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(new long[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromStringTypeNullIsNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeUInt32FromQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "\"123\"";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromSinglyQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "  '123'";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "\"123\",'124',\"125\"";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromQuotedString2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = " [\"123\",'124',\"125\"] ";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromVariantJsonTokenTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
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
        public void DecodeEncodeUInt32ArrayFromVariantJsonTokenTypeVariant1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
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
        public void DecodeEncodeUInt32ArrayFromVariantJsonTokenTypeVariant2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
                Type = "UInt32",
                Body = System.Array.Empty<uint>()
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(System.Array.Empty<uint>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromVariantJsonStringTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                Type = "UInt32",
                Body = 123u
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromVariantJsonStringTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                Type = "UInt32",
                Body = new uint[] { 123u, 124u, 125u }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromVariantJsonTokenTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
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
        public void DecodeEncodeUInt32ArrayFromVariantJsonTokenTypeNull1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
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
        public void DecodeEncodeUInt32ArrayFromVariantJsonTokenTypeNull2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
                Type = "UInt32",
                Body = System.Array.Empty<uint>()
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(System.Array.Empty<uint>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromVariantJsonStringTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                Type = "uint32",
                Body = 123u
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromVariantJsonStringTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                type = "UInt32",
                body = new uint[] { 123u, 124u, 125u }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32FromVariantJsonTokenTypeNullMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
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
        public void DecodeEncodeUInt32FromVariantJsonStringTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                DataType = "UInt32",
                Value = 123u
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123u), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt32ArrayFromVariantJsonTokenTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
                dataType = "UInt32",
                value = new uint[] { 123u, 124u, 125u }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(new uint[] { 123u, 124u, 125u });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123u, 124u, 125u), encoded);
        }

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
        [Fact]
        public void DecodeEncodeUInt32MatrixFromStringJsonStringTypeUInt32()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new uint[,,] {
                { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt32);
            var expected = new Variant(new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                });
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeUInt32MatrixFromVariantJsonStringTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                type = "UInt32",
                body = new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant(new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                });
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeUInt32MatrixFromVariantJsonTokenTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                dataType = "UInt32",
                value = new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant(new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                });
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeUInt32MatrixFromVariantJsonStringTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                type = "UInt32",
                body = new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                });
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeUInt32MatrixFromVariantJsonTokenTypeNullMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                dataType = "UInt32",
                value = new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
                }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(new uint[,,] {
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } },
                    { { 123u, 124u, 125u }, { 123u, 124u, 125u }, { 123u, 124u, 125u } }
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
