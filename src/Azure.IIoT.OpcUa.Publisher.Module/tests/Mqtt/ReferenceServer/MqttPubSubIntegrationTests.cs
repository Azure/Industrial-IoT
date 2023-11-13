// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.ReferenceServer
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.ReferenceServer;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Furly.Extensions.Mqtt;
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class MqttPubSubIntegrationTests : PublisherIntegrationTestBase
    {
        private readonly ReferenceServer _fixture;
        private readonly ITestOutputHelper _output;

        public MqttPubSubIntegrationTests(ITestOutputHelper output) : base(output)
        {
            _output = output;
            _fixture = new ReferenceServer();
            EndpointUrl = _fixture.EndpointUrl;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _fixture.Dispose();
            }
        }

        [Fact]
        public async Task CanSendDataItemToMqttBrokerTest()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemToMqttBrokerTest), "./Resources/DataItems.json",
                messageType: "ua-data", arguments: new string[] { "--mm=PubSub", "--mdt={TelemetryTopic}/metadatamessage", "--dm=False" },
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
        public async Task CanSendDataItemButNotMetaDataWhenMetaDataIsDisabledTest()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemButNotMetaDataWhenMetaDataIsDisabledTest), "./Resources/DataItems.json",
                arguments: new string[] { "--dm", "--mm=DataSetMessages" },
                version: MqttVersion.v5);

            // Assert
            var message = Assert.Single(messages);
            var output = message.Message.GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

            Assert.Null(metadata);
        }

        [Fact]
        public async Task CanSendDataItemAsDataSetMessagesToMqttBrokerWithCompliantEncodingTest()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemAsDataSetMessagesToMqttBrokerWithCompliantEncodingTest),
                "./Resources/DataItems.json", messageType: "ua-deltaframe",
                arguments: new string[] { "-c", "--mm=DataSetMessages" },
                version: MqttVersion.v311);

            // Assert
            var message = Assert.Single(messages);
            var output = message.Message.GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

            Assert.NotNull(metadata);
        }

        [Fact]
        public async Task CanSendDataItemAsRawDataSetsToMqttBrokerWithCompliantEncodingTest()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemAsRawDataSetsToMqttBrokerWithCompliantEncodingTest),
                "./Resources/DataItems.json", messageType: "ua-deltaframe",
                arguments: new string[] { "-c", "--dm=False", "--mm=RawDataSets", "--mdt" },
                version: MqttVersion.v5);

            // Assert
            var output = Assert.Single(messages);
            Assert.NotEqual(JsonValueKind.Null, output.Message.ValueKind);
            Assert.InRange(output.Message.GetProperty("Output").GetDouble(),
                double.MinValue, double.MaxValue);

            // Explicitely enabled metadata despite messaging profile
            Assert.NotNull(metadata);
            Assert.EndsWith("/$metadata", metadata.Value.Topic, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("./Resources/SimpleEvents.json")]
        public async Task CanEncodeWithoutReversibleEncodingTest(string publishedNodesFile)
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(nameof(CanEncodeWithoutReversibleEncodingTest),
                publishedNodesFile, messageType: "ua-data", arguments: new[] { "--mm=PubSub", "--me=Json", "--dm=false" },
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
                var eventId = value.GetProperty(BasicPubSubIntegrationTests.kEventId).GetProperty("Value");
                var message = value.GetProperty(BasicPubSubIntegrationTests.kMessage).GetProperty("Value");
                var cycleId = value.GetProperty(BasicPubSubIntegrationTests.kCycleId).GetProperty("Value");
                var currentStep = value.GetProperty(BasicPubSubIntegrationTests.kCurrentStep).GetProperty("Value");

                Assert.Equal(JsonValueKind.String, eventId.ValueKind);
                Assert.Equal(JsonValueKind.String, message.ValueKind);
                Assert.Equal(JsonValueKind.String, cycleId.ValueKind);
                Assert.Equal(JsonValueKind.String, currentStep.GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, currentStep.GetProperty("Duration").ValueKind);
            });

            Assert.NotNull(metadata);
            BasicPubSubIntegrationTests.AssertSimpleEventsMetadata(metadata.Value);
        }

        [Theory]
        [InlineData("./Resources/SimpleEvents.json")]
        public async Task CanEncodeWithReversibleEncodingTest(string publishedNodesFile)
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(nameof(CanEncodeWithReversibleEncodingTest),
                publishedNodesFile, TimeSpan.FromMinutes(2), 4, messageType: "ua-data",
                arguments: new[] { "--mm=PubSub", "--me=JsonReversible", "--dm=False" },
                version: MqttVersion.v311);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                var body = m.GetProperty("Payload");
                var eventId = body.GetProperty(BasicPubSubIntegrationTests.kEventId).GetProperty("Value");
                Assert.Equal("ByteString", eventId.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, eventId.GetProperty("Body").ValueKind);

                var message = body.GetProperty(BasicPubSubIntegrationTests.kMessage).GetProperty("Value");
                Assert.Equal("LocalizedText", message.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, message.GetProperty("Body").GetProperty("Text").ValueKind);
                Assert.Equal("en-US", message.GetProperty("Body").GetProperty("Locale").GetString());

                var cycleId = body.GetProperty(BasicPubSubIntegrationTests.kCycleId).GetProperty("Value");
                Assert.Equal("String", cycleId.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, cycleId.GetProperty("Body").ValueKind);

                var currentStep = body.GetProperty(BasicPubSubIntegrationTests.kCurrentStep).GetProperty("Value");
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

        [Theory]
        [InlineData("./Resources/SimpleEvents.json")]
        public async Task CanEncodeEventWithCompliantEncodingTestTest(string publishedNodesFile)
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(nameof(CanEncodeEventWithCompliantEncodingTestTest),
                publishedNodesFile, messageType: "ua-data", arguments: new[] { "-c", "--mm=PubSub", "--me=Json" },
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
                var eventId = value.GetProperty(BasicPubSubIntegrationTests.kEventId).GetProperty("Value");
                var message = value.GetProperty(BasicPubSubIntegrationTests.kMessage).GetProperty("Value");
                var cycleId = value.GetProperty(BasicPubSubIntegrationTests.kCycleId).GetProperty("Value");
                var currentStep = value.GetProperty(BasicPubSubIntegrationTests.kCurrentStep).GetProperty("Value");

                Assert.Equal(JsonValueKind.String, eventId.ValueKind);
                Assert.Equal(JsonValueKind.String, message.ValueKind);
                Assert.Equal(JsonValueKind.String, cycleId.ValueKind);
                Assert.Equal(JsonValueKind.String, currentStep.GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, currentStep.GetProperty("Duration").ValueKind);
            });

            Assert.NotNull(metadata);
            BasicPubSubIntegrationTests.AssertCompliantSimpleEventsMetadata(metadata.Value);
        }

        [Theory]
        [InlineData("./Resources/SimpleEvents.json")]
        public async Task CanEncodeWithReversibleEncodingAndWithCompliantEncodingTestTest(string publishedNodesFile)
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(nameof(CanEncodeWithReversibleEncodingAndWithCompliantEncodingTestTest),
                publishedNodesFile, TimeSpan.FromMinutes(2), 4, messageType: "ua-data",
                arguments: new[] { "-c", "--mm=PubSub", "--me=JsonReversible" },
                version: MqttVersion.v311);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                var body = m.GetProperty("Payload");
                var eventId = body.GetProperty(BasicPubSubIntegrationTests.kEventId).GetProperty("Value");
                Assert.Equal(15, eventId.GetProperty("Type").GetInt32());
                Assert.Equal(JsonValueKind.String, eventId.GetProperty("Body").ValueKind);

                var message = body.GetProperty(BasicPubSubIntegrationTests.kMessage).GetProperty("Value");
                Assert.Equal(21, message.GetProperty("Type").GetInt32());
                Assert.Equal(JsonValueKind.String, message.GetProperty("Body").GetProperty("Text").ValueKind);
                Assert.Equal("en-US", message.GetProperty("Body").GetProperty("Locale").GetString());

                var cycleId = body.GetProperty(BasicPubSubIntegrationTests.kCycleId).GetProperty("Value");
                Assert.Equal(12, cycleId.GetProperty("Type").GetInt32());
                Assert.Equal(JsonValueKind.String, cycleId.GetProperty("Body").ValueKind);

                var currentStep = body.GetProperty(BasicPubSubIntegrationTests.kCurrentStep).GetProperty("Value");
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
        public async Task CanSendPendingConditionsToMqttBrokerTest()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(nameof(CanSendPendingConditionsToMqttBrokerTest),
                "./Resources/PendingAlarms.json", BasicPubSubIntegrationTests.GetAlarmCondition, messageType: "ua-data",
                arguments: new string[] { "--mm=PubSub", "--dm=False" }, version: MqttVersion.v311);

            // Assert
            var evt = Assert.Single(messages);
            _output.WriteLine(evt.ToString());

            Assert.Equal(JsonValueKind.Object, evt.Message.ValueKind);
            Assert.True(evt.Message.GetProperty("Payload").GetProperty("Severity").GetProperty("Value").GetInt32() >= 100);

            Assert.NotNull(metadata);
        }
    }
}
