// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.Schemas.Json;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Json.Schema;
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;

    public class JsonNetworkMessageJsonSchemaTests
    {

        [Theory]
        [MemberData(nameof(GetMessageMetaDataFiles))]
        public async Task CreateSingleMessageSchemaWithoutHeaderAsync(string messageMetaDataFile)
        {
            var messageMetaData = await LoadAsync<PublishedNetworkMessageSchemaModel>(messageMetaDataFile);
            messageMetaData = messageMetaData with
            {
                NetworkMessageContentFlags = NetworkMessageContentFlags.SingleDataSetMessage
            };

            var schema = new JsonNetworkMessage(messageMetaData);

            var json = schema.ToString();
            await AssertAsync("Default", messageMetaDataFile, json);

            var schema2 = SchemaReader.ReadSchema(json, ".");
            Assert.NotNull(schema2);
            // var schema2 = global::Json.Schema.JsonSchema.FromText(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [MemberData(nameof(GetMessageMetaDataFiles))]
        public async Task CreateRawMessageSchemaAsync(string messageMetaDataFile)
        {
            var messageMetaData = await LoadAsync<PublishedNetworkMessageSchemaModel>(messageMetaDataFile);
            messageMetaData = messageMetaData with
            {
                NetworkMessageContentFlags = NetworkMessageContentFlags.SingleDataSetMessage,
                DataSetMessages = messageMetaData.DataSetMessages.Select(d => d with
                {
                    DataSetMessageContentFlags = 0u,
                    DataSetFieldContentFlags = DataSetFieldContentFlags.RawData
                }).ToList()
            };

            var schema = new JsonNetworkMessage(messageMetaData);

            var json = schema.ToString();
            await AssertAsync("Raw", messageMetaDataFile, json);

            var schema2 = SchemaReader.ReadSchema(json, ".");
            Assert.NotNull(schema2);
            // var schema2 = global::Json.Schema.JsonSchema.FromText(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [MemberData(nameof(GetMessageMetaDataFiles))]
        public async Task CreateRawMessageSchemaReversibleAsync(string messageMetaDataFile)
        {
            var messageMetaData = await LoadAsync<PublishedNetworkMessageSchemaModel>(messageMetaDataFile);
            messageMetaData = messageMetaData with
            {
                NetworkMessageContentFlags = NetworkMessageContentFlags.SingleDataSetMessage,
                DataSetMessages = messageMetaData.DataSetMessages.Select(d => d with
                {
                    DataSetMessageContentFlags = DataSetMessageContentFlags.ReversibleFieldEncoding,
                    DataSetFieldContentFlags = 0u

                }).ToList()
            };

            var schema = new JsonNetworkMessage(messageMetaData);

            var json = schema.ToString();
            await AssertAsync("RawReversible", messageMetaDataFile, json);

            var schema2 = SchemaReader.ReadSchema(json, ".");
            Assert.NotNull(schema2);
            // var schema2 = global::Json.Schema.JsonSchema.FromText(json);
            //Assert.Equal(schema.Schema, schema2);
        }

        [Theory]
        [MemberData(nameof(GetMessageMetaDataFiles))]
        public async Task NetworkMessageHeaderNamesTheWriterGroupTheWayItIsPublishedAsync(
            string messageMetaDataFile)
        {
            //
            // Every other case here sets SingleDataSetMessage, which returns
            // the payload schema before a network message header is described
            // at all, so none of them reaches the members below. That gap is
            // why the schema went on naming this member DataSetWriterGroup -
            // the name it took, with nameof, from the custom encoder's message
            // class - long after 3.0 started publishing WriterGroupName.
            //
            var messageMetaData = await LoadAsync<PublishedNetworkMessageSchemaModel>(
                messageMetaDataFile);
            messageMetaData = messageMetaData with
            {
                NetworkMessageContentFlags =
                    NetworkMessageContentFlags.NetworkMessageHeader |
                    NetworkMessageContentFlags.DataSetMessageHeader |
                    NetworkMessageContentFlags.PublisherId
            };

            var json = new JsonNetworkMessage(messageMetaData).ToString();

            Assert.Contains("\"WriterGroupName\"", json, StringComparison.Ordinal);
            Assert.DoesNotContain("DataSetWriterGroup", json, StringComparison.Ordinal);
            Assert.Contains("\"PublisherId\"", json, StringComparison.Ordinal);
            Assert.Contains("\"MessageId\"", json, StringComparison.Ordinal);
            Assert.NotNull(SchemaReader.ReadSchema(json, "."));
        }

        private static async ValueTask<T> LoadAsync<T>(string file)
        {
            await using var fs = new FileStream(file, FileMode.Open,
                FileAccess.Read, FileShare.Read);
            return await JsonSerializer.DeserializeAsync<T>(fs);
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
#if WRITE
            var folder = Path.Combine(".", "JsonSchema", name);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            await File.WriteAllTextAsync(Path.Combine(folder, Path.GetFileName(messageMetaDataFile)), json);
#else
            var folder = Path.Combine(".", "Encoders", "Schemas", "JsonSchema", name);
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
