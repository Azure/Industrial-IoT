// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Currently, we create new independent instances of server, publisher and mocked IoT services for each test,
    /// this could be optimised e.g. create only single instance of server and publisher between tests in the same class.
    /// </summary>
    [Collection(ReadCollection.Name)]
    public class MqttPubSubIntegrationTests : PublisherMqttIntegrationTestBase {
        private readonly ITestOutputHelper _output;

        public MqttPubSubIntegrationTests(ReferenceServerFixture fixture, ITestOutputHelper output) : base(fixture) {
            _output = output;
        }

        [Fact]
        public async Task CanSendDataItemToIoTHubTest() {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(@"./PublishedNodes/DataItems.json",
                false, messageType: "ua-data", arguments: new string[] { "--mm=PubSub", "--mqn=metadatamessage" }).ConfigureAwait(false);

            // Assert
            var message = Assert.Single(messages);
            var output = message.Message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

            Assert.NotNull(metadata);
            Assert.EndsWith("/metadatamessage", metadata.Value.Topic);
        }

        [Fact]
        public async Task CanSendDataItemButNotMetaDataWhenMetaDataIsDisabledTest() {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(@"./PublishedNodes/DataItems.json", true,
                arguments: new string[] { "--dm", "--mm=DataSetMessages" }).ConfigureAwait(false);

            // Assert
            var message = Assert.Single(messages);
            var output = message.Message.GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

            Assert.Null(metadata);
        }

        [Fact]
        public async Task CanSendDataItemAsDataSetMessagesToIoTHubWithCompliantEncodingTest() {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(@"./PublishedNodes/DataItems.json", false,
                messageType: "ua-deltaframe", arguments: new string[] { "-c", "--mm=DataSetMessages" }).ConfigureAwait(false);

            // Assert
            var message = Assert.Single(messages);
            var output = message.Message.GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

            Assert.NotNull(metadata);
        }

        [Fact]
        public async Task CanSendDataItemAsRawDataSetsToIoTHubWithCompliantEncodingTest() {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(@"./PublishedNodes/DataItems.json", true,
                messageType: "ua-deltaframe", arguments: new string[] { "-c", "--dm=False", "--mm=RawDataSets", "--mqn=$metadata" }).ConfigureAwait(false);

            // Assert
            var output = Assert.Single(messages);
            Assert.NotEqual(JsonValueKind.Null, output.Message.ValueKind);
            Assert.InRange(output.Message.GetProperty("Output").GetDouble(), double.MinValue, double.MaxValue);

            // Explicitely enabled metadata despite messaging profile
            Assert.NotNull(metadata);
            Assert.EndsWith("/$metadata", metadata.Value.Topic);
        }

        [Theory]
        [InlineData(@"./PublishedNodes/SimpleEvents.json")]
        public async Task CanEncodeWithoutReversibleEncodingTest(string publishedNodesFile) {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                publishedNodesFile, true, messageType: "ua-data",
                arguments: new[] { "--mm=PubSub", "--me=Json" }
            ).ConfigureAwait(false);

            Assert.Single(result);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m => {
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

            BasicPubSubIntegrationTests.AssertSimpleEventsMetadata(metadata.Value.Message);
        }

        [Theory]
        [InlineData(@"./PublishedNodes/SimpleEvents.json")]
        public async Task CanEncodeWithReversibleEncodingTest(string publishedNodesFile) {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                publishedNodesFile, TimeSpan.FromMinutes(2), 4, false, messageType: "ua-data",
                arguments: new[] { "--mm=PubSub", "--me=JsonReversible" }
            ).ConfigureAwait(false);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m => {
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

            BasicPubSubIntegrationTests.AssertSimpleEventsMetadata(metadata.Value.Message);
        }

        [Theory]
        [InlineData(@"./PublishedNodes/SimpleEvents.json")]
        public async Task CanEncodeEventWithCompliantEncodingTestTest(string publishedNodesFile) {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                publishedNodesFile, true, messageType: "ua-data",
                arguments: new[] { "-c", "--mm=PubSub", "--me=Json" }
            ).ConfigureAwait(false);

            Assert.Single(result);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m => {
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

            BasicPubSubIntegrationTests.AssertCompliantSimpleEventsMetadata(metadata.Value.Message);
        }

        [Theory]
        [InlineData(@"./PublishedNodes/SimpleEvents.json")]
        public async Task CanEncodeWithReversibleEncodingAndWithCompliantEncodingTestTest(string publishedNodesFile) {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                publishedNodesFile, TimeSpan.FromMinutes(2), 4, false, messageType: "ua-data",
                arguments: new[] { "-c", "--mm=PubSub", "--me=JsonReversible" }
            ).ConfigureAwait(false);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m => {
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
                Assert.Equal(2, body.GetProperty("Encoding").GetInt32());
                Assert.Equal(JsonValueKind.String, body.GetProperty("Body").GetProperty("CycleStepDataType").GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.String, body.GetProperty("Body").GetProperty("CycleStepDataType").GetProperty("Duration").ValueKind);
            });

            BasicPubSubIntegrationTests.AssertCompliantSimpleEventsMetadata(metadata.Value.Message);
        }

        [Fact]
        public async Task CanSendPendingConditionsToIoTHubTest() {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(@"./PublishedNodes/PendingAlarms.json",
                false, BasicPubSubIntegrationTests.GetAlarmCondition,
                messageType: "ua-data", arguments: new string[] { "--mm=PubSub" }).ConfigureAwait(false);

            // Assert
            _output.WriteLine(messages.ToString());
            var evt = Assert.Single(messages);

            Assert.Equal(JsonValueKind.Object, evt.Message.ValueKind);
            Assert.True(evt.Message.GetProperty("Payload").GetProperty("Severity").GetProperty("Value").GetInt32() >= 100);

            Assert.NotNull(metadata);
        }
    }
}