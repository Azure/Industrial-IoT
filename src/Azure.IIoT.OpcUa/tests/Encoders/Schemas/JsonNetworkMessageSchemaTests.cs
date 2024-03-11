// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using System.IO;
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
        }

        [Theory]
        [InlineData("./Resources/SingleDataWriter.json")]
        [InlineData("./Resources/SingleEventWriter.json")]
        [InlineData("./Resources/SingleWithExtensions.json")]
        [InlineData("./Resources/SinglePendingAlarm.json")]
        public async Task CreateJsonNetworkMessageWithNs(string writerGroupFile)
        {
            var group = await LoadAsync<WriterGroupModel>(writerGroupFile);
            var schema = new JsonNetworkMessageSchema(group, "http://www.microsoft.com");

            var json = schema.ToString();
            var document = JsonDocument.Parse(json);
            json = JsonSerializer.Serialize(document, kIndented);
            Assert.NotNull(json);
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
        }

        private static async ValueTask<T> LoadAsync<T>(string file)
        {
            var serializer = new NewtonsoftJsonSerializer() ;
            await using (var fs = new FileStream(file, FileMode.Open))
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
