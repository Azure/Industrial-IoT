// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.ReferenceServer
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.ReferenceServer;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt;
    using Json.More;
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(MqttReferenceServerCollection.Name)]
    public class MqttPubSubIntegrationTests : PublisherIntegrationTestBase, IClassFixture<ReferenceServer>
    {
        private readonly ReferenceServer _fixture;
        private readonly ITestOutputHelper _output;

        public MqttPubSubIntegrationTests(ReferenceServer fixture, ITestOutputHelper output) : base(output)
        {
            _output = output;
            _fixture = fixture;
            EndpointUrl = _fixture.EndpointUrl;
        }

        [Fact]
        public async Task CanSendDataItemToMqttBrokerTestAsync()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemToMqttBrokerTestAsync), "./Resources/DataItems.json",
                messageType: "ua-data", arguments: ["--mm=PubSub", "--mdt={TelemetryTopic}/metadatamessage", "--dm=False"],
                version: MqttVersion.v311);

            // Assert
            var message = Assert.Single(messages);
            var output = message.Message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

            Assert.NotNull(metadata);
            Assert.EndsWith("/metadatamessage", metadata.Value.Topic, StringComparison.Ordinal);
        }

        [Fact]
        public async Task NativePubSubRuntimePublishesDataItemsToMqttBrokerAsync()
        {
            //
            // Preview path: the same scenario routed through the native OPC UA
            // PubSub runtime instead of the custom encoder sink. MQTT is used
            // because the native egress requires a transport that declares
            // quality of service and schema capabilities.
            //
            // Act
            var (_, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(NativePubSubRuntimePublishesDataItemsToMqttBrokerAsync),
                "./Resources/DataItems.json", TimeSpan.FromMinutes(2), 20,
                messageType: "ua-data",
                arguments: ["--mm=PubSub", "--dm=False", "--ps=False", "--unp=True"],
                version: MqttVersion.v5);

            // Assert
            Assert.NotEmpty(messages);
            //
            // The runtime emits an initial key frame before any value has been
            // observed, so the first message carries an empty payload.
            //
            var output = messages
                .Select(message => message.Message.GetProperty("Messages")[0]
                    .GetProperty("Payload"))
                .Where(payload => payload.TryGetProperty("Output", out _))
                .Select(payload => payload.GetProperty("Output"))
                .FirstOrDefault();

            Assert.NotEqual(JsonValueKind.Undefined, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(),
                double.MinValue, double.MaxValue);
        }

        [Fact]
        public async Task CanSendDataItemButNotMetaDataWhenMetaDataIsDisabledTestAsync()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemButNotMetaDataWhenMetaDataIsDisabledTestAsync), "./Resources/DataItems.json",
                arguments: ["--dm", "--mm=DataSetMessages"],
                version: MqttVersion.v5);

            // Assert
            var message = Assert.Single(messages);
            var output = message.Message.GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

            Assert.Null(metadata);
        }

        [Fact]
        public async Task CanSendDataItemAsDataSetMessagesToMqttBrokerWithCompliantEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemAsDataSetMessagesToMqttBrokerWithCompliantEncodingTestAsync),
                "./Resources/DataItems.json", messageType: "ua-deltaframe",
                arguments: ["-c", "--mm=DataSetMessages"],
                version: MqttVersion.v311);

            // Assert
            var message = Assert.Single(messages);
            var output = message.Message.GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

            Assert.NotNull(metadata);
        }

        [Fact]
        public async Task CanSendDataItemAsRawDataSetsToMqttBrokerWithCompliantEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemAsRawDataSetsToMqttBrokerWithCompliantEncodingTestAsync),
                "./Resources/DataItems.json", messageType: "ua-deltaframe",
                arguments: ["-c", "--dm=False", "--mm=RawDataSets", "--mdt"],
                version: MqttVersion.v5);

            // Assert
            var output = Assert.Single(messages);
            Assert.NotEqual(JsonValueKind.Null, output.Message.ValueKind);
            Assert.InRange(output.Message.GetProperty("Output").GetDouble(),
                double.MinValue, double.MaxValue);

            // Explicitely enabled metadata despite messaging profile
            Assert.NotNull(metadata);
            Assert.EndsWith("/metadata", metadata.Value.Topic, StringComparison.Ordinal);
        }

        [Fact]
        public async Task CanEncodeWithoutReversibleEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(nameof(CanEncodeWithoutReversibleEncodingTestAsync),
                "./Resources/SimpleEvents.json", messageType: "ua-data", arguments: ["--mm=PubSub", "--me=Json", "--dm=false"],
                version: MqttVersion.v5);

            Assert.Single(result);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                var value = m.GetProperty("Payload");

                // Variant encoding is the default
                var eventId = value.GetProperty(BasicPubSubIntegrationTests.EventId).GetProperty("Value");
                var message = value.GetProperty(BasicPubSubIntegrationTests.Message).GetProperty("Value");
                var cycleId = value.GetProperty(BasicPubSubIntegrationTests.CycleIdUri).GetProperty("Value");
                var currentStep = value.GetProperty(BasicPubSubIntegrationTests.CurrentStepUri).GetProperty("Value");

                Assert.Equal(JsonValueKind.String, eventId.ValueKind);
                Assert.Equal(JsonValueKind.String, message.ValueKind);
                Assert.Equal(JsonValueKind.String, cycleId.ValueKind);
                Assert.Equal(JsonValueKind.String, currentStep.GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, currentStep.GetProperty("Duration").ValueKind);
            });

            Assert.NotNull(metadata);
            BasicPubSubIntegrationTests.AssertSimpleEventsMetadata(metadata.Value);
        }

        [Fact]
        public async Task CanEncodeWithReversibleEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(nameof(CanEncodeWithReversibleEncodingTestAsync),
                "./Resources/SimpleEvents.json", TimeSpan.FromMinutes(2), 4, messageType: "ua-data",
                arguments: ["--mm=PubSub", "--me=JsonReversible", "--dm=False"],
                version: MqttVersion.v311);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                var body = m.GetProperty("Payload");
                var eventId = body.GetProperty(BasicPubSubIntegrationTests.EventId).GetProperty("Value");
                Assert.Equal("ByteString", eventId.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, eventId.GetProperty("Body").ValueKind);

                var message = body.GetProperty(BasicPubSubIntegrationTests.Message).GetProperty("Value");
                Assert.Equal("LocalizedText", message.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, message.GetProperty("Body").GetProperty("Text").ValueKind);
                Assert.Equal("en-US", message.GetProperty("Body").GetProperty("Locale").GetString());

                var cycleId = body.GetProperty(BasicPubSubIntegrationTests.CycleIdUri).GetProperty("Value");
                Assert.Equal("String", cycleId.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, cycleId.GetProperty("Body").ValueKind);

                var currentStep = body.GetProperty(BasicPubSubIntegrationTests.CurrentStepUri).GetProperty("Value");
                body = currentStep.GetProperty("Body");
                Assert.Equal("ExtensionObject", currentStep.GetProperty("Type").GetString());
                Assert.Equal("http://opcfoundation.org/SimpleEvents#i=183", body.GetProperty("TypeId").GetString());
                Assert.Equal("Json", body.GetProperty("Encoding").GetString());
                Assert.Equal(JsonValueKind.String, body.GetProperty("Body").GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, body.GetProperty("Body").GetProperty("Duration").ValueKind);
            });

            Assert.NotNull(metadata);
            BasicPubSubIntegrationTests.AssertSimpleEventsMetadata(metadata.Value);
        }

        [Fact]
        public async Task CanEncodeEventWithCompliantEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(nameof(CanEncodeEventWithCompliantEncodingTestAsync),
                "./Resources/SimpleEvents.json", messageType: "ua-data", arguments: ["-c", "--mm=PubSub", "--me=Json"],
                version: MqttVersion.v5);

            Assert.Single(result);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                var value = m.GetProperty("Payload");

                // Variant encoding is the default
                var eventId = value.GetProperty(BasicPubSubIntegrationTests.EventId).GetProperty("Value");
                var message = value.GetProperty(BasicPubSubIntegrationTests.Message).GetProperty("Value");
                var cycleId = value.GetProperty(BasicPubSubIntegrationTests.CycleIdExpanded).GetProperty("Value");
                var currentStep = value.GetProperty(BasicPubSubIntegrationTests.CurrentStepExpanded).GetProperty("Value");

                Assert.Equal(JsonValueKind.String, eventId.ValueKind);
                Assert.Equal(JsonValueKind.String, message.ValueKind);
                Assert.Equal(JsonValueKind.String, cycleId.ValueKind);
                Assert.Equal(JsonValueKind.String, currentStep.GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, currentStep.GetProperty("Duration").ValueKind);
            });

            Assert.NotNull(metadata);
            BasicPubSubIntegrationTests.AssertCompliantSimpleEventsMetadata(metadata.Value);
        }

        [Fact]
        public async Task CanEncodeWithReversibleEncodingAndWithCompliantEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(nameof(CanEncodeWithReversibleEncodingAndWithCompliantEncodingTestAsync),
                "./Resources/SimpleEvents.json", TimeSpan.FromMinutes(2), 4, messageType: "ua-data",
                arguments: ["-c", "--mm=PubSub", "--me=JsonReversible"],
                version: MqttVersion.v311);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                var body = m.GetProperty("Payload");
                var eventId = body.GetProperty(BasicPubSubIntegrationTests.EventId).GetProperty("Value");
                Assert.Equal(15, eventId.GetProperty("Type").GetInt32());
                Assert.Equal(JsonValueKind.String, eventId.GetProperty("Body").ValueKind);

                var message = body.GetProperty(BasicPubSubIntegrationTests.Message).GetProperty("Value");
                Assert.Equal(21, message.GetProperty("Type").GetInt32());
                Assert.Equal(JsonValueKind.String, message.GetProperty("Body").GetProperty("Text").ValueKind);
                Assert.Equal("en-US", message.GetProperty("Body").GetProperty("Locale").GetString());

                var cycleId = body.GetProperty(BasicPubSubIntegrationTests.CycleIdExpanded).GetProperty("Value");
                Assert.Equal(12, cycleId.GetProperty("Type").GetInt32());
                Assert.Equal(JsonValueKind.String, cycleId.GetProperty("Body").ValueKind);

                var currentStep = body.GetProperty(BasicPubSubIntegrationTests.CurrentStepExpanded).GetProperty("Value");
                body = currentStep.GetProperty("Body");
                Assert.Equal(22, currentStep.GetProperty("Type").GetInt32());
                Assert.Equal(183, body.GetProperty("TypeId").GetProperty("Id").GetInt32());
                Assert.Equal(JsonValueKind.String, body.GetProperty("Body").GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, body.GetProperty("Body").GetProperty("Duration").ValueKind);
            });

            Assert.NotNull(metadata);
            BasicPubSubIntegrationTests.AssertCompliantSimpleEventsMetadata(metadata.Value);
        }

        [Fact]
        public async Task CanSendPendingConditionsToMqttBrokerTestAsync()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(nameof(CanSendPendingConditionsToMqttBrokerTestAsync),
                "./Resources/PendingAlarms.json", BasicPubSubIntegrationTests.GetAlarmCondition, messageType: "ua-data",
                arguments: ["--mm=PubSub", "--dm=False"], version: MqttVersion.v311);

            // Assert
            var message = Assert.Single(messages);
            _output.WriteLine(message.Topic + message.Message.ToJsonString());

            Assert.Equal(JsonValueKind.Object, message.Message.ValueKind);
            Assert.True(message.Message.GetProperty("Payload").GetProperty("Severity").GetProperty("Value").GetInt32() >= 0);

            Assert.NotNull(metadata);
        }

        /// <summary>
        /// Compares the wire shape the native runtime produces against the shape
        /// the custom encoder produces for the same configuration. Structure is
        /// compared rather than values, because timestamps, sequence numbers and
        /// identifiers legitimately differ between two runs, while a missing
        /// envelope member or a value written as a bare number instead of a
        /// DataValue envelope is a real break for every consumer.
        /// </summary>
        /// <remarks>
        /// Skipped: the native writer group builds its JSON messages without the
        /// configured content masks, so every messaging mode currently produces
        /// the same shape. Closing that is stack work rather than Publisher work,
        /// and this theory is the gate that proves it is closed. See the 8d gap
        /// list in the migration plan.
        /// </remarks>
        /// <param name="messagingMode"></param>
        [Theory(Skip = "Native writer group ignores the JSON content masks; see 8d gap list.")]
        [InlineData("PubSub")]
        [InlineData("FullNetworkMessages")]
        [InlineData("DataSetMessages")]
        [InlineData("SingleDataSetMessage")]
        [InlineData("RawDataSets")]
        public async Task NativePubSubMatchesTheCustomEncoderWireShapeAsync(string messagingMode)
        {
            var custom = await CaptureShapeAsync(messagingMode, native: false);
            var native = await CaptureShapeAsync(messagingMode, native: true);

            _output.WriteLine("custom: " + custom);
            _output.WriteLine("native: " + native);
            Assert.Equal(custom, native);
        }

        private async Task<string> CaptureShapeAsync(string messagingMode, bool native)
        {
            string[] arguments = native
                ? ["--mm=" + messagingMode, "--dm=False", "--ps=False", "--unp=True"]
                : ["--mm=" + messagingMode, "--dm=False", "--ps=False"];
            var (_, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(NativePubSubMatchesTheCustomEncoderWireShapeAsync) + messagingMode + native,
                "./Resources/DataItems.json", TimeSpan.FromMinutes(2), 20,
                arguments: arguments, version: MqttVersion.v5);

            //
            // The runtime may emit an initial key frame before any value has been
            // observed, so the first message carrying the field is the one that
            // describes the wire shape.
            //
            var carrying = messages
                .Select(message => message.Message)
                .FirstOrDefault(message => FindOutput(message).ValueKind != JsonValueKind.Undefined);
            Assert.NotEqual(JsonValueKind.Undefined, carrying.ValueKind);
            return Shape(carrying);
        }

        private static JsonElement FindOutput(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("Output", out var output))
                {
                    return output;
                }
                foreach (var property in element.EnumerateObject())
                {
                    var found = FindOutput(property.Value);
                    if (found.ValueKind != JsonValueKind.Undefined)
                    {
                        return found;
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var found = FindOutput(item);
                    if (found.ValueKind != JsonValueKind.Undefined)
                    {
                        return found;
                    }
                }
            }
            return default;
        }

        private static string Shape(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    return "{" + string.Join(",", element.EnumerateObject()
                        .OrderBy(property => property.Name, StringComparer.Ordinal)
                        .Select(property => property.Name + ":" + Shape(property.Value))) + "}";
                case JsonValueKind.Array:
                    //
                    // Message counts differ between runs, so only the shape of the
                    // first element is compared.
                    //
                    return element.GetArrayLength() == 0
                        ? "[]" : "[" + Shape(element[0]) + "]";
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return "Boolean";
                default:
                    return element.ValueKind.ToString();
            }
        }
    }
}
