// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Encoders {
    using System;
    using Xunit;
    using System.IO;
    using System.Collections.Generic;
    using Opc.Ua.Extensions;

    public class JsonEncoderDecoderTests {

        [Fact]
        public void ReadWriteProgramDiagnostic2DataTypeStream() {

            // Create dummy type
            var expected = new ProgramDiagnostic2DataType {
                CreateClientName = "Testname",
                CreateSessionId = new NodeId(Guid.NewGuid()),
                InvocationCreationTime = DateTime.UtcNow,
                LastMethodCall = "swappido",
                LastMethodCallTime = DateTime.UtcNow,
                LastMethodInputArguments = new ArgumentCollection {
                    new Argument("something1",
                        new NodeId(2354), -1, "somedesciroeioi") { ArrayDimensions = new uint[0] },
                    new Argument("something2",
                        new NodeId(23), -1, "fdsadfsdaf") { ArrayDimensions = new uint[0] },
                    new Argument("something3",
                        new NodeId(44), 1, "fsadf  sadfsdfsadfsd") { ArrayDimensions = new uint[0] },
                    new Argument("something4",
                        new NodeId(23), 1, "dfad  sdafdfdf  fasdf") { ArrayDimensions = new uint[0] }
                },
                LastMethodInputValues = new VariantCollection {
                    new Variant(4L),
                    new Variant("test"),
                    new Variant(new long[] {1, 2, 3, 4, 5 }),
                    new Variant(new string[] {"1", "2", "3", "4", "5" })
                },
                LastMethodOutputArguments = new ArgumentCollection {
                    new Argument("foo1",
                        new NodeId(2354), -1, "somedesciroeioi") { ArrayDimensions = new uint[0] },
                    new Argument("foo2",
                        new NodeId(33), -1, "fdsadfsdaf") { ArrayDimensions = new uint[0] },
                    new Argument("adfsdafsdsdsafdsfa",
                        new NodeId("absc"), 1, "fsadf  sadfsdfsadfsd") { ArrayDimensions = new uint[0] },
                    new Argument("ddddd",
                        new NodeId(25), 1, "dfad  sdafdfdf  fasdf") { ArrayDimensions = new uint[0] }
                },
                LastMethodOutputValues = new VariantCollection {
                    new Variant(4L),
                    new Variant("test"),
                    new Variant(new long[] {1, 2, 3, 4, 5 }),
                    new Variant(new string[] {"1", "2", "3", "4", "5" })
                },
                LastMethodReturnStatus = new StatusResult(
                    StatusCodes.BadAggregateConfigurationRejected),
                LastMethodSessionId = new NodeId(
                    Utils.Nonce.CreateNonce(32)),
                LastTransitionTime = DateTime.UtcNow - TimeSpan.FromDays(23)
            };

            var count = 100;
            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream()) {
                using (var encoder = new JsonEncoderEx(stream, context,
                        JsonEncoderEx.JsonEncoding.Array)) {
                    for (var i = 0; i < count; i++) {
                        encoder.WriteEncodeable(null, expected, expected.GetType());
                    }
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer)) {
                using (var decoder = new JsonDecoderEx(stream, context)) {
                    for (var i = 0; i < count; i++) {
                        var result = decoder.ReadEncodeable(null, expected.GetType());
                        Assert.True(result.IsEqual(expected));
                    }
                    var eof = decoder.ReadEncodeable(null, expected.GetType());
                    Assert.Null(eof);
                }
            }
        }

        [Fact]
        public void ReadWriteDatavalueWithIntStream() {

            // Create dummy
            var expected = new DataValue(new Variant(12345));
            var count = 10000;
            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream()) {
                using (var encoder = new JsonEncoderEx(stream, context,
                        JsonEncoderEx.JsonEncoding.Array)) {
                    for (var i = 0; i < count; i++) {
                        encoder.WriteDataValue(null, expected);
                    }
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer)) {
                using (var decoder = new JsonDecoderEx(stream, context)) {
                    for (var i = 0; i < count; i++) {
                        var result = decoder.ReadDataValue(null);
                        Assert.Equal(expected, result);
                    }
                    var eof = decoder.ReadDataValue(null);
                    Assert.Null(eof);
                }
            }
        }

        [Fact]
        public void ReadWriteDatavalueWithStringStream() {

            // Create dummy
            var expected = new DataValue(new Variant("TestTestTestTest"
                + Guid.NewGuid()));
            var count = 10000;
            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream()) {
                using (var encoder = new JsonEncoderEx(stream, context,
                        JsonEncoderEx.JsonEncoding.Array)) {
                    for (var i = 0; i < count; i++) {
                        encoder.WriteDataValue(null, expected);
                    }
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer)) {
                using (var decoder = new JsonDecoderEx(stream, context)) {
                    for (var i = 0; i < count; i++) {
                        var result = decoder.ReadDataValue(null);
                        Assert.Equal(expected, result);
                    }
                    var eof = decoder.ReadDataValue(null);
                    Assert.Null(eof);
                }
            }
        }

        [Fact]
        public void ReadWriteDataValueDictionary() {

            // Create dummy
            var expected = new Dictionary<string, DataValue> {
                ["abcd"] = new DataValue(new Variant(1234), StatusCodes.Good, DateTime.Now, DateTime.UtcNow),
                ["http://microsoft.com"] = new DataValue(new Variant(-222222222), StatusCodes.Bad, DateTime.MinValue, DateTime.Now),
                ["1111111111111111111111111"] = new DataValue(new Variant(false), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["@#$%^&*()_+~!@#$%^*(){}"] = new DataValue(new Variant(new byte[] { 0, 2, 4, 6 }), StatusCodes.Good),
                ["1245"] = new DataValue(new Variant("hello"), StatusCodes.Bad, DateTime.Now, DateTime.MinValue),
                ["..."] = new DataValue(new Variant(new Variant("imbricated"))),
            };

            var count = 10000;
            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream()) {
                using (var encoder = new JsonEncoderEx(stream, context,
                        JsonEncoderEx.JsonEncoding.Array)) {
                    for (var i = 0; i < count; i++) {
                        encoder.WriteDataValueDictionary(null, expected);
                    }
                }
                buffer = stream.ToArray();
            }
            // convert DataValue timestamps to OpcUa Utc 
            var expectedResult = new Dictionary<string, DataValue>();
            foreach (var entry in expected) {
                expectedResult[entry.Key] = new DataValue(entry.Value).ToOpcUaUniversalTime();
            }
            using (var stream = new MemoryStream(buffer)) {
                using (var decoder = new JsonDecoderEx(stream, context)) {
                    for (var i = 0; i < count; i++) {
                        var result = decoder.ReadDataValueDictionary(null);
                        Assert.Equal(expectedResult, result);
                    }
                    var eof = decoder.ReadDataValue(null);
                    Assert.Null(eof);
                }
            }
        }
    }
}
