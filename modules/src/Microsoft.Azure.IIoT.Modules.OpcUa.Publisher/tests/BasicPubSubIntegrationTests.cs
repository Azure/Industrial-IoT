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
    public class BasicPubSubIntegrationTests : PublisherIntegrationTestBase {
        private const string kEventId = "EventId";
        private const string kMessage = "Message";
        private const string kCycleId = "http://opcfoundation.org/SimpleEvents#CycleId";
        private const string kCurrentStep = "http://opcfoundation.org/SimpleEvents#CurrentStep";
        private readonly ITestOutputHelper _output;

        public BasicPubSubIntegrationTests(ReferenceServerFixture fixture, ITestOutputHelper output) : base(fixture) {
            _output = output;
        }

        [Fact]
        public async Task CanSendDataItemToIoTHubTest() {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(@"./PublishedNodes/DataItems.json",
                messageType: "ua-data", arguments: new string[] { "--mm=PubSub" }).ConfigureAwait(false);

            // Assert
            var message = Assert.Single(messages);
            var output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
        }

        [Fact]
        public async Task CanSendDataItemAsDataSetMessagesToIoTHubWithCompliantEncodingTest() {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(@"./PublishedNodes/DataItems.json",
                messageType: "ua-deltaframe", arguments: new string[] { "-c", "--mm=DataSetMessages" }).ConfigureAwait(false);

            // Assert
            var message = Assert.Single(messages);
            var output = message.GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
        }

        [Fact]
        public async Task CanSendDataItemAsRawDataSetsToIoTHubWithCompliantEncodingTest() {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(@"./PublishedNodes/DataItems.json",
                messageType: "ua-deltaframe", arguments: new string[] { "-c", "--mm=RawDataSets" }).ConfigureAwait(false);

            // Assert
            var output = Assert.Single(messages);
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Output").GetDouble(), double.MinValue, double.MaxValue);
        }

        [Theory]
        [InlineData(@"./PublishedNodes/SimpleEvents.json")]
        public async Task CanEncodeWithoutReversibleEncodingTest(string publishedNodesFile) {
            // Arrange
            // Act
            var result = await ProcessMessagesAsync(
                publishedNodesFile, messageType: "ua-data",
                arguments: new[] { "--mm=PubSub", "--me=Json" }
            ).ConfigureAwait(false);

            Assert.Single(result);

            var messages = result
                .SelectMany(x => x.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m => {
                var value = m.GetProperty("Payload");

                // Variant encoding is the default
                var eventId = value.GetProperty(kEventId).GetProperty("Value");
                var message = value.GetProperty(kMessage).GetProperty("Value");
                var cycleId = value.GetProperty(kCycleId).GetProperty("Value");
                var currentStep = value.GetProperty(kCurrentStep).GetProperty("Value");

                Assert.Equal(JsonValueKind.String, eventId.ValueKind);
                Assert.Equal(JsonValueKind.String, message.ValueKind);
                Assert.Equal(JsonValueKind.String, cycleId.ValueKind);
                Assert.Equal(JsonValueKind.String, currentStep.GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, currentStep.GetProperty("Duration").ValueKind);
            });
        }

        [Theory]
        [InlineData(@"./PublishedNodes/SimpleEvents.json")]
        public async Task CanEncodeWithReversibleEncodingTest(string publishedNodesFile) {
            // Arrange
            // Act
            var result = await ProcessMessagesAsync(
                publishedNodesFile, TimeSpan.FromMinutes(2), 4, messageType: "ua-data",
                arguments: new[] { "--mm=PubSub", "--me=JsonReversible" }
            ).ConfigureAwait(false);

            var messages = result
                .SelectMany(x => x.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m => {
                var body = m.GetProperty("Payload");
                var eventId = body.GetProperty(kEventId).GetProperty("Value");
                Assert.Equal("ByteString", eventId.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, eventId.GetProperty("Body").ValueKind);

                var message = body.GetProperty(kMessage).GetProperty("Value");
                Assert.Equal("LocalizedText", message.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, message.GetProperty("Body").GetProperty("Text").ValueKind);
                Assert.Equal("en-US", message.GetProperty("Body").GetProperty("Locale").GetString());

                var cycleId = body.GetProperty(kCycleId).GetProperty("Value");
                Assert.Equal("String", cycleId.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, cycleId.GetProperty("Body").ValueKind);

                var currentStep = body.GetProperty(kCurrentStep).GetProperty("Value");
                body = currentStep.GetProperty("Body");
                Assert.Equal("ExtensionObject", currentStep.GetProperty("Type").GetString());
                Assert.Equal("http://opcfoundation.org/SimpleEvents#i=183", body.GetProperty("TypeId").GetString());
                Assert.Equal("Json", body.GetProperty("Encoding").GetString());
                Assert.Equal(JsonValueKind.String, body.GetProperty("Body").GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, body.GetProperty("Body").GetProperty("Duration").ValueKind);
            });
        }

        [Theory]
        [InlineData(@"./PublishedNodes/SimpleEvents.json")]
        public async Task CanEncodeEventWithCompliantEncodingTestTest(string publishedNodesFile) {
            // Arrange
            // Act
            var result = await ProcessMessagesAsync(
                publishedNodesFile, messageType: "ua-data",
                arguments: new[] { "-c", "--mm=PubSub", "--me=Json" }
            ).ConfigureAwait(false);

            Assert.Single(result);

            var messages = result
                .SelectMany(x => x.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m => {
                var value = m.GetProperty("Payload");

                // Variant encoding is the default
                var eventId = value.GetProperty(kEventId).GetProperty("Value");
                var message = value.GetProperty(kMessage).GetProperty("Value");
                var cycleId = value.GetProperty(kCycleId).GetProperty("Value");
                var currentStep = value.GetProperty(kCurrentStep).GetProperty("Value");

                Assert.Equal(JsonValueKind.String, eventId.ValueKind);
                Assert.Equal(JsonValueKind.String, message.ValueKind);
                Assert.Equal(JsonValueKind.String, cycleId.ValueKind);
                Assert.Equal(JsonValueKind.String, currentStep.GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, currentStep.GetProperty("Duration").ValueKind);
            });
        }

        [Theory]
        [InlineData(@"./PublishedNodes/SimpleEvents.json")]
        public async Task CanEncodeWithReversibleEncodingAndWithCompliantEncodingTestTest(string publishedNodesFile) {
            // Arrange
            // Act
            var result = await ProcessMessagesAsync(
                publishedNodesFile, TimeSpan.FromMinutes(2), 4, messageType: "ua-data",
                arguments: new[] { "-c", "--mm=PubSub", "--me=JsonReversible" }
            ).ConfigureAwait(false);

            var messages = result
                .SelectMany(x => x.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m => {
                var body = m.GetProperty("Payload");
                var eventId = body.GetProperty(kEventId).GetProperty("Value");
                Assert.Equal(15, eventId.GetProperty("Type").GetInt32());
                Assert.Equal(JsonValueKind.String, eventId.GetProperty("Body").ValueKind);

                var message = body.GetProperty(kMessage).GetProperty("Value");
                Assert.Equal(21, message.GetProperty("Type").GetInt32());
                Assert.Equal(JsonValueKind.String, message.GetProperty("Body").GetProperty("Text").ValueKind);
                Assert.Equal("en-US", message.GetProperty("Body").GetProperty("Locale").GetString());

                var cycleId = body.GetProperty(kCycleId).GetProperty("Value");
                Assert.Equal(12, cycleId.GetProperty("Type").GetInt32());
                Assert.Equal(JsonValueKind.String, cycleId.GetProperty("Body").ValueKind);

                var currentStep = body.GetProperty(kCurrentStep).GetProperty("Value");
                body = currentStep.GetProperty("Body");
                Assert.Equal(22, currentStep.GetProperty("Type").GetInt32());
                Assert.Equal(183, body.GetProperty("TypeId").GetProperty("Id").GetInt32());
                Assert.Equal(2, body.GetProperty("Encoding").GetInt32());
                Assert.Equal(JsonValueKind.String, body.GetProperty("Body").GetProperty("CycleStepDataType").GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.String, body.GetProperty("Body").GetProperty("CycleStepDataType").GetProperty("Duration").ValueKind);
            });
        }

        [Fact]
        public async Task CanSendPendingConditionsToIoTHubTest() {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(@"./PublishedNodes/PendingAlarms.json", GetAlarmCondition,
                messageType: "ua-data", arguments: new string[] { "--mm=PubSub" }).ConfigureAwait(false);

            // Assert
            _output.WriteLine(messages.ToString());
            var evt = Assert.Single(messages);

            Assert.Equal(JsonValueKind.Object, evt.ValueKind);
            Assert.True(evt.GetProperty("Payload").GetProperty("Severity").GetProperty("Value").GetInt32() >= 100);
        }

        private static JsonElement GetAlarmCondition(JsonElement jsonElement) {
            var messages = jsonElement.GetProperty("Messages");
            if (messages.ValueKind != JsonValueKind.Array) {
                return default;
            }
            return messages.EnumerateArray().FirstOrDefault(element => {
                return element.GetProperty("MessageType").GetString() == "ua-condition" &&
                    element.GetProperty("Payload").TryGetProperty("SourceNode", out var node) &&
                        node.TryGetProperty("Value", out node) &&
                            node.ValueKind != JsonValueKind.Null &&
                            node.GetString().StartsWith("http://opcfoundation.org/AlarmCondition#s=1%3a");
            });
        }
    }
}