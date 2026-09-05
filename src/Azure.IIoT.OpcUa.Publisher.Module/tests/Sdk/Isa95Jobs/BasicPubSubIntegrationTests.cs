// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.Isa95Jobs
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using ReferenceServerIntegrationTests =
        Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.ReferenceServer.BasicPubSubIntegrationTests;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class BasicPubSubIntegrationTests : PublisherIntegrationTestBase, IClassFixture<Isa95JobsServer>
    {
        internal const string EventId = "EventId";
        internal const string Message = "Message";
        private readonly ITestOutputHelper _output;
        private readonly Isa95JobsServer _fixture;

        public BasicPubSubIntegrationTests(Isa95JobsServer fixture, ITestOutputHelper output)
            : base(output)
        {
            _output = output;
            _fixture = fixture;
            EndpointUrl = _fixture.EndpointUrl;
        }

        [Fact]
        public async Task CanEncodeWithReversibleEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                nameof(CanEncodeWithReversibleEncodingTestAsync),
                "./Resources/Isa95Jobs.json", TimeSpan.FromMinutes(2), 4,
                ReferenceServerIntegrationTests.GetEventNetworkMessage,
                messageType: "ua-data",
                arguments: ["--mm=PubSub", "--me=JsonReversible", "--dm=false"]
            );

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                AssertIsa95Payload(m.GetProperty("Payload"));
            });
        }

        [Fact]
        public async Task CanEncodeEventWithCompliantEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                nameof(CanEncodeEventWithCompliantEncodingTestAsync),
                "./Resources/Isa95Jobs.json",
                ReferenceServerIntegrationTests.GetEventNetworkMessage,
                messageType: "ua-data",
                arguments: ["-c", "--mm=PubSub", "--me=Json"]);

            Assert.Single(result);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                AssertIsa95Payload(m.GetProperty("Payload"));
            });
        }

        [Fact]
        public async Task CanEncodeWithReversibleEncodingAndWithCompliantEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                nameof(CanEncodeWithReversibleEncodingAndWithCompliantEncodingTestAsync),
                "./Resources/Isa95Jobs.json", TimeSpan.FromMinutes(2), 4,
                ReferenceServerIntegrationTests.GetEventNetworkMessage,
                messageType: "ua-data",
                arguments: ["-c", "--mm=PubSub", "--me=JsonReversible"]);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                AssertIsa95Payload(m.GetProperty("Payload"));
            });
        }

        private static void AssertIsa95Payload(JsonElement payload)
        {
            Assert.Equal(
                JsonValueKind.String,
                GetStructuredValue(GetValue(payload.GetProperty(EventId))).ValueKind);
            AssertLocalizedText(GetStructuredValue(GetValue(payload.GetProperty(Message))));

            var jobResponse = GetStructuredValue(
                GetValue(GetIsa95Field(payload, "JobResponse")));
            var equipmentActuals = jobResponse.GetProperty("EquipmentActuals");
            var materialActuals = jobResponse.GetProperty("MaterialActuals");

            Assert.Equal(JsonValueKind.Array, equipmentActuals.ValueKind);
            Assert.Equal(JsonValueKind.Array, materialActuals.ValueKind);
            Assert.Equal(2, equipmentActuals.GetArrayLength());
            Assert.Equal(2, materialActuals.GetArrayLength());
            Assert.All(equipmentActuals.EnumerateArray(), equipment =>
            {
                Assert.Equal("consumable", equipment.GetProperty("EquipmentUse").GetString());
                Assert.Equal(JsonValueKind.String, equipment.GetProperty("Quantity").ValueKind);
            });
            Assert.All(materialActuals.EnumerateArray(), material =>
            {
                Assert.True(material.GetProperty("MaterialClassID").TryGetGuid(out _));
                Assert.Equal("consumable", material.GetProperty("MaterialUse").GetString());
                Assert.Equal(JsonValueKind.String, material.GetProperty("Quantity").ValueKind);
            });
        }

        private static JsonElement GetIsa95Field(JsonElement payload, string browseName)
        {
            return payload.EnumerateObject()
                .Where(property => property.Name.Equals(browseName, StringComparison.Ordinal) ||
                    property.Name.EndsWith($";{browseName}", StringComparison.Ordinal) ||
                    property.Name.EndsWith($"#{browseName}", StringComparison.Ordinal))
                .Select(property => property.Value)
                .Single();
        }

        private static JsonElement GetStructuredValue(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Object &&
                value.TryGetProperty("Body", out var body))
            {
                return body.ValueKind == JsonValueKind.Object &&
                    body.TryGetProperty("Body", out var nestedBody)
                    ? nestedBody
                    : body;
            }
            return value;
        }

        private static JsonElement GetValue(JsonElement value)
        {
            return value.ValueKind == JsonValueKind.Object &&
                value.TryGetProperty("Value", out var encodedValue)
                ? encodedValue
                : value;
        }

        private static void AssertLocalizedText(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.String)
            {
                return;
            }

            Assert.Equal(JsonValueKind.Object, value.ValueKind);
            Assert.Equal(JsonValueKind.String, value.GetProperty("Text").ValueKind);
            Assert.Equal("en-US", value.GetProperty("Locale").GetString());
        }
    }
}
