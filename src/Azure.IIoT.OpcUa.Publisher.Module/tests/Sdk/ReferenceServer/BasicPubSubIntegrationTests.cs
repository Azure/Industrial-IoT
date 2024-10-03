// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.ReferenceServer
{
    using Autofac.Features.Indexed;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using FluentAssertions;
    using Google.Protobuf.WellKnownTypes;
    using Json.More;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class BasicPubSubIntegrationTests : PublisherIntegrationTestBase
    {
        internal const string kEventId = "EventId";
        internal const string kMessage = "Message";
        internal const string kCycleIdExpanded = "nsu=http://opcfoundation.org/SimpleEvents;CycleId";
        internal const string kCurrentStepExpanded = "nsu=http://opcfoundation.org/SimpleEvents;CurrentStep";
        internal const string kCycleIdUri = "http://opcfoundation.org/SimpleEvents#CycleId";
        internal const string kCurrentStepUri = "http://opcfoundation.org/SimpleEvents#CurrentStep";
        private readonly ITestOutputHelper _output;
        private readonly ReferenceServer _fixture;

        public BasicPubSubIntegrationTests(ITestOutputHelper output)
            : base(output)
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
        public async Task CanSendDataItemToIoTHubTest()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemToIoTHubTest), "./Resources/DataItems.json",
                messageType: "ua-data", arguments: new string[] { "--mm=PubSub", "--dm=false" });

            // Assert
            var message = Assert.Single(messages).Message;
            var output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

            Assert.NotNull(metadata);
        }

        [Fact]
        public async Task CanSendModelChangeEventsToIoTHubTest()
        {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(nameof(CanSendModelChangeEventsToIoTHubTest), "./Resources/ModelChanges.json",
                TimeSpan.FromMinutes(2), 5, messageType: "ua-data", arguments: new[] { "--mm=PubSub", "--dm=false" });

            // Assert
            Assert.NotEmpty(messages);
            var payload1 = messages[0].Message.GetProperty("Messages")[0].GetProperty("Payload");
            _output.WriteLine(payload1.ToJsonString());
            Assert.NotEqual(JsonValueKind.Null, payload1.ValueKind);
            Assert.True(Guid.TryParse(payload1.GetProperty("EventId").GetProperty("Value").GetString(), out _));
            Assert.Equal("http://www.microsoft.com/opc-publisher#s=ReferenceChange",
                payload1.GetProperty("EventType").GetProperty("Value").GetString());
            Assert.Equal("i=84", payload1.GetProperty("SourceNode").GetProperty("Value").GetString());
            Assert.True(DateTime.TryParse(payload1.GetProperty("Time").GetProperty("Value").GetString(), out _));
            Assert.True(payload1.GetProperty("Change").GetProperty("Value").GetProperty("IsForward").GetBoolean());
            Assert.Equal("Objects", payload1.GetProperty("Change").GetProperty("Value").GetProperty("DisplayName").GetString());

            var payload2 = messages[1].Message.GetProperty("Messages")[0].GetProperty("Payload");
            _output.WriteLine(payload2.ToJsonString());
            Assert.NotEqual(JsonValueKind.Null, payload1.ValueKind);
            Assert.True(Guid.TryParse(payload2.GetProperty("EventId").GetProperty("Value").GetString(), out _));
            Assert.Equal("http://www.microsoft.com/opc-publisher#s=NodeChange",
                payload2.GetProperty("EventType").GetProperty("Value").GetString());
            Assert.Equal("i=85", payload2.GetProperty("SourceNode").GetProperty("Value").GetString());
            Assert.True(DateTime.TryParse(payload2.GetProperty("Time").GetProperty("Value").GetString(), out _));
            Assert.Equal("Objects", payload2.GetProperty("Change").GetProperty("Value").GetProperty("DisplayName").GetString());

            // TODO: currently metadata is sent later
            // Assert.NotNull(metadata);
        }

        [Fact]
        public async Task CanSendDataItemButNotMetaDataWhenMetaDataIsDisabledTest()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemButNotMetaDataWhenMetaDataIsDisabledTest),
                "./Resources/DataItems.json",
                arguments: new string[] { "-c", "--dm", "--mm=DataSetMessages" });

            // Assert
            var message = Assert.Single(messages).Message;
            var output = message.GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

            Assert.Null(metadata);
        }

        [Fact]
        public async Task CanSendDataItemButNotMetaDataWhenComplexTypeSystemIsDisabledTest()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemButNotMetaDataWhenMetaDataIsDisabledTest),
                "./Resources/DataItems.json",
                arguments: new string[] { "-c", "--dct", "--mm=DataSetMessages" });

            // Assert
            var message = Assert.Single(messages).Message;
            var output = message.GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

            Assert.Null(metadata);
        }

        [Fact]
        public async Task CanSendDataItemAsDataSetMessagesToIoTHubWithCompliantEncodingTest()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemAsDataSetMessagesToIoTHubWithCompliantEncodingTest), "./Resources/DataItems.json",
                messageType: "ua-deltaframe", arguments: new string[] { "-c", "--mm=DataSetMessages" });

            // Assert
            var message = Assert.Single(messages).Message;
            var output = message.GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

            Assert.NotNull(metadata);
        }

        [Fact]
        public async Task CanSendDataItemAsRawDataSetsToIoTHubWithCompliantEncodingTest()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemAsRawDataSetsToIoTHubWithCompliantEncodingTest), "./Resources/DataItems.json",
                messageType: "ua-deltaframe", arguments: new string[] { "-c", "--dm=False", "--mm=RawDataSets" });

            // Assert
            var output = Assert.Single(messages).Message;
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Output").GetDouble(), double.MinValue, double.MaxValue);

            // Explicitely enabled metadata despite messaging profile
            Assert.NotNull(metadata);
        }

        [Fact]
        public async Task CanEncodeWithoutReversibleEncodingTest()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                nameof(CanEncodeWithoutReversibleEncodingTest),
                "./Resources/SimpleEvents.json", messageType: "ua-data",
                arguments: new[] { "--mm=PubSub", "--me=Json", "--dm=false" }
            );

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
                var eventId = value.GetProperty(kEventId).GetProperty("Value");
                var message = value.GetProperty(kMessage).GetProperty("Value");
                var cycleId = value.GetProperty(kCycleIdUri).GetProperty("Value");
                var currentStep = value.GetProperty(kCurrentStepUri).GetProperty("Value");

                Assert.Equal(JsonValueKind.String, eventId.ValueKind);
                Assert.Equal(JsonValueKind.String, message.ValueKind);
                Assert.Equal(JsonValueKind.String, cycleId.ValueKind);
                Assert.Equal(JsonValueKind.String, currentStep.GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, currentStep.GetProperty("Duration").ValueKind);
            });

            AssertSimpleEventsMetadata(metadata);
        }

        [Fact]
        public async Task CanEncodeWithReversibleEncodingTest()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                nameof(CanEncodeWithReversibleEncodingTest),
                "./Resources/SimpleEvents.json", TimeSpan.FromMinutes(2), 4, messageType: "ua-data",
                arguments: new[] { "--mm=PubSub", "--me=JsonReversible", "--dm=false" }
            );

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                var body = m.GetProperty("Payload");
                var eventId = body.GetProperty(kEventId).GetProperty("Value");
                Assert.Equal("ByteString", eventId.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, eventId.GetProperty("Body").ValueKind);

                var message = body.GetProperty(kMessage).GetProperty("Value");
                Assert.Equal("LocalizedText", message.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, message.GetProperty("Body").GetProperty("Text").ValueKind);
                Assert.Equal("en-US", message.GetProperty("Body").GetProperty("Locale").GetString());

                var cycleId = body.GetProperty(kCycleIdUri).GetProperty("Value");
                Assert.Equal("String", cycleId.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, cycleId.GetProperty("Body").ValueKind);

                var currentStep = body.GetProperty(kCurrentStepUri).GetProperty("Value");
                body = currentStep.GetProperty("Body");
                Assert.Equal("ExtensionObject", currentStep.GetProperty("Type").GetString());
                Assert.Equal("http://opcfoundation.org/SimpleEvents#i=183", body.GetProperty("TypeId").GetString());
                Assert.Equal("Json", body.GetProperty("Encoding").GetString());
                Assert.Equal(JsonValueKind.String, body.GetProperty("Body").GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, body.GetProperty("Body").GetProperty("Duration").ValueKind);
            });

            AssertSimpleEventsMetadata(metadata);
        }

        [Fact]
        public async Task CanEncodeEventWithCompliantEncodingTestTest()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                nameof(CanEncodeEventWithCompliantEncodingTestTest),
                "./Resources/SimpleEvents.json", messageType: "ua-data",
                arguments: new[] { "-c", "--mm=PubSub", "--me=Json" });

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
                var eventId = value.GetProperty(kEventId).GetProperty("Value");
                var message = value.GetProperty(kMessage).GetProperty("Value");
                var cycleId = value.GetProperty(kCycleIdExpanded).GetProperty("Value");
                var currentStep = value.GetProperty(kCurrentStepExpanded).GetProperty("Value");

                Assert.Equal(JsonValueKind.String, eventId.ValueKind);
                Assert.Equal(JsonValueKind.String, message.ValueKind);
                Assert.Equal(JsonValueKind.String, cycleId.ValueKind);
                Assert.Equal(JsonValueKind.String, currentStep.GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, currentStep.GetProperty("Duration").ValueKind);
            });

            AssertCompliantSimpleEventsMetadata(metadata);
        }

        [Fact]
        public async Task CanEncodeWithReversibleEncodingAndWithCompliantEncodingTestTest()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                nameof(CanEncodeWithReversibleEncodingAndWithCompliantEncodingTestTest),
                "./Resources/SimpleEvents.json", TimeSpan.FromMinutes(2), 4, messageType: "ua-data",
                arguments: new[] { "-c", "--mm=PubSub", "--me=JsonReversible" });

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                var body = m.GetProperty("Payload");
                var eventId = body.GetProperty(kEventId).GetProperty("Value");
                Assert.Equal(15, eventId.GetProperty("Type").GetInt32());
                Assert.Equal(JsonValueKind.String, eventId.GetProperty("Body").ValueKind);

                var message = body.GetProperty(kMessage).GetProperty("Value");
                Assert.Equal(21, message.GetProperty("Type").GetInt32());
                Assert.Equal(JsonValueKind.String, message.GetProperty("Body").GetProperty("Text").ValueKind);
                Assert.Equal("en-US", message.GetProperty("Body").GetProperty("Locale").GetString());

                var cycleId = body.GetProperty(kCycleIdExpanded).GetProperty("Value");
                Assert.Equal(12, cycleId.GetProperty("Type").GetInt32());
                Assert.Equal(JsonValueKind.String, cycleId.GetProperty("Body").ValueKind);

                var currentStep = body.GetProperty(kCurrentStepExpanded).GetProperty("Value");
                body = currentStep.GetProperty("Body");
                Assert.Equal(22, currentStep.GetProperty("Type").GetInt32());
                Assert.Equal(183, body.GetProperty("TypeId").GetProperty("Id").GetInt32());
                Assert.Equal(JsonValueKind.String, body.GetProperty("Body").GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, body.GetProperty("Body").GetProperty("Duration").ValueKind);
            });

            AssertCompliantSimpleEventsMetadata(metadata);
        }

        [Fact]
        public async Task CanEncode2EventsWithCompliantEncodingTest()
        {
            var dataSetWriterNames = new HashSet<string>();

            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                nameof(CanEncode2EventsWithCompliantEncodingTest),
                "./Resources/SimpleEvents2.json", GetBothEvents, messageType: "ua-data",
                arguments: new[] { "-c", "--mm=PubSub", "--me=Json" });

            Assert.Single(result);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            dataSetWriterNames.Select(d => d.Split('|')[1])
                .Should().Contain(new[] { "CycleStarted", "Alarm" });

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                var value = m.GetProperty("Payload");

                // Variant encoding is the default
                var eventId = value.GetProperty(kEventId).GetProperty("Value");
                var message = value.GetProperty(kMessage).GetProperty("Value");
                var cycleId = value.GetProperty(kCycleIdExpanded).GetProperty("Value");
                var currentStep = value.GetProperty(kCurrentStepExpanded).GetProperty("Value");

                Assert.Equal(JsonValueKind.String, eventId.ValueKind);
                Assert.Equal(JsonValueKind.String, message.ValueKind);
                Assert.Equal(JsonValueKind.String, cycleId.ValueKind);
                Assert.Equal(JsonValueKind.String, currentStep.GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, currentStep.GetProperty("Duration").ValueKind);
            });

            JsonElement GetBothEvents(JsonElement jsonElement)
            {
                var messages = jsonElement.GetProperty("Messages");
                if (messages.ValueKind != JsonValueKind.Array)
                {
                    return default;
                }
                foreach (var element in messages.EnumerateArray())
                {
                    var dataSetWriterName = element.GetProperty("DataSetWriterName").GetString();
                    if (dataSetWriterName != null)
                    {
                        dataSetWriterNames.Add(dataSetWriterName);
                    }
                }
                return dataSetWriterNames.Count == 2 ? jsonElement : default;
            }
        }

        [Fact]
        public async Task CanSendPendingConditionsToIoTHubTest()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendPendingConditionsToIoTHubTest), "./Resources/PendingAlarms.json", GetAlarmCondition,
                messageType: "ua-data", arguments: new string[] { "--mm=PubSub", "--dm=False" });

            // Assert
            Assert.NotEmpty(messages);
            var message = Assert.Single(messages).Message;
            _output.WriteLine(message.ToJsonString());

            Assert.Equal(JsonValueKind.Object, message.ValueKind);
            Assert.True(message.GetProperty("Payload").GetProperty("Severity").GetProperty("Value").GetInt32() >= 100);

            Assert.NotNull(metadata);
        }

        [Fact]
        public async Task CanSendKeyFramesWithExtensionFieldsToIoTHubTest()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemToIoTHubTest), "./Resources/KeyFrames.json",
                messageType: "ua-data", arguments: new string[] { "--mm=FullNetworkMessages", "--dm=false" });

            // Assert
            var message = Assert.Single(messages).Message;
            var firstDataSet = message.GetProperty("Messages")[0];
            Assert.Equal("ua-keyframe", firstDataSet.GetProperty("MessageType").GetString());
            var payload = firstDataSet.GetProperty("Payload");
            Assert.NotEqual(JsonValueKind.Null, payload.ValueKind);

            var time = payload.GetProperty("CurrentTime");
            Assert.NotEqual(JsonValueKind.Null, time.ValueKind);
            Assert.True(time.GetProperty("Value").GetDateTime() < DateTime.UtcNow);
            Assert.False(payload.GetProperty("Important").GetProperty("Value").GetBoolean());
            Assert.Equal(5, payload.GetProperty("AssetId").GetProperty("Value").GetInt16());
            Assert.Equal("mm/sec", payload.GetProperty("EngineeringUnits").GetProperty("Value").GetString());
            Assert.Equal(12.3465, payload.GetProperty("Variance").GetProperty("Value").GetDouble(), 6);

            Assert.NotNull(metadata);
            var metadataFields = metadata.Value.Message.GetProperty("MetaData").GetProperty("Fields");
            Assert.Equal(JsonValueKind.Array, metadataFields.ValueKind);
            var fieldNames = metadataFields.EnumerateArray().Select(v => v.GetProperty("Name").GetString()).ToHashSet();

            var expectedNames = new[] { "AssetId", "CurrentTime", "EngineeringUnits", "Important", "Variance" };
            Assert.Equal(expectedNames.Length, fieldNames.Count);
            Assert.All(expectedNames, n => fieldNames.Contains(n));
        }

        [Fact]
        public async Task CanSendFullAndCompliantNetworkMessageWithEndpointUrlAndApplicationUriToIoTHubTest()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemToIoTHubTest), "./Resources/DataItems.json", messageType: "ua-data",
                arguments: new string[] { "--mm=PubSub", "--fm=true", "--strict" });

            // Assert
            var message = Assert.Single(messages).Message;
            var firstDataSet = message.GetProperty("Messages")[0];
            var payload = firstDataSet.GetProperty("Payload");
            Assert.NotEqual(JsonValueKind.Null, payload.ValueKind);
            var output = payload.GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
            var ep = payload.GetProperty("EndpointUrl").GetProperty("Value").GetString();
            Assert.False(string.IsNullOrEmpty(ep));
            var appuri = payload.GetProperty("ApplicationUri").GetProperty("Value").GetString();
            Assert.False(string.IsNullOrEmpty(appuri));

            Assert.NotNull(metadata);
            var fields = metadata.Value.Message.GetProperty("MetaData").GetProperty("Fields");
            Assert.Equal(JsonValueKind.Array, fields.ValueKind);
            var fieldNames = fields.EnumerateArray().Select(v => v.GetProperty("Name").GetString()).ToList();
            Assert.Equal(3, fieldNames.Count);
            Assert.Equal("Output", fieldNames[0]);
            Assert.Equal("EndpointUrl", fieldNames[1]);
            Assert.Equal("ApplicationUri", fieldNames[2]);
        }

        [Fact]
        public async Task CanSendKeyFramesWithExtensionFieldsToIoTHubTestJsonReversible()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendKeyFramesWithExtensionFieldsToIoTHubTestJsonReversible), "./Resources/KeyFrames.json", messageType: "ua-data",
            // NOTE: while we --fm and fullnetworkmessage, the keyframes.json overrides this back to PubSub
                arguments: new string[] { "--mm=FullNetworkMessages", "--me=JsonReversible", "--fm=true", "--strict" });

            // Assert
            var message = Assert.Single(messages).Message;
            var firstDataSet = message.GetProperty("Messages")[0];
            Assert.Equal("ua-keyframe", firstDataSet.GetProperty("MessageType").GetString());
            var payload = firstDataSet.GetProperty("Payload");
            Assert.NotEqual(JsonValueKind.Null, payload.ValueKind);

            var time = payload.GetProperty("CurrentTime").GetProperty("Value");
            Assert.NotEqual(JsonValueKind.Null, time.ValueKind);
            Assert.True(time.GetProperty("Body").GetDateTime() < DateTime.UtcNow);

            var ep = payload.TryGetProperty("EndpointUrl", out _);
            Assert.False(ep);
            var appuri = payload.TryGetProperty("ApplicationUri", out _);
            Assert.False(appuri);

            Assert.False(payload.GetProperty("Important").GetProperty("Value").GetProperty("Body").GetBoolean());
            Assert.Equal("5", payload.GetProperty("AssetId").GetProperty("Value").GetProperty("Body").GetString());
            Assert.Equal("mm/sec", payload.GetProperty("EngineeringUnits").GetProperty("Value").GetProperty("Body").GetString());
            Assert.Equal(12.3465, payload.GetProperty("Variance").GetProperty("Value").GetProperty("Body").GetDouble());

            Assert.NotNull(metadata);
            var metadataFields = metadata.Value.Message.GetProperty("MetaData").GetProperty("Fields");
            Assert.Equal(JsonValueKind.Array, metadataFields.ValueKind);
            var fieldNames = metadataFields.EnumerateArray().Select(v => v.GetProperty("Name").GetString()).ToHashSet();
            var expectedNames = new[] { "AssetId", "CurrentTime", "EngineeringUnits", "Important", "Variance" };
            Assert.Equal(expectedNames.Length, fieldNames.Count);
            Assert.All(expectedNames, n => fieldNames.Contains(n));
            // TODO: Need to have order in fields!  Assert.Equal(metadataFields.EnumerateArray().Select(v => v.GetProperty("Name").GetString()),
            // TODO: Need to have order in fields!      payload.EnumerateObject().Select(p => p.Name));
        }

        [Fact]
        public async Task CyclicReadWithAgeTestAsync()
        {
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CyclicReadWithAgeTestAsync), "./Resources/CyclicRead.json",
                TimeSpan.FromMinutes(1), 10, messageType: "ua-data",
                arguments: new string[] { "--mm=PubSub", "--dm=false" });

            // Assert
            Assert.Equal(10, messages.Count);
            var message = messages[0].Message;
            var output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
            Assert.NotNull(metadata);
        }

        [Fact]
        public async Task PeriodicHeartbeatTest()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(PeriodicHeartbeatTest), "./Resources/Heartbeat2.json",
                TimeSpan.FromMinutes(1), 10, messageType: "ua-data",
                arguments: new string[] { "--mm=PubSub", "-c" });

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal(10, messages.Count);

            // Assert that all messages are 1 second apart
            var timestamps = messages.ConvertAll(m => m.Message.GetProperty("Messages")[0]
                .GetProperty("Timestamp").GetDateTimeOffset());

            _output.WriteLine(string.Join('\n', timestamps
                        .Select(t => t.ToString("o", CultureInfo.InvariantCulture))
                        .ToArray()));
            var diffs = new List<TimeSpan>();
            for (var index = 0; index < timestamps.Count - 1; index++)
            {
                var diff = timestamps[index + 1] - timestamps[index];
                diffs.Add(diff);
            }
            _output.WriteLine(string.Join('\n', diffs
                        .Select(t => t.ToString())
                        .ToArray()));
