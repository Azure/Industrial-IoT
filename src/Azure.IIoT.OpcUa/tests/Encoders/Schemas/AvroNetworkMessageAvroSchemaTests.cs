// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.Schemas.Avro;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;

    public class AvroNetworkMessageAvroSchemaTests
    {
        [Theory]
        [MemberData(nameof(GetMessageMetaDataFiles))]
        public async Task CreateAvroNetworkMessageSchemas(string messageMetaDataFile)
        {
            var message = await LoadAsync<PublishedNetworkMessageSchemaModel>(messageMetaDataFile);
            var schema = new AvroNetworkMessage(message);

            var json = schema.ToString();
            var document = JsonDocument.Parse(json);
            json = JsonSerializer.Serialize(document, kIndented);
            Assert.NotNull(json);
            await AssertAsync("NetworkMessageDefault", messageMetaDataFile, json);

            var schema2 = global::Avro.Schema.Parse(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [MemberData(nameof(GetMessageMetaDataFiles))]
        public async Task CreateAvroNetworkMessageWithNs(string messageMetaDataFile)
        {
            var message = await LoadAsync<PublishedNetworkMessageSchemaModel>(messageMetaDataFile);
            var schema = new AvroNetworkMessage(message, new SchemaOptions
            {
                Namespace = "http://www.microsoft.com"
            });

            var json = schema.ToString();
            await AssertAsync("NetworkMessage", messageMetaDataFile, json);

            var schema2 = global::Avro.Schema.Parse(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [MemberData(nameof(GetMessageMetaDataFiles))]
        public async Task CreateMessageSchemaWithoutNetworkHeader(string messageMetaDataFile)
        {
            var message = await LoadAsync<PublishedNetworkMessageSchemaModel>(messageMetaDataFile);
            message = message with
            {
                NetworkMessageContentFlags =
                    NetworkMessageContentFlags.DataSetMessageHeader
            };

            var schema = new AvroNetworkMessage(message);

            var json = schema.ToString();
            await AssertAsync("Multiple", messageMetaDataFile, json);

            var schema2 = global::Avro.Schema.Parse(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [MemberData(nameof(GetMessageMetaDataFiles))]
        public async Task CreateSingleMessageSchema(string messageMetaDataFile)
        {
            var message = await LoadAsync<PublishedNetworkMessageSchemaModel>(messageMetaDataFile);
            message = message with
            {
                NetworkMessageContentFlags =
                    NetworkMessageContentFlags.DataSetMessageHeader
                    | NetworkMessageContentFlags.SingleDataSetMessage
            };

            var schema = new AvroNetworkMessage(message);

            var json = schema.ToString();
            await AssertAsync("Single", messageMetaDataFile, json);

            var schema2 = global::Avro.Schema.Parse(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [MemberData(nameof(GetMessageMetaDataFiles))]
        public async Task CreateSingleMessageSchemaWithoutHeader(string messageMetaDataFile)
        {
            var message = await LoadAsync<PublishedNetworkMessageSchemaModel>(messageMetaDataFile);
            message = message with
            {
                NetworkMessageContentFlags =
                        NetworkMessageContentFlags.SingleDataSetMessage
            };

            var schema = new AvroNetworkMessage(message);

            var json = schema.ToString();
            await AssertAsync("Default", messageMetaDataFile, json);

            var schema2 = global::Avro.Schema.Parse(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [MemberData(nameof(GetMessageMetaDataFiles))]
        public async Task CreateRawMessageSchema(string messageMetaDataFile)
        {
            var message = await LoadAsync<PublishedNetworkMessageSchemaModel>(messageMetaDataFile);
            message = message with
            {
                NetworkMessageContentFlags =
                    NetworkMessageContentFlags.SingleDataSetMessage,
                DataSetMessages = message.DataSetMessages.Select(d => d with
                {
                    DataSetMessageContentFlags = 0u,
                    DataSetFieldContentFlags = DataSetFieldContentFlags.RawData
                }).ToList()
            };

            var schema = new AvroNetworkMessage(message);

            var json = schema.ToString();
            await AssertAsync("Raw", messageMetaDataFile, json);

            var schema2 = global::Avro.Schema.Parse(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [MemberData(nameof(GetMessageMetaDataFiles))]
        public async Task CreateRawMessageSchemaReversible(string messageMetaDataFile)
        {
            var message = await LoadAsync<PublishedNetworkMessageSchemaModel>(messageMetaDataFile);
            message = message with
            {
                NetworkMessageContentFlags = NetworkMessageContentFlags.SingleDataSetMessage,
                DataSetMessages = message.DataSetMessages.Select(d => d with
                {
                    DataSetMessageContentFlags = DataSetMessageContentFlags.ReversibleFieldEncoding,
                    DataSetFieldContentFlags = 0u

                }).ToList()
            };

            var schema = new AvroNetworkMessage(message);

            var json = schema.ToString();
            await AssertAsync("RawReversible", messageMetaDataFile, json);

            var schema2 = global::Avro.Schema.Parse(json);

            //Assert.Equal(schema.Schema, schema2);
        }

        private static async ValueTask<T> LoadAsync<T>(string file)
        {
            var serializer = new NewtonsoftJsonSerializer();
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

        private static async Task AssertAsync(string name, string messageMetaDataFile, string json)
        {
            var document = JsonDocument.Parse(json);
            json = JsonSerializer.Serialize(document, kIndented).ReplaceLineEndings();
            Assert.NotNull(json);
#if !WRITE
            var folder = Path.Combine(".", "AvroSchema", name);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            await File.WriteAllTextAsync(Path.Combine(folder, Path.GetFileName(messageMetaDataFile)), json);
#else
            var folder = Path.Combine(".", "Encoders", "Schemas", "AvroSchema", name);
            var expected = await File.ReadAllTextAsync(Path.Combine(folder, Path.GetFileName(messageMetaDataFile)));
            Assert.Equal(expected.ReplaceLineEndings(), json);
#endif
        }

        public static TheoryData<string> GetMessageMetaDataFiles()
        {
            var resources = Directory.GetFiles(Path.Combine(".", "Resources"), "*.json");
            return new TheoryData<string>(resources);
        }
    }
}
