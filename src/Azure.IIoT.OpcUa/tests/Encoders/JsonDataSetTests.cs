// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Xunit;

    public class JsonDataSetTests
    {
        [Fact]
        public void ReadWriteProgramDiagnostic2DataTypeStream()
        {
            // Create dummy type
            var expected = VariantVariants.Complex;

            const int count = 100;
            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream())
            {
                using (var encoder = new JsonEncoderEx(stream, context,
                        JsonEncoderEx.JsonEncoding.Array))
                {
                    for (var i = 0; i < count; i++)
                    {
                        encoder.WriteEncodeable(null, expected, expected.GetType());
                    }
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new JsonDecoderEx(stream, context))
            {
                for (var i = 0; i < count; i++)
                {
                    var result = decoder.ReadEncodeable(null, expected.GetType());
                    Assert.True(result.IsEqual(expected));
                }
                var eof = decoder.ReadEncodeable(null, expected.GetType());
                Assert.Null(eof);
            }
        }

        [Fact]
        public void ReadWriteDataValueWithIntStream()
        {
            // Create dummy
            var expected = new DataValue(new Variant(12345));
            const int count = 10000;
            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream())
            {
                using (var encoder = new JsonEncoderEx(stream, context,
                        JsonEncoderEx.JsonEncoding.Array))
                {
                    for (var i = 0; i < count; i++)
                    {
                        encoder.WriteDataValue(null, expected);
                    }
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new JsonDecoderEx(stream, context))
            {
                for (var i = 0; i < count; i++)
                {
                    var result = decoder.ReadDataValue(null);
                    Assert.Equal(expected, result);
                }
                var eof = decoder.ReadDataValue(null);
                Assert.Null(eof);
            }
        }

        [Fact]
        public void ReadWriteDataValueWithStringStream()
        {
            // Create dummy
            var expected = new DataValue(new Variant("TestTestTestTest"
                + Guid.NewGuid()));
            const int count = 10000;
            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream())
            {
                using (var encoder = new JsonEncoderEx(stream, context,
                        JsonEncoderEx.JsonEncoding.Array))
                {
                    for (var i = 0; i < count; i++)
                    {
                        encoder.WriteDataValue(null, expected);
                    }
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new JsonDecoderEx(stream, context))
            {
                for (var i = 0; i < count; i++)
                {
                    var result = decoder.ReadDataValue(null);
                    Assert.Equal(expected, result);
                }
                var eof = decoder.ReadDataValue(null);
                Assert.Null(eof);
            }
        }

        [Fact]
        public void ReadWriteDataSetArrayTest()
        {
            // Create dummy
            var expected = new DataSet(new Dictionary<string, DataValue>
            {
                ["abcd"] = new DataValue(new Variant(1234), StatusCodes.Good, DateTime.UtcNow, DateTime.UtcNow),
                ["http://microsoft.com"] = new DataValue(new Variant(-222222222), StatusCodes.Bad, DateTime.MinValue, DateTime.UtcNow),
                ["1111111111111111111111111"] = new DataValue(new Variant(false), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["@#$%^&*()_+~!@#$%^*(){}"] = new DataValue(new Variant(new byte[] { 0, 2, 4, 6 }), StatusCodes.Good),
                ["1245"] = new DataValue(new Variant("hello"), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["..."] = new DataValue(new Variant("imbricated"))
            });

            const int count = 10000;
            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream())
            {
                using (var encoder = new JsonEncoderEx(stream, context,
                        JsonEncoderEx.JsonEncoding.Array))
                {
                    for (var i = 0; i < count; i++)
                    {
                        encoder.WriteDataSet(null, expected);
                    }
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new JsonDecoderEx(stream, context))
            {
                for (var i = 0; i < count; i++)
                {
                    var result = decoder.ReadDataSet(null);
                    Assert.Equal(expected, result);
                }
                var eof = decoder.ReadDataSet(null);
                Assert.Null(eof);
            }
        }

        [Fact]
        public void ReadWriteDataSetWithSingleEntryTest()
        {
            // Create dummy
            var expected = new DataSet(new Dictionary<string, DataValue>
            {
                ["abcd"] = new DataValue(new Variant(1234), StatusCodes.Good, DateTime.UtcNow, DateTime.UtcNow)
            });

            expected.DataSetFieldContentMask |= DataSetFieldContentFlags.SingleFieldDegradeToValue;

            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream())
            {
                using (var encoder = new JsonEncoderEx(stream, context,
                        JsonEncoderEx.JsonEncoding.Array))
                {
                    encoder.WriteDataSet(null, expected);
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new JsonDecoderEx(stream, context))
            {
                var result = decoder.ReadDataValue(null);
                Assert.Equal(expected.DataSetFields.FirstOrDefault(f => f.Name == "abcd").Value, result);
            }
        }

        [Fact]
        public void ReadWriteDataSetWithSingleValueRawTest()
        {
            // Create dummy
            var expected = new DataSet(new Dictionary<string, DataValue>
            {
                ["abcd"] = new DataValue(new Variant(1234), StatusCodes.Good, DateTime.UtcNow, DateTime.UtcNow)
            });

            expected.DataSetFieldContentMask |= DataSetFieldContentFlags.SingleFieldDegradeToValue;
            expected.DataSetFieldContentMask |= DataSetFieldContentFlags.RawData;

            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream())
            {
                using (var encoder = new JsonEncoderEx(stream, context,
                        JsonEncoderEx.JsonEncoding.Array))
                {
                    encoder.WriteDataSet(null, expected);
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new JsonDecoderEx(stream, context))
            {
                var result = decoder.ReadInt32(null);
                Assert.Equal(expected.DataSetFields.FirstOrDefault(f => f.Name == "abcd").Value.Value, result);
            }
        }
    }
}