#if FIX
            // Not stable enough when run with all tests together
            // TODO: Need a better and more reliable timer mechanism.
            var allowedVariance = TimeSpan.FromMilliseconds(10);
            Assert.All(diffs, diff => Assert.True(
                diff - TimeSpan.FromSeconds(1)< allowedVariance &&
                diff - TimeSpan.FromSeconds(1) > -allowedVariance, $"{diff} > {allowedVariance}"));
#endif
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1)]
        public async Task CanSendKeyFramesToIoTHubTest(int maxMonitoredItems)
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendKeyFramesToIoTHubTest), "./Resources/KeyFrames.json", TimeSpan.FromMinutes(2), 11,
                messageType: "ua-data", arguments: new[] { "--dm=false", "--xmi=" + maxMonitoredItems });

            // Assert
            var allDataSetMessages = messages.Select(m => m.Message.GetProperty("Messages")).SelectMany(m => m.EnumerateArray()).ToList();
            Assert.True(allDataSetMessages.Count >= 11);
            var dataSetMessages = allDataSetMessages.Take(11).ToArray();
            Assert.Equal("ua-keyframe", dataSetMessages[0].GetProperty("MessageType").GetString());
            Assert.All(dataSetMessages.AsSpan(1, 9).ToArray(), m => Assert.Equal("ua-deltaframe", m.GetProperty("MessageType").GetString()));
            Assert.Equal("ua-keyframe", dataSetMessages[10].GetProperty("MessageType").GetString());
            Assert.NotNull(metadata);
        }

        internal static JsonElement GetAlarmCondition(JsonElement jsonElement)
        {
            var messages = jsonElement.GetProperty("Messages");
            return messages.ValueKind != JsonValueKind.Array
                ? default
                : messages.EnumerateArray().FirstOrDefault(element =>
                {
                    return element.GetProperty("MessageType").GetString() == "ua-condition" &&
                        element.GetProperty("Payload").TryGetProperty("SourceNode", out var node) &&
                            node.TryGetProperty("Value", out node) &&
                                node.ValueKind != JsonValueKind.Null &&
                                node.GetString().StartsWith("http://opcfoundation.org/AlarmCondition#s=1%3a",
                                    StringComparison.InvariantCulture);
                });
        }

        internal static void AssertCompliantSimpleEventsMetadata(JsonMessage? metadata)
        {
            Assert.NotNull(metadata);
            var eventFields = metadata.Value.Message.GetProperty("MetaData").GetProperty("Fields");
            Assert.Equal(JsonValueKind.Array, eventFields.ValueKind);
            Assert.Collection(eventFields.EnumerateArray(),
                v =>
                {
                    Assert.Equal("EventId", v.GetProperty("Name").GetString());
                    Assert.Equal(15, v.GetProperty("DataType").GetProperty("Id").GetInt32());
                },
                v =>
                {
                    Assert.Equal("Message", v.GetProperty("Name").GetString());
                    Assert.Equal(21, v.GetProperty("DataType").GetProperty("Id").GetInt32());
                },
                v =>
                {
                    Assert.Equal(kCycleIdExpanded, v.GetProperty("Name").GetString());
                    Assert.Equal(12, v.GetProperty("DataType").GetProperty("Id").GetInt32());
                },
                v =>
                {
                    Assert.Equal(kCurrentStepExpanded, v.GetProperty("Name").GetString());
                    Assert.Equal(183, v.GetProperty("DataType").GetProperty("Id").GetInt32());
                    Assert.Equal("http://opcfoundation.org/SimpleEvents",
                        v.GetProperty("DataType").GetProperty("Namespace").GetString());
                });

            var namespaces = metadata.Value.Message.GetProperty("MetaData").GetProperty("Namespaces");
            Assert.Equal(JsonValueKind.Array, namespaces.ValueKind);
            Assert.Equal(24, namespaces.GetArrayLength());
            var structureDataTypes = metadata.Value.Message.GetProperty("MetaData").GetProperty("StructureDataTypes");
            Assert.Equal(JsonValueKind.Array, structureDataTypes.ValueKind);
            var s = structureDataTypes.EnumerateArray().First().GetProperty("StructureDefinition");
            Assert.Equal("Structure_0", s.GetProperty("StructureType").GetString());
            var structureFields = s.GetProperty("Fields");
            Assert.Equal(JsonValueKind.Array, structureFields.ValueKind);
            Assert.Collection(structureFields.EnumerateArray(),
                v =>
                {
                    Assert.Equal("Name", v.GetProperty("Name").GetString());
                    Assert.Equal(12, v.GetProperty("DataType").GetProperty("Id").GetInt32());
                },
                v =>
                {
                    Assert.Equal("Duration", v.GetProperty("Name").GetString());
                    Assert.Equal(11, v.GetProperty("DataType").GetProperty("Id").GetInt32());
                });
        }

        internal static void AssertSimpleEventsMetadata(JsonMessage? metadata)
        {
            Assert.NotNull(metadata);
            var eventFields = metadata.Value.Message.GetProperty("MetaData").GetProperty("Fields");
            Assert.Equal(JsonValueKind.Array, eventFields.ValueKind);
            Assert.Collection(eventFields.EnumerateArray(),
                v =>
                {
                    Assert.Equal("EventId", v.GetProperty("Name").GetString());
                    Assert.Equal("ByteString", v.GetProperty("DataType").GetString());
                },
                v =>
                {
                    Assert.Equal("Message", v.GetProperty("Name").GetString());
                    Assert.Equal("LocalizedText", v.GetProperty("DataType").GetString());
                },
                v =>
                {
                    Assert.Equal("http://opcfoundation.org/SimpleEvents#CycleId", v.GetProperty("Name").GetString());
                    Assert.Equal("String", v.GetProperty("DataType").GetString());
                },
                v =>
                {
                    Assert.Equal("http://opcfoundation.org/SimpleEvents#CurrentStep", v.GetProperty("Name").GetString());
                    Assert.Equal("http://opcfoundation.org/SimpleEvents#i=183", v.GetProperty("DataType").GetString());
                });

            var namespaces = metadata.Value.Message.GetProperty("MetaData").GetProperty("Namespaces");
            Assert.Equal(JsonValueKind.Array, namespaces.ValueKind);
            Assert.Equal(24, namespaces.GetArrayLength());
            var structureDataTypes = metadata.Value.Message.GetProperty("MetaData").GetProperty("StructureDataTypes");
            Assert.Equal(JsonValueKind.Array, structureDataTypes.ValueKind);
            var s = structureDataTypes.EnumerateArray().First().GetProperty("StructureDefinition");
            Assert.Equal("Structure_0", s.GetProperty("StructureType").GetString());
            var structureFields = s.GetProperty("Fields");
            Assert.Equal(JsonValueKind.Array, structureFields.ValueKind);
            Assert.Collection(structureFields.EnumerateArray(),
                v =>
                {
                    Assert.Equal("Name", v.GetProperty("Name").GetString());
                    Assert.Equal("String", v.GetProperty("DataType").GetString());
                },
                v =>
                {
                    Assert.Equal("Duration", v.GetProperty("Name").GetString());
                    Assert.Equal("Double", v.GetProperty("DataType").GetString());
                });
        }
    }
}
