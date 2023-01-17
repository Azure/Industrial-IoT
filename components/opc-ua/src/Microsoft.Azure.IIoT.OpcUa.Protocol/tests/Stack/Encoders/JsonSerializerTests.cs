// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Encoders {
    using Opc.Ua.Models;
    using Xunit;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Linq;
    using System;

    public class JsonSerializerTests {

        [Fact]
        public void ReadWriteQualifiedName() {
            var expected = new QualifiedName("hello");

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<QualifiedName>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteQualifiedNameArray() {
            var expected = new[] {
                new QualifiedName("bla", 0),
                new QualifiedName("bla44", 0),
                new QualifiedName("bla2", 0),
                new QualifiedName("bla", 0),
                };

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<QualifiedName[]>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteDataSet1() {
            var expected = new DataSet {
                ["abcd"] = new DataValue(new Variant(1234), StatusCodes.Good, DateTime.UtcNow, DateTime.UtcNow),
                ["http://microsoft.com"] = new DataValue(new Variant(-222222222), StatusCodes.Bad, DateTime.MinValue, DateTime.UtcNow),
                ["1111111111111111111111111"] = new DataValue(new Variant(false), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["@#$%^&*()_+~!@#$%^*(){}"] = new DataValue(new Variant(new byte[] { 0, 2, 4, 6 }), StatusCodes.Good),
                ["1245"] = new DataValue(new Variant("hello"), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["..."] = new DataValue(new Variant("imbricated")),
            };

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<DataSet>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteDataValue1() {
            var expected = new DataValue(new Variant("hello"), StatusCodes.Good, DateTime.UtcNow);

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<DataValue>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteDataValue2() {
            var expected = new DataValue(new Variant("hello"), StatusCodes.Good, DateTime.UtcNow);

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<DataValue>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteDataValue3() {
            var expected = new DataValue(new Variant(new byte[30]), StatusCodes.Good, DateTime.UtcNow, DateTime.UtcNow);

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<DataValue>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteDataValue4() {
            var expected = new DataValue(StatusCodes.BadAggregateInvalidInputs, DateTime.UtcNow);

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<DataValue>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteDataValueNull() {
            DataValue expected = null;

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<DataValue>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteDataValueArray() {
            var expected = new[] {
                new DataValue(new Variant("bla")),
                new DataValue(new Variant(0)),
                new DataValue(new Variant(new byte[30])),
                new DataValue(new Variant("bla"), StatusCodes.BadAggregateListMismatch, DateTime.UtcNow, DateTime.UtcNow)
                };

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<DataValue[]>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteLocalizedText1() {
            var expected = new LocalizedText("hello");


            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<LocalizedText>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteLocalizedText2() {
            var expected = new LocalizedText(CultureInfo.CurrentCulture.Name, "hello");

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<LocalizedText>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteLocalizedTextNull() {
            LocalizedText expected = null;

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<LocalizedText>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteLocalizedTextArray1() {
            var expected = new[] {
                new LocalizedText("hello"),
                new LocalizedText("world"),
                new LocalizedText("here"),
                new LocalizedText("I am"),
                };

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<LocalizedText[]>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteLocalizedTextArray2() {
            var expected = new[] {
                new LocalizedText(CultureInfo.CurrentCulture.Name, "hello"),
                new LocalizedText(CultureInfo.CurrentCulture.Name, "world"),
                new LocalizedText(CultureInfo.CurrentCulture.Name, "here"),
                new LocalizedText(CultureInfo.CurrentCulture.Name, "I am"),
                };

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<LocalizedText[]>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteUUID() {
            var expected = new Uuid(Guid.NewGuid().ToString());

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<Uuid>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteNullableUUID() {
            Uuid? expected = new Uuid(Guid.NewGuid().ToString());

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<Uuid?>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteNullableUUIDWithNull() {
            Uuid? expected = null;

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<Uuid?>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteStatusCode() {
            var expected = new StatusCode(StatusCodes.BadAggregateInvalidInputs);

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<StatusCode>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteNullableStatusCode() {
            StatusCode? expected = new StatusCode(StatusCodes.BadAggregateInvalidInputs);

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<StatusCode?>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteNullableStatusCodeWithNull() {
            StatusCode? expected = null;

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<StatusCode?>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteArgument() {
            var expected = new Argument("something1",
                    new NodeId(2354), -1, "somedesciroeioi") {
                ArrayDimensions = Array.Empty<uint>()
            };

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<Argument>(json);
            Assert.True(result.IsEqual(expected));
        }

        [Fact]
        public void ReadWriteArgumentArray() {
            var expected = new[] {
                new Argument("something1",
                    new NodeId(2354), -1, "somedesciroeioi") { ArrayDimensions = Array.Empty<uint>() },
                new Argument("something2",
                    new NodeId(23), -1, "fdsadfsdaf") { ArrayDimensions = Array.Empty<uint>() },
                new Argument("something3",
                    new NodeId(44), 1, "fsadf  sadfsdfsadfsd") { ArrayDimensions = Array.Empty<uint>() },
                new Argument("something4",
                    new NodeId(23), 1, "dfad  sdafdfdf  fasdf") { ArrayDimensions = Array.Empty<uint>() }
            };

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<Argument[]>(json);
            for (var i = 0; i < result.Length; i++) {
                Assert.True(result[i].IsEqual(expected[i]));
            }
        }

        [Fact]
        public void ReadWriteStringVariant() {
            var expected = new Variant("5");

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<Variant>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteUintVariant() {
            var expected = new Variant((uint)99);

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<Variant>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteNullableVariant() {
            Variant? expected = new Variant("5");

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<Variant?>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteNullableVariantWithNull() {
            Variant? expected = null;

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<Variant?>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteStringArrayVariant() {
            var expected = new Variant(new string[] { "1", "2", "3", "4", "5" });

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<Variant>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteByteStringVariant() {
            var expected = new Variant(new byte[] { 1, 2, 3, 4, 5, 6 });

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<Variant>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteMatrixVariant1() {
            var expected = new Variant(new byte[4, 4] {
                { 1, 1, 1, 1 }, { 2, 2, 2, 2 }, { 3, 3, 3, 3 }, { 4, 4, 4, 4 }
            });

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<Variant>(json);
            Assert.True(expected.Value is Matrix);
            Assert.True(result.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)result.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)result.Value).Dimensions);
        }

        [Fact]
        public void ReadWriteMatrixVariant2() {
            var expected = new Variant(new[, ,] {
                { { "1", "1", "1" }, { "2", "2", "2" }, { "3", "3", "3" } },
                { { "1", "1", "1" }, { "2", "2", "2" }, { "3", "3", "3" } },
                { { "1", "1", "1" }, { "2", "2", "2" }, { "3", "3", "3" } },
                { { "1", "1", "1" }, { "2", "2", "2" }, { "3", "3", "3" } },
                { { "1", "1", "1" }, { "2", "2", "2" }, { "3", "3", "3" } },
                { { "1", "1", "1" }, { "2", "2", "2" }, { "3", "3", "3" } },
                { { "1", "1", "1" }, { "2", "2", "2" }, { "3", "3", "3" } }
            });

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<Variant>(json);
            Assert.True(expected.Value is Matrix);
            Assert.True(result.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)result.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)result.Value).Dimensions);
        }

        [Fact]
        public void ReadWriteVariantCollection() {
            var expected = new VariantCollection {
                new Variant(4L),
                new Variant("test"),
                new Variant(new long[] { 1, 2, 3, 4, 5 }),
                new Variant(new string[] {"1", "2", "3", "4", "5" })
            };

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<VariantCollection>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteVariantArray() {
            var expected = new[] {
                new Variant(4L),
                new Variant("test"),
                new Variant(new long[] {1, 2, 3, 4, 5 }),
                new Variant(new string[] {"1", "2", "3", "4", "5" })
            };

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<Variant[]>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteNodeAttributeSetNull() {
            NodeAttributeSet expected = null;

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<NodeAttributeSet>(json);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadWriteProgramDiagnostic2DataType() {
            // Create dummy type
            var expected = new ProgramDiagnostic2DataType {
                CreateClientName = "Testname",
                CreateSessionId = new NodeId(Guid.NewGuid()),
                InvocationCreationTime = DateTime.UtcNow,
                LastMethodCall = "swappido",
                LastMethodCallTime = DateTime.UtcNow,
                LastMethodInputArguments = new ArgumentCollection {
                    new Argument("something1",
                        new NodeId(2354), -1, "somedesciroeioi") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("something2",
                        new NodeId(23), -1, "fdsadfsdaf") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("something3",
                        new NodeId(44), 1, "fsadf  sadfsdfsadfsd") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("something4",
                        new NodeId(23), 1, "dfad  sdafdfdf  fasdf") { ArrayDimensions = Array.Empty<uint>() }
                },
                LastMethodInputValues = new VariantCollection {
                    new Variant(4L),
                    new Variant("test"),
                    new Variant(new long[] {1, 2, 3, 4, 5 }),
                    new Variant(new string[] {"1", "2", "3", "4", "5" })
                },
                LastMethodOutputArguments = new ArgumentCollection {
                    new Argument("foo1",
                        new NodeId(2354), -1, "somedesciroeioi") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("foo2",
                        new NodeId(33), -1, "fdsadfsdaf") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("adfsdafsdsdsafdsfa",
                        new NodeId("absc"), 1, "fsadf  sadfsdfsadfsd") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("ddddd",
                        new NodeId(25), 1, "dfad  sdafdfdf  fasdf") { ArrayDimensions = Array.Empty<uint>() }
                },
                LastMethodOutputValues = new VariantCollection {
                    new Variant(4L),
                    new Variant("test"),
                    new Variant(new long[] {1, 2, 3, 4, 5 }),
                    new Variant(new string[] {"1", "2", "3", "4", "5" })
                },
                LastMethodReturnStatus =
                    StatusCodes.BadAggregateConfigurationRejected,
                LastMethodSessionId = new NodeId(
                    Utils.Nonce.CreateNonce(32)),
                LastTransitionTime = DateTime.UtcNow - TimeSpan.FromDays(23)
            };

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<ProgramDiagnostic2DataType>(json);
            Assert.True(result.IsEqual(expected));
        }

        [Fact]
        public void ReadWriteProgramDiagnostic2DataTypeAsExtensionObject() {
            // Create dummy type
            var type = new ProgramDiagnostic2DataType {
                CreateClientName = "Testname",
                CreateSessionId = new NodeId(Guid.NewGuid()),
                InvocationCreationTime = DateTime.UtcNow,
                LastMethodCall = "swappido",
                LastMethodCallTime = DateTime.UtcNow,
                LastMethodInputArguments = new ArgumentCollection {
                    new Argument("something1",
                        new NodeId(2354), -1, "somedesciroeioi") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("something2",
                        new NodeId(23), -1, "fdsadfsdaf") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("something3",
                        new NodeId(44), 1, "fsadf  sadfsdfsadfsd") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("something4",
                        new NodeId(23), 1, "dfad  sdafdfdf  fasdf") { ArrayDimensions = Array.Empty<uint>() }
                },
                LastMethodInputValues = new VariantCollection {
                    new Variant(4L),
                    new Variant("test"),
                    new Variant(new long[] {1, 2, 3, 4, 5 }),
                    new Variant(new string[] {"1", "2", "3", "4", "5" })
                },
                LastMethodOutputArguments = new ArgumentCollection {
                    new Argument("foo1",
                        new NodeId(2354), -1, "somedesciroeioi") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("foo2",
                        new NodeId(33), -1, "fdsadfsdaf") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("adfsdafsdsdsafdsfa",
                        new NodeId("absc"), 1, "fsadf  sadfsdfsadfsd") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("ddddd",
                        new NodeId(25), 1, "dfad  sdafdfdf  fasdf") { ArrayDimensions = Array.Empty<uint>() }
                },
                LastMethodOutputValues = new VariantCollection {
                    new Variant(4L),
                    new Variant("test"),
                    new Variant(new long[] {1, 2, 3, 4, 5 }),
                    new Variant(new string[] {"1", "2", "3", "4", "5" })
                },
                LastMethodReturnStatus =
                    StatusCodes.BadAggregateConfigurationRejected,
                LastMethodSessionId = new NodeId(
                    Utils.Nonce.CreateNonce(32)),
                LastTransitionTime = DateTime.UtcNow - TimeSpan.FromDays(23)
            };
            var expected = new ExtensionObject(type);

            var json = _serializer.SerializeToString(expected);
            var result = _serializer.Deserialize<ExtensionObject>(json);
            Assert.Equal(result, expected);
        }

        private readonly IJsonSerializer _serializer =
            new NewtonSoftJsonSerializer(new JsonConverters().YieldReturn());
    }
}
