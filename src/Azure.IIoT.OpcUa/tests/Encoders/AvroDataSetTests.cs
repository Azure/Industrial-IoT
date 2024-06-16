// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.Schemas.Avro;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using global::Avro;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.IO;
    using System.Linq;
    using Xunit;

    public class AvroDataSetTests
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
                using (var encoder = new SchemalessAvroEncoder(stream, context))
                {
                    for (var i = 0; i < count; i++)
                    {
                        encoder.WriteEncodeable(null, expected, expected.GetType());
                    }
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new SchemalessAvroDecoder(stream, context))
            {
                for (var i = 0; i < count; i++)
                {
                    var result = decoder.ReadEncodeable(null, expected.GetType());
                    Assert.True(result.IsEqual(expected));
                }
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
                using (var encoder = new SchemalessAvroEncoder(stream, context))
                {
                    for (var i = 0; i < count; i++)
                    {
                        encoder.WriteDataValue(null, expected);
                    }
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new SchemalessAvroDecoder(stream, context))
            {
                for (var i = 0; i < count; i++)
                {
                    var result = decoder.ReadDataValue(null);
                    Assert.Equal(expected, result);
                }
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
                using (var encoder = new SchemalessAvroEncoder(stream, context))
                {
                    for (var i = 0; i < count; i++)
                    {
                        encoder.WriteDataValue(null, expected);
                    }
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new SchemalessAvroDecoder(stream, context))
            {
                for (var i = 0; i < count; i++)
                {
                    var result = decoder.ReadDataValue(null);
                    Assert.Equal(expected, result);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteProgramDiagnostic2DataTypeSchema(bool concise)
        {
            // Create dummy type
            var expected = VariantVariants.Complex;
            const int count = 100;
            byte[] buffer;
            var context = new ServiceMessageContext();
            Schema schema;
            using (var stream = new MemoryStream())
            {
                using (var encoder = new AvroSchemaBuilder(stream, context, emitConciseSchemas: concise))
                {
                    encoder.WriteArray(null, Enumerable
                        .Repeat(expected, count)
                        .ToList(), v => encoder.WriteEncodeable(null, v, v.GetType()));
                    schema = encoder.Schema;
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            {
                var json = schema.ToJson();
                Assert.NotNull(json);
                using (var decoder = new AvroDecoder(stream, schema, context))
                {
                    var results = decoder.ReadArray(null,
                        () => decoder.ReadEncodeable(null, expected.GetType()));
                    Assert.Equal(count, results.Length);
                    for (var i = 0; i < count; i++)
                    {
                        Assert.True(results[i].IsEqual(expected));
                    }
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteDataValueArrayWithIntAndSchema(bool concise)
        {
            // Create dummy
            var expected = new DataValue(new Variant(12345));
            const int count = 10000;
            byte[] buffer;
            var context = new ServiceMessageContext();
            Schema schema;
            using (var stream = new MemoryStream())
            {
                using (var encoder = new AvroSchemaBuilder(stream, context, emitConciseSchemas: concise))
                {
                    encoder.WriteArray(null, Enumerable
                        .Repeat(expected, count)
                        .ToList(), v => encoder.WriteDataValue(null, v));
                    schema = encoder.Schema;
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new AvroDecoder(stream, schema, context))
            {
                var results = decoder.ReadArray(null,
                    () => decoder.ReadDataValue(null));
                Assert.Equal(count, results.Length);
                for (var i = 0; i < count; i++)
                {
                    Assert.Equal(expected, results[i]);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteDataValueArrayWithStringAndSchema(bool concise)
        {
            // Create dummy
            var expected = new DataValue(new Variant("TestTestTestTest"
                + Guid.NewGuid()));
            const int count = 10000;
            byte[] buffer;
            var context = new ServiceMessageContext();
            Schema schema;
            using (var stream = new MemoryStream())
            {
                using (var encoder = new AvroSchemaBuilder(stream, context, emitConciseSchemas: concise))
                {
                    encoder.WriteArray(null, Enumerable
                        .Repeat(expected, count)
                        .ToList(), v => encoder.WriteDataValue(null, v));
                    schema = encoder.Schema;
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new AvroDecoder(stream, schema, context))
            {
                var results = decoder.ReadArray(null,
                    () => decoder.ReadDataValue(null));
                Assert.Equal(count, results.Length);
                for (var i = 0; i < count; i++)
                {
                    Assert.Equal(expected, results[i]);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteDataSetTest1(bool concise)
        {
            // Create dummy
            var expected = new DataSet
            {
                ["abcd"] = new DataValue(new Variant(1234), StatusCodes.Good, DateTime.UtcNow, DateTime.UtcNow),
                ["http://microsoft.com"] = new DataValue(new Variant(-222222222), StatusCodes.Bad, DateTime.MinValue, DateTime.UtcNow),
                ["1111111111111111111111111"] = new DataValue(new Variant(false), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["@#$%^&*()_+~!@#$%^*(){}"] = new DataValue(new Variant(new byte[] { 0, 2, 4, 6 }), StatusCodes.Good),
                ["1245"] = new DataValue(new Variant("hello"), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["..."] = new DataValue(new Variant("imbricated"))
            };

            byte[] buffer;
            var context = new ServiceMessageContext();
            Schema schema;
            using (var stream = new MemoryStream())
            {
                using (var encoder = new AvroSchemaBuilder(stream, context, emitConciseSchemas: concise))
                {
                    encoder.WriteDataSet(null, expected);
                    schema = encoder.Schema;
                }
                buffer = stream.ToArray();
            }
            var json = schema.ToJson();
            Assert.NotNull(json);
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new AvroDecoder(stream, schema, context))
            {
                var result = decoder.ReadDataSet(null);
                Assert.True(expected.Equals(result));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteDataSetTest2(bool concise)
        {
            // Create dummy
            var expected = new DataSet
            {
                ["abcd"] = new DataValue(new Variant(1234), StatusCodes.Good, DateTime.UtcNow, DateTime.UtcNow),
                ["http://microsoft.com"] = null,
                ["1111111111111111111111111"] = new DataValue(new Variant(false), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["@#$%^&*()_+~!@#$%^*(){}"] = new DataValue(new Variant(new byte[] { 0, 2, 4, 6 }), StatusCodes.Good),
                ["1245"] = null,
                ["..."] = new DataValue(new Variant("imbricated"))
            };

            byte[] buffer;
            var context = new ServiceMessageContext();
            Schema schema;
            using (var stream = new MemoryStream())
            {
                using (var encoder = new AvroSchemaBuilder(stream, context, emitConciseSchemas: concise))
                {
                    encoder.WriteDataSet(null, expected);
                    schema = encoder.Schema;
                }
                buffer = stream.ToArray();
            }
            var json = schema.ToJson();
            Assert.NotNull(json);
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new AvroDecoder(stream, schema, context))
            {
                var result = decoder.ReadDataSet(null);
                Assert.True(expected.Equals(result));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteDataSetArrayRawTest1(bool concise)
        {
            // Create dummy
            var expected = new DataSet(DataSetFieldContentFlags.RawData)
            {
                ["abcd"] = new DataValue(new Variant(1234), StatusCodes.Good, DateTime.UtcNow, DateTime.UtcNow),
                ["http://microsoft.com"] = new DataValue(new Variant(-222222222), StatusCodes.Bad, DateTime.MinValue, DateTime.UtcNow),
                ["1111111111111111111111111"] = new DataValue(new Variant(false), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["@#$%^&*()_+~!@#$%^*(){}"] = new DataValue(new Variant(new byte[] { 0, 2, 4, 6 }), StatusCodes.Good),
                ["1245"] = new DataValue(new Variant("hello"), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["..."] = new DataValue(new Variant("imbricated"))
            };

            byte[] buffer;
            var context = new ServiceMessageContext();
            Schema schema;
            using (var stream = new MemoryStream())
            {
                using (var encoder = new AvroSchemaBuilder(stream, context, emitConciseSchemas: concise))
                {
                    encoder.WriteDataSet(null, expected);
                    schema = encoder.Schema;
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new AvroDecoder(stream, schema, context))
            {
                var result = decoder.ReadDataSet(null);
                Assert.True(expected.Equals(result));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteDataSetArrayRawTest2(bool concise)
        {
            // Create dummy
            var expected = new DataSet(DataSetFieldContentFlags.RawData)
            {
                ["abcd"] = null,
                ["http://microsoft.com"] = new DataValue(new Variant(-222222222), StatusCodes.Bad, DateTime.MinValue, DateTime.UtcNow),
                ["1111111111111111111111111"] = null,
                ["@#$%^&*()_+~!@#$%^*(){}"] = new DataValue(new Variant(new byte[] { 0, 2, 4, 6 }), StatusCodes.Good),
                ["1245"] = new DataValue(new Variant("hello"), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["..."] = null
            };

            byte[] buffer;
            var context = new ServiceMessageContext();
            Schema schema;
            using (var stream = new MemoryStream())
            {
                using (var encoder = new AvroSchemaBuilder(stream, context, emitConciseSchemas: concise))
                {
                    encoder.WriteDataSet(null, expected);
                    schema = encoder.Schema;
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new AvroDecoder(stream, schema, context))
            {
                var result = decoder.ReadDataSet(null);
                Assert.True(expected.Equals(result));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteDataSetWithSingleEntryTest(bool concise)
        {
            // Create dummy
            var expected = new DataSet
            {
                ["abcd"] = new DataValue(new Variant(1234),
                    StatusCodes.Good, DateTime.UtcNow, DateTime.UtcNow)
            };

            byte[] buffer;
            var context = new ServiceMessageContext();
            Schema schema;
            using (var stream = new MemoryStream())
            {
                using (var encoder = new AvroSchemaBuilder(stream, context, emitConciseSchemas: concise))
                {
                    encoder.WriteDataSet(null, expected);
                    schema = encoder.Schema;
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new AvroDecoder(stream, schema, context))
            {
                var result = decoder.ReadDataSet(null);
                Assert.Equal(expected["abcd"], result["abcd"]);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteDataSetWithSingleComplexEntryTest(bool concise)
        {
            // Create dummy
            var expected = new DataSet
            {
                ["abcd"] = new DataValue(new Variant(VariantVariants.Complex),
                    StatusCodes.Good, DateTime.UtcNow, DateTime.UtcNow)
            };

            byte[] buffer;
            var context = new ServiceMessageContext();
            Schema schema;
            using (var stream = new MemoryStream())
            {
                using (var encoder = new AvroSchemaBuilder(stream, context, emitConciseSchemas: concise))
                {
                    encoder.WriteDataSet(null, expected);
                    schema = encoder.Schema;
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new AvroDecoder(stream, schema, context))
            {
                var result = decoder.ReadDataSet(null);
                Assert.True(result["abcd"].Value is ExtensionObject);
                var eo = (ExtensionObject)result["abcd"].Value;
                Assert.True(eo.Body is IEncodeable);
                var e = (IEncodeable)eo.Body;
                Assert.Equal(VariantVariants.Complex.AsJson(context), e.AsJson(context));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteDataSetWithSingleValueRawTest(bool concise)
        {
            // Create dummy
            var expected = new DataSet(DataSetFieldContentFlags.RawData)
            {
                ["abcd"] = new DataValue(new Variant(1234),
                    StatusCodes.Good, DateTime.UtcNow, DateTime.UtcNow)
            };

            byte[] buffer;
            var context = new ServiceMessageContext();
            Schema schema;
            using (var stream = new MemoryStream())
            {
                using (var encoder = new AvroSchemaBuilder(stream, context, emitConciseSchemas: concise))
                {
                    encoder.WriteDataSet(null, expected);
                    schema = encoder.Schema;
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new AvroDecoder(stream, schema, context))
            {
                var result = decoder.ReadDataSet(null);
                Assert.Equal(expected["abcd"].Value, result["abcd"].Value);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteDataSetWithSingleComplexValueRawTest(bool concise)
        {
            // Create dummy
            var expected = new DataSet(DataSetFieldContentFlags.RawData)
            {
                ["abcd"] = new DataValue(new Variant(VariantVariants.Complex),
                    StatusCodes.Good, DateTime.UtcNow, DateTime.UtcNow)
            };

            byte[] buffer;
            var context = new ServiceMessageContext();
            Schema schema;
            using (var stream = new MemoryStream())
            {
                using (var encoder = new AvroSchemaBuilder(stream, context, emitConciseSchemas: concise))
                {
                    encoder.WriteDataSet(null, expected);
                    schema = encoder.Schema;
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new AvroDecoder(stream, schema, context))
            {
                var result = decoder.ReadDataSet(null);
                Assert.True(result["abcd"].Value is ExtensionObject);
                var eo = (ExtensionObject)result["abcd"].Value;
                Assert.True(eo.Body is IEncodeable);
                var e = (IEncodeable)eo.Body;
                Assert.Equal(VariantVariants.Complex.AsJson(context), e.AsJson(context));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteDataSetArrayRawStreamTest(bool concise)
        {
            // Create dummy
            var expected = new DataSet(DataSetFieldContentFlags.RawData)
            {
                ["abcd"] = new DataValue(new Variant(1234), StatusCodes.Good, DateTime.UtcNow, DateTime.UtcNow),
                ["http://microsoft.com"] = new DataValue(new Variant(-222222222), StatusCodes.Bad, DateTime.MinValue, DateTime.UtcNow),
                ["1111111111111111111111111"] = new DataValue(new Variant(false), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["@#$%^&*()_+~!@#$%^*(){}"] = new DataValue(new Variant(new byte[] { 0, 2, 4, 6 }), StatusCodes.Good),
                ["1245"] = new DataValue(new Variant("hello"), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["..."] = new DataValue(new Variant("imbricated"))
            };

            const int count = 10000;
            byte[] buffer;
            var context = new ServiceMessageContext();
            Schema schema;
            using (var stream = new MemoryStream())
            {
                using (var encoder = new AvroSchemaBuilder(stream, context, emitConciseSchemas: concise))
                {
                    encoder.WriteArray(null, Enumerable
                        .Repeat(expected, count)
                        .ToList(), v => encoder.WriteDataSet(null, v));
                    schema = encoder.Schema;
                }
                buffer = stream.ToArray();
            }
            using (var stream = new MemoryStream(buffer))
            using (var decoder = new AvroDecoder(stream, schema, context))
            {
                var results = decoder.ReadArray(null,
                    () => decoder.ReadDataSet(null));
                Assert.Equal(count, results.Length);
                for (var i = 0; i < count; i++)
                {
                    Assert.True(expected.Equals(results[i]));
                }
            }
        }
    }
}
