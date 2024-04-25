// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.PubSub.Schemas;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Json.Schema;
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
        [InlineData("./Resources/MultiWriterWithError.json")]
        [InlineData("./Resources/MultiDataWriter.json")]
        [InlineData("./Resources/SingleKeepAlive.json")]
        public async Task CreateNetworkMessageJsonSchemas(string writerGroupFile)
        {
            var group = await LoadAsync<WriterGroupModel>(writerGroupFile);
            var schema = new JsonNetworkMessageJsonSchema(group);

            var json = schema.ToString();
            var document = JsonDocument.Parse(json);
            json = JsonSerializer.Serialize(document, kIndented);
            Assert.NotNull(json);
            await AssertAsync("NetworkMessageDefault", writerGroupFile, json);

            var schema2 = SchemaReader.ReadSchema(json, ".");
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        [InlineData("./Resources/MultiWriterWithError.json")]
        [InlineData("./Resources/MultiDataWriter.json")]
        [InlineData("./Resources/SingleKeepAlive.json")]
        public async Task CreateJsonNetworkMessageWithNs(string writerGroupFile)
        {
            var group = await LoadAsync<WriterGroupModel>(writerGroupFile);
            var schema = new JsonNetworkMessageJsonSchema(group, new SchemaOptions
            {
                Namespace = "http://www.microsoft.com"
            });

            var json = schema.ToString();
            await AssertAsync("NetworkMessage", writerGroupFile, json);
            var schema2 = SchemaReader.ReadSchema(json, ".");
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        [InlineData("./Resources/MultiWriterWithError.json")]
        [InlineData("./Resources/MultiDataWriter.json")]
        [InlineData("./Resources/SingleKeepAlive.json")]
        public async Task CreateLegacyJsonNetworkMessage(string writerGroupFile)
        {
            var group = await LoadAsync<WriterGroupModel>(writerGroupFile);
            var schema = new JsonNetworkMessageJsonSchema(group);

            var json = schema.ToString();
            await AssertAsync("Legacy", writerGroupFile, json);

            var schema2 = SchemaReader.ReadSchema(json, ".");
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        [InlineData("./Resources/MultiWriterWithError.json")]
        [InlineData("./Resources/MultiDataWriter.json")]
        [InlineData("./Resources/SingleKeepAlive.json")]
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

            var schema = new JsonNetworkMessageJsonSchema(group);

            var json = schema.ToString();
            await AssertAsync("Multiple", writerGroupFile, json);

            var schema2 = SchemaReader.ReadSchema(json, ".");
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        [InlineData("./Resources/MultiWriterWithError.json")]
        [InlineData("./Resources/MultiDataWriter.json")]
        [InlineData("./Resources/SingleKeepAlive.json")]
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

            var schema = new JsonNetworkMessageJsonSchema(group);

            var json = schema.ToString();
            await AssertAsync("Single", writerGroupFile, json);

            var schema2 = SchemaReader.ReadSchema(json, ".");
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        [InlineData("./Resources/MultiWriterWithError.json")]
        [InlineData("./Resources/MultiDataWriter.json")]
        [InlineData("./Resources/SingleKeepAlive.json")]
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

            var schema = new JsonNetworkMessageJsonSchema(group);

            var json = schema.ToString();
            await AssertAsync("Default", writerGroupFile, json);

            var schema2 = SchemaReader.ReadSchema(json, ".");
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        [InlineData("./Resources/MultiWriterWithError.json")]
        [InlineData("./Resources/MultiDataWriter.json")]
        [InlineData("./Resources/SingleKeepAlive.json")]
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

            var schema = new JsonNetworkMessageJsonSchema(group);

            var json = schema.ToString();
            await AssertAsync("Raw", writerGroupFile, json);

            var schema2 = SchemaReader.ReadSchema(json, ".");
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        [InlineData("./Resources/MultiWriterWithError.json")]
        [InlineData("./Resources/MultiDataWriter.json")]
        [InlineData("./Resources/SingleKeepAlive.json")]
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

            var schema = new JsonNetworkMessageJsonSchema(group);

            var json = schema.ToString();
            await AssertAsync("RawReversible", writerGroupFile, json);

            var schema2 = SchemaReader.ReadSchema(json, ".");
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

        private static async Task AssertAsync(string name, string writerGroupFile, string json)
        {
            var document = JsonDocument.Parse(json);
            json = JsonSerializer.Serialize(document, kIndented).ReplaceLineEndings();
            Assert.NotNull(json);
#if WRITE
            var folder = Path.Combine(".", "JsonSchema", name);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            await File.WriteAllTextAsync(Path.Combine(folder, Path.GetFileName(writerGroupFile)), json);
#else
            var folder = Path.Combine(".", "Encoders", "Schemas", "JsonSchema", name);
            var expected = await File.ReadAllTextAsync(Path.Combine(folder, Path.GetFileName(writerGroupFile)));
            Assert.Equal(expected.ReplaceLineEndings(), json);
#endif
        }
    }
}
