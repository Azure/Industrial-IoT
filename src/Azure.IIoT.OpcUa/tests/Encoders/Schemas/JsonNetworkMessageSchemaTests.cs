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

    public class JsonNetworkMessageSchemaTests
    {
        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        public async Task CreateJsonNetworkMessageSchemas(string writerGroupFile)
        {
            var group = await LoadAsync<WriterGroupModel>(writerGroupFile);
            var schema = new JsonNetworkMessageSchema(group);

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
        public async Task CreateJsonNetworkMessageWithNs(string writerGroupFile)
        {
            var group = await LoadAsync<WriterGroupModel>(writerGroupFile);
            var schema = new JsonNetworkMessageSchema(group, new SchemaGenerationOptions
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
        public async Task CreateLegacyJsonNetworkMessage(string writerGroupFile)
        {
            var group = await LoadAsync<WriterGroupModel>(writerGroupFile);
            var schema = new JsonNetworkMessageSchema(group);

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

            var schema = new JsonNetworkMessageSchema(group);

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

            var schema = new JsonNetworkMessageSchema(group);

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

            var schema = new JsonNetworkMessageSchema(group);

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

            var schema = new JsonNetworkMessageSchema(group);

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

            var schema = new JsonNetworkMessageSchema(group);

            var json = schema.ToString();
            var document = JsonDocument.Parse(json);
            json = JsonSerializer.Serialize(document, kIndented);
            Assert.NotNull(json);
            var schema2 = global::Avro.Schema.Parse(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        // [Theory]
        // [InlineData("./Resources/SingleDataWriter.json")]
        // public async Task ValidateValueAgainstSchema(string writerGroupFile)
        // {
        //     var value = /*lang=json,strict*/ "{\"MessageId\":\"e7a67d55-b247-4c82-9638-7d247d0b4232\",\"MessageType\":\"ua-data\",\"PublisherId\":\"iothub-lvmdwa_device_desktop-bgfjs91_module_publisher\",\"DataSetWriterGroup\":\"<<UnknownWriterGroup>>\",\"Messages\":[{\"MetaDataVersion\":{\"MajorVersion\":1,\"MinorVersion\":1},\"MessageType\":\"ua-deltaframe\",\"DataSetWriterName\":\"<<UnknownDataSet>>\",\"Payload\":{\"Output\":{\"Value\":0.77700000000000002,\"SourceTimestamp\":\"2024-03-11T18:43:53.1957727Z\"}}}]}"
        //     var group = await LoadAsync<WriterGroupModel>(writerGroupFile);
        //     var schema = new JsonNetworkMessageSchema(group);
        //
        //     var json = schema.ToString();
        //     var document = JsonDocument.Parse(json);
        //     json = JsonSerializer.Serialize(document, kIndented);
        //     var schema2 = Avro.Schema.Parse(json);
        //     Avro
        // }

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
