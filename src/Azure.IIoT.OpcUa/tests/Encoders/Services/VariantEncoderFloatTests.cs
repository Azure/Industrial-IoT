// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Opc.Ua;
    using System.Text.Json.Nodes;
    using Xunit;

    public class VariantEncoderFloatTests
    {
        [Fact]
        public void DecodeEncodeFloatFromJValue()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(-123.123f);
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(-123.123f, encoded!.GetValue<float>());
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromJArray()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromArray(-123.123f, 124.124f, 0.0f);
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant([-123.123f, 124.124f, 0.0f]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(str, encoded));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromJArrayTypeNullIsDouble()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromArray(-123.123f, 124.124f, 0.0f);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant([-123.123, 124.124, 0.0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(str, encoded));
        }

        [Fact]
        public void DecodeEncodeFloatFromString1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "-123.123";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(-123.123f, encoded!.GetValue<float>());
        }

        [Fact]
        public void DecodeEncodeFloatFromString2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "-123";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(-123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123f), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "-123.123, 124.124, 0.0";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant([-123.123f, 124.124f, 0.0f]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(-123.123f, 124.124f, 0.0f), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromString2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[-123.123, 124.124, 0.0]";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant([-123.123f, 124.124f, 0.0f]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(-123.123f, 124.124f, 0.0f), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromString3()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[]";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(System.Array.Empty<float>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatFromStringTypeNumberIsDouble()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "-123.123";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123.123), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromStringTypeNumberIsDouble1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[-123.123, 124.124, 0.0]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(new Variant[] {
                new(-123.123), new(124.124), new(0.0)
            });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(-123.123, 124.124, 0.0), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromStringTypeNumberIsDouble2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[]";
            var variant = codec.Decode(str, BuiltInType.Number);
            var expected = new Variant(System.Array.Empty<Variant>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatFromStringTypeNullIsDouble()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "-123.123";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(-123.123), encoded));
        }
        [Fact]
        public void DecodeEncodeFloatArrayFromStringTypeNullIsDouble()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "-123.123, 124.124, 0.0";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant([-123.123, 124.124, 0.0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(-123.123, 124.124, 0.0), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromStringTypeNullIsDouble2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[-123.123, 124.124, 0.0]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant([-123.123, 124.124, 0.0]);
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(-123.123, 124.124, 0.0), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromStringTypeNullIsNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Null(encoded);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeFloatFromQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "\"-123.123\"";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(-123.123f, encoded!.GetValue<float>());
        }

        [Fact]
        public void DecodeEncodeFloatFromSinglyQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "  '-123.123'";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(-123.123f, encoded!.GetValue<float>());
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "\"-123.123\",'124.124',\"0.0\"";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant([-123.123f, 124.124f, 0.0f]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(-123.123f, 124.124f, 0.0f), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromQuotedString2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = " [\"-123.123\",'124.124',\"0.0\"] ";
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant([-123.123f, 124.124f, 0.0f]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(-123.123f, 124.124f, 0.0f), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonTokenTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                Type = "Float",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(-123.123f, encoded!.GetValue<float>());
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonTokenTypeVariant1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                Type = "Float",
                Body = new float[] { -123.123f, 124.124f, 0.0f }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant([-123.123f, 124.124f, 0.0f]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(-123.123f, 124.124f, 0.0f), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonTokenTypeVariant2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                Type = "Float",
                Body = System.Array.Empty<float>()
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(System.Array.Empty<float>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonStringTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                Type = "Float",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(-123.123f, encoded!.GetValue<float>());
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonStringTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                Type = "Float",
                Body = new float[] { -123.123f, 124.124f, 0.0f }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant([-123.123f, 124.124f, 0.0f]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(-123.123f, 124.124f, 0.0f), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonTokenTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                Type = "Float",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(-123.123f, encoded!.GetValue<float>());
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonTokenTypeNull1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                TYPE = "FLOAT",
                BODY = new float[] { -123.123f, 124.124f, 0.0f }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant([-123.123f, 124.124f, 0.0f]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(-123.123f, 124.124f, 0.0f), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonTokenTypeNull2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                Type = "Float",
                Body = System.Array.Empty<float>()
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(System.Array.Empty<float>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonStringTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                Type = "float",
                Body = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(-123.123f, encoded!.GetValue<float>());
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonStringTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                type = "Float",
                body = new float[] { -123.123f, 124.124f, 0.0f }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant([-123.123f, 124.124f, 0.0f]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(-123.123f, 124.124f, 0.0f), encoded));
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonTokenTypeNullMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                DataType = "Float",
                Value = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(-123.123f, encoded!.GetValue<float>());
        }

        [Fact]
        public void DecodeEncodeFloatFromVariantJsonStringTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                DataType = "Float",
                Value = -123.123f
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(-123.123f);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(-123.123f, encoded!.GetValue<float>());
        }

        [Fact]
        public void DecodeEncodeFloatArrayFromVariantJsonTokenTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                dataType = "Float",
                value = new float[] { -123.123f, 124.124f, 0.0f }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant([-123.123f, 124.124f, 0.0f]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(-123.123f, 124.124f, 0.0f), encoded));
        }

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
        [Fact]
        public void DecodeEncodeFloatMatrixFromStringJsonTypeFloat()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new float[,,] {
                { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
            });
            var variant = codec.Decode(str, BuiltInType.Float);
            var expected = new Variant((object)new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                });
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeFloatMatrixFromVariantJsonTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                type = "Float",
                body = new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((object)new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                });
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeFloatMatrixFromVariantJsonTokenTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                dataType = "Float",
                value = new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant((object)new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                });
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeFloatMatrixFromVariantJsonTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                type = "Float",
                body = new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((object)new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                });
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

        [Fact]
        public void DecodeEncodeFloatMatrixFromVariantJsonTokenTypeNullMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                dataType = "Float",
                value = new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant((object)new float[,,] {
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } },
                    { { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f }, { -123.456f, 124.567f, -125.0f } }
                });
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.True(expected.Value is Matrix);
            Assert.True(variant.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)variant.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)variant.Value).Dimensions);
        }

#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
    }
}
