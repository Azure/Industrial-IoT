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

    public class VariantEncoderUInt64Tests
    {
        [Fact]
        public void DecodeEncodeUInt64FromJValue()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(123Lu);
            var variant = codec.Decode(str, BuiltInType.UInt64);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromJArray()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromArray(123Lu, 124Lu, 125Lu);
            var variant = codec.Decode(str, BuiltInType.UInt64);
            var expected = new Variant([123Lu, 124Lu, 125Lu]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromJValueTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(123Lu);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromJArrayTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromArray(123Lu, 124Lu, 125Lu);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant([123L, 124L, 125L]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "123";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt64);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "123, 124, 125";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt64);
            var expected = new Variant([123Lu, 124Lu, 125Lu]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromString2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[123, 124, 125]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt64);
            var expected = new Variant([123Lu, 124Lu, 125Lu]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromString3()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt64);
            var expected = new Variant(System.Array.Empty<ulong>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromStringTypeIntegerIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "123";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Integer);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromStringTypeIntegerIsInt641()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[123, 124, 125]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Integer);
            var expected = new Variant(new Variant[] {
                new(123L), new(124L), new(125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromStringTypeIntegerIsInt642()
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
        public void DecodeEncodeUInt64FromStringTypeNumberIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "123";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Number);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromStringTypeNumberIsInt641()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[123, 124, 125]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Number);
            var expected = new Variant(new Variant[] {
                new(123L), new(124L), new(125L)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromStringTypeNumberIsInt642()
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
        public void DecodeEncodeUInt64FromStringTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "123";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(123L);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123Lu), encoded);
        }
        [Fact]
        public void DecodeEncodeUInt64ArrayFromStringTypeNullIsInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "123, 124, 125";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant([123L, 124L, 125L]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromStringTypeNullIsInt642()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "[123, 124, 125]";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant([123L, 124L, 125L]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromStringTypeNullIsNull()
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
        public void DecodeEncodeUInt64FromQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "\"123\"";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt64);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromSinglyQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "  '123'";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt64);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = "\"123\",'124',\"125\"";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt64);
            var expected = new Variant([123Lu, 124Lu, 125Lu]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromQuotedString2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            const string str = " [\"123\",'124',\"125\"] ";
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt64);
            var expected = new Variant([123Lu, 124Lu, 125Lu]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromVariantJsonTokenTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
                Type = "UInt64",
                Body = 123Lu
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromVariantJsonTokenTypeVariant1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
                Type = "UInt64",
                Body = new ulong[] { 123Lu, 124Lu, 125Lu }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant([123Lu, 124Lu, 125Lu]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromVariantJsonTokenTypeVariant2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
                Type = "UInt64",
                Body = System.Array.Empty<ulong>()
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(System.Array.Empty<ulong>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromVariantJsonStringTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                Type = "UInt64",
                Body = 123Lu
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromVariantJsonStringTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                Type = "UInt64",
                Body = new ulong[] { 123Lu, 124Lu, 125Lu }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant([123Lu, 124Lu, 125Lu]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromVariantJsonTokenTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
                Type = "UInt64",
                Body = 123Lu
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromVariantJsonTokenTypeNull1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
                TYPE = "UINT64",
                BODY = new ulong[] { 123Lu, 124Lu, 125Lu }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant([123Lu, 124Lu, 125Lu]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromVariantJsonTokenTypeNull2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
                Type = "UInt64",
                Body = System.Array.Empty<ulong>()
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(System.Array.Empty<ulong>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromVariantJsonStringTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                Type = "uint64",
                Body = 123Lu
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromVariantJsonStringTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                type = "UInt64",
                body = new ulong[] { 123Lu, 124Lu, 125Lu }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant([123Lu, 124Lu, 125Lu]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123Lu, 124Lu, 125Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromVariantJsonTokenTypeNullMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
                DataType = "UInt64",
                Value = 123Lu
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64FromVariantJsonStringTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                DataType = "UInt64",
                Value = 123Lu
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant(123Lu);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromObject(123Lu), encoded);
        }

        [Fact]
        public void DecodeEncodeUInt64ArrayFromVariantJsonTokenTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.FromObject(new
            {
                dataType = "UInt64",
                value = new ulong[] { 123Lu, 124Lu, 125Lu }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant([123Lu, 124Lu, 125Lu]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(_serializer.FromArray(123Lu, 124Lu, 125Lu), encoded);
        }

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
        [Fact]
        public void DecodeEncodeUInt64MatrixFromStringJsonTypeUInt64()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new ulong[,,] {
                { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } }
            }
            );
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.UInt64);
            var expected = new Variant((object)new ulong[,,] {
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } }
                });
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeUInt64MatrixFromVariantJsonTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                type = "UInt64",
                body = new ulong[,,] {
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } }
                }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant((object)new ulong[,,] {
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } }
                });
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeUInt64MatrixFromVariantJsonTokenTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                dataType = "UInt64",
                value = new ulong[,,] {
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } }
                }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Variant);
            var expected = new Variant((object)new ulong[,,] {
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } }
                });
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeUInt64MatrixFromVariantJsonTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                type = "UInt64",
                body = new ulong[,,] {
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } }
                }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant((object)new ulong[,,] {
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } }
                });
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeUInt64MatrixFromVariantJsonTokenTypeNullMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var str = _serializer.SerializeToString(new
            {
                dataType = "UInt64",
                value = new ulong[,,] {
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } }
                }
            });
            var variant = codec.Decode(_serializer.FromObject(str), BuiltInType.Null);
            var expected = new Variant((object)new ulong[,,] {
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } },
                    { { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu }, { 123Lu, 124Lu, 125Lu } }
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
