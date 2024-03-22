// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Avro
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;

    public class AvroNetworkMessageSchemaTests
    {
        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        public async Task CreateAvroNetworkMessageSchemas(string writerGroupFile)
        {
            var group = await LoadAsync<WriterGroupModel>(writerGroupFile);
            var schema = new AvroNetworkMessageSchema(group);

            var json = schema.ToString();
            var document = JsonDocument.Parse(json);
            json = JsonSerializer.Serialize(document, kIndented);
            Assert.NotNull(json);
            var schema2 = global::Avro.Schema.Parse(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        public async Task CreateAvroNetworkMessageWithNs(string writerGroupFile)
        {
            var group = await LoadAsync<WriterGroupModel>(writerGroupFile);
            var schema = new AvroNetworkMessageSchema(group, new SchemaGenerationOptions
            {
                Namespace = "http://www.microsoft.com"
            });

            var json = schema.ToString();
            var document = JsonDocument.Parse(json);
            json = JsonSerializer.Serialize(document, kIndented);
            Assert.NotNull(json);

            var schema2 = global::Avro.Schema.Parse(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        public async Task CreateMessageSchemaWithoutNetworkHeader(string writerGroupFile)
        {
            var group = await LoadAsync<WriterGroupModel>(writerGroupFile);
            group = group with
            {
                MessageSettings = group.MessageSettings with
                {
                    NetworkMessageContentMask =
                        NetworkMessageContentMask.DataSetMessageHeader
                }
            };

            var schema = new AvroNetworkMessageSchema(group);

            var json = schema.ToString();
            var document = JsonDocument.Parse(json);
            json = JsonSerializer.Serialize(document, kIndented);
            Assert.NotNull(json);
            var schema2 = global::Avro.Schema.Parse(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        public async Task CreateSingleMessageSchema(string writerGroupFile)
        {
            var group = await LoadAsync<WriterGroupModel>(writerGroupFile);
            group = group with
            {
                MessageSettings = group.MessageSettings with
                {
                    NetworkMessageContentMask =
                        NetworkMessageContentMask.DataSetMessageHeader
                        | NetworkMessageContentMask.SingleDataSetMessage
                }
            };

            var schema = new AvroNetworkMessageSchema(group);

            var json = schema.ToString();
            var document = JsonDocument.Parse(json);
            json = JsonSerializer.Serialize(document, kIndented);
            Assert.NotNull(json);
            var schema2 = global::Avro.Schema.Parse(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        public async Task CreateSingleMessageSchemaWithoutHeader(string writerGroupFile)
        {
            var group = await LoadAsync<WriterGroupModel>(writerGroupFile);
            group = group with
            {
                MessageSettings = group.MessageSettings with
                {
                    NetworkMessageContentMask =
                        NetworkMessageContentMask.SingleDataSetMessage
                }
            };

            var schema = new AvroNetworkMessageSchema(group);

            var json = schema.ToString();
            var document = JsonDocument.Parse(json);
            json = JsonSerializer.Serialize(document, kIndented);
            Assert.NotNull(json);
            var schema2 = global::Avro.Schema.Parse(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        public async Task CreateRawMessageSchema(string writerGroupFile)
        {
            var group = await LoadAsync<WriterGroupModel>(writerGroupFile);
            group = group with
            {
                MessageSettings = group.MessageSettings with
                {
                    NetworkMessageContentMask =
                        NetworkMessageContentMask.SingleDataSetMessage
                },
                DataSetWriters = group.DataSetWriters.Select(d => d with
                {
                    MessageSettings = d.MessageSettings with
                    {
                        DataSetMessageContentMask = 0u
                    },
                    DataSetFieldContentMask = DataSetFieldContentMask.RawData
                }).ToList()
            };

            var schema = new AvroNetworkMessageSchema(group);

            var json = schema.ToString();
            var document = JsonDocument.Parse(json);
            json = JsonSerializer.Serialize(document, kIndented);
            Assert.NotNull(json);
            var schema2 = global::Avro.Schema.Parse(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        public async Task CreateRawMessageSchemaReversible(string writerGroupFile)
        {
            var group = await LoadAsync<WriterGroupModel>(writerGroupFile);
            group = group with
            {
                MessageSettings = group.MessageSettings with
                {
                    NetworkMessageContentMask = NetworkMessageContentMask.SingleDataSetMessage
                },
                DataSetWriters = group.DataSetWriters.Select(d => d with
                {
                    MessageSettings = d.MessageSettings with
                    {
                        DataSetMessageContentMask = DataSetContentMask.ReversibleFieldEncoding
                    },
                    DataSetFieldContentMask = 0u

                }).ToList()
            };

            var schema = new AvroNetworkMessageSchema(group);

            var json = schema.ToString();
            var document = JsonDocument.Parse(json);
            json = JsonSerializer.Serialize(document, kIndented);
            Assert.NotNull(json);
            var schema2 = global::Avro.Schema.Parse(json);
            
            //Assert.Equal(schema.Schema, schema2);
        }

        private static async ValueTask<T> LoadAsync<T>(string file)
        {
            var serializer = new NewtonsoftJsonSerializer() ;
            await using (var fs = new FileStream(file, FileMode.Open,
                FileAccess.Read, FileShare.Read))
            {
                return await ((IJsonSerializer)serializer).DeserializeAsync<T>(fs);
            }
        }

        private static readonly JsonSerializerOptions kIndented = new()
        {
            WriteIndented = true
        };
    }
}
