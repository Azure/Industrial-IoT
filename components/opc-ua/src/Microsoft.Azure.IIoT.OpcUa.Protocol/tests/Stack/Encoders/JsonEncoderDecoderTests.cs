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
        public void ReadWriteDataValueWithIntStream() {

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
        public void ReadWriteDataValueWithStringStream() {

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
        public void ReadWriteDataSetArrayTest() {

            // Create dummy
            var expected = new DataSet {
                ["abcd"] = new DataValue(new Variant(1234), StatusCodes.Good, DateTime.UtcNow, DateTime.UtcNow),
                ["http://microsoft.com"] = new DataValue(new Variant(-222222222), StatusCodes.Bad, DateTime.MinValue, DateTime.UtcNow),
                ["1111111111111111111111111"] = new DataValue(new Variant(false), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["@#$%^&*()_+~!@#$%^*(){}"] = new DataValue(new Variant(new byte[] { 0, 2, 4, 6 }), StatusCodes.Good),
                ["1245"] = new DataValue(new Variant("hello"), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["..."] = new DataValue(new Variant("imbricated")),
            };

            var count = 10000;
            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream()) {
                using (var encoder = new JsonEncoderEx(stream, context,
                        JsonEncoderEx.JsonEncoding.Array)) {
                    for (var i = 0; i < count; i++) {
                        encoder.WriteDataSet(null, expected);
                    }
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer)) {
                using (var decoder = new JsonDecoderEx(stream, context)) {
                    for (var i = 0; i < count; i++) {
                        var result = decoder.ReadDataSet(null);
                        Assert.Equal(expected, result);
                    }
                    var eof = decoder.ReadDataSet(null);
                    Assert.Null(eof);
                }
            }
        }
    }
}
