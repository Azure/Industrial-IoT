// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.ReferenceServer
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Json.More;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class ReferenceServerIntegrationTests : PublisherIntegrationTestBase, IClassFixture<ReferenceServer>
    {
        private const string kEventId = "EventId";
        private const string kMessage = "Message";
        private const string kCycleId = "http://opcfoundation.org/SimpleEvents#CycleId";
        private const string kCurrentStep = "http://opcfoundation.org/SimpleEvents#CurrentStep";
        private const string kOutput = "Output";
        private const string kDoubleValues = "DoubleValues";
        private const string kInt64Values = "Int64Values";
        private readonly ITestOutputHelper _output;
        private readonly ReferenceServer _fixture;

        public ReferenceServerIntegrationTests(ReferenceServer fixture, ITestOutputHelper output)
            : base(output)
        {
            _output = output;
            _fixture = fixture;
            EndpointUrl = _fixture.EndpointUrl;
        }

        [Fact]
        public async Task CanSendDataItemToIoTHubTestAsync()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemToIoTHubTestAsync), "./Resources/DataItems.json",
                messageType: "ua-data", arguments: ["--mm=PubSub", "--dm=false"]);

            // Assert
            var message = Assert.Single(messages).Message;
            AssertDataItemNetworkMessage(message);
            Assert.NotNull(metadata);
        }

        [Theory]
        [InlineData(MessageTimestamp.EncodingTimeUtc, HeartbeatBehavior.WatchdogLKV)]
        [InlineData(MessageTimestamp.EncodingTimeUtc, HeartbeatBehavior.WatchdogLKG)]
        [InlineData(MessageTimestamp.CurrentTimeUtc, HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps)]
        [InlineData(MessageTimestamp.PublishTime, HeartbeatBehavior.PeriodicLKV)]
        public async Task CanSendHeartbeatToIoTHubTestAsync(MessageTimestamp timestamp, HeartbeatBehavior behavior)
        {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(nameof(CanSendHeartbeatToIoTHubTestAsync) + timestamp,
                "./Resources/Heartbeat.json", TimeSpan.FromMinutes(2), 5, messageType: "ua-data",
                arguments: ["--mm=PubSub", "--fm=True", $"--mts={timestamp}", $"--hbb={behavior}"]);

            // Assert
            Assert.True(messages.Count > 1);
            var timestamps = new HashSet<DateTimeOffset>();
            foreach (var item in messages)
            {
                var message = item.Message;
                _output.WriteLine(message.ToJsonString());
                var dataSetMessage = message.GetProperty("Messages")[0];
                var payload = dataSetMessage.GetProperty("Payload");
                var value = GetOnlyDataField(payload).GetProperty("Value");
                Assert.Equal("en-US", value.EnumerateArray().First().GetString());
                Assert.NotEmpty(payload.GetProperty("ApplicationUri").GetProperty("Value").GetString());
                Assert.True(dataSetMessage.GetProperty("SequenceNumber").GetUInt32() > 0);

                if (dataSetMessage.TryGetProperty("Timestamp", out var messageTimestamp))
                {
                    Assert.NotEmpty(messageTimestamp.GetString());
                    timestamps.Add(messageTimestamp.GetDateTimeOffset());
                }
            }
            if (timestamp == MessageTimestamp.PublishTime)
            {
                Assert.NotEmpty(timestamps);
            }
            else
            {
                Assert.Equal(messages.Count, timestamps.Count);
            }
        }

        [Theory]
        [InlineData(HeartbeatBehavior.WatchdogLKV)]
        [InlineData(HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps)]
        [InlineData(HeartbeatBehavior.PeriodicLKV)]
        public async Task CanSendHeartbeatWithMIErrorToIoTHubTestAsync(HeartbeatBehavior behavior)
        {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(nameof(CanSendHeartbeatWithMIErrorToIoTHubTestAsync),
                "./Resources/HeartbeatErrors.json", TimeSpan.FromMinutes(2), 5, messageType: "ua-data",
                arguments: ["--mm=PubSub", "--fm=True", $"--hbb={behavior}"]);

            // Assert
            Assert.True(messages.Count > 1);
            var statusCodes = new List<uint>();
            foreach (var item in messages)
            {
                var message = item.Message;
                _output.WriteLine(message.ToJsonString());
                var dataSetMessage = message.GetProperty("Messages")[0];
                var payload = dataSetMessage.GetProperty("Payload");

                Assert.NotEmpty(payload.GetProperty("ApplicationUri").GetProperty("Value").GetString());
                Assert.True(dataSetMessage.GetProperty("SequenceNumber").GetUInt32() > 0);
                //
                // Part 6 §5.4.2.18 Table 42 names this member Status. The stack
                // wrote StatusCode until the conformance issue raised against it
                // was accepted upstream, and the custom encoder takes the name
                // from the stack, so both paths now spell it the specification's
                // way. Recorded as a 3.0 wire change rather than pinned back,
                // because pinning it would make the two paths disagree and turn
                // the native default switch into a second silent change.
                //
                // The code is asserted rather than the symbolic name because
                // Symbol is a verbose-encoding member - Part 6 §5.4.2.6 - and
                // this test publishes compact, where a status is
                // {"Code":n} and Good is {}. The custom encoder wrote the
                // symbol under either encoding; that is another 3.0 change.
                // The code identifies the status exactly, so this is the
                // stronger assertion as well as the encoding-independent one.
                //
                statusCodes.Add(GetStatusCode(GetOnlyDataField(payload)));
            }
            Assert.Contains(kBadNodeIdUnknown, statusCodes);

            static uint GetStatusCode(JsonElement field)
            {
                if (!field.TryGetProperty("Status", out var status))
                {
                    return 0u;
                }
                return status.ValueKind == JsonValueKind.Number
                    ? status.GetUInt32()
                    : status.TryGetProperty("Code", out var code) ? code.GetUInt32() : 0u;
            }
        }

        /// <summary>
        /// <c>BadNodeIdUnknown</c>, spelled out because the assertion above is
        /// on the wire value rather than on a name the encoding may not carry.
        /// </summary>
        private const uint kBadNodeIdUnknown = 0x80340000;

        [Fact]
        public async Task CanSendDeadbandItemsToIoTHubTestAsync()
        {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(nameof(CanSendDeadbandItemsToIoTHubTestAsync),
                "./Resources/Deadband.json", TimeSpan.FromMinutes(2), 20, messageType: "ua-data",
                arguments: ["--mm=PubSub", "--fm=True"]);

            // Assert
            messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));
            var payloads = messages.SelectMany(m => m.Message.GetProperty("Messages").EnumerateArray())
                .Select(m => m.GetProperty("Payload"))
                .ToList();

            var doubleValues = payloads.Where(payload => payload.TryGetProperty(kDoubleValues, out var value) &&
                value.TryGetProperty("Value", out _));
            AssertDeadband<double>(doubleValues, kDoubleValues, value => value.GetDouble(),
                (previous, current) => Math.Abs(previous - current) >= 5.0,
                (previous, current) => Math.Abs(previous - current), "absolute deadband limit {0} < 5 ({1}/{2})");

            var int64Values = payloads.Where(payload => payload.TryGetProperty(kInt64Values, out var value) &&
                value.TryGetProperty("Value", out _));
            AssertDeadband<long>(int64Values, kInt64Values, ReadInt64,
                (previous, current) => Math.Abs(previous - current) >= 3,
                (previous, current) => Math.Abs(previous - current), "percent deadband limit {0} < 3% ({1}/{2})");

            //
            // Part 6 5.4.2.3 has a 64 bit integer written as a JSON string, so
            // that a consumer whose numbers are doubles cannot silently lose
            // precision on a value it cannot represent. The number form is
            // still accepted because a value small enough is legal either way
            // and this assertion is about the deadband, not the spelling.
            //
            static long ReadInt64(JsonElement value)
            {
                return value.ValueKind == JsonValueKind.String
                    ? long.Parse(value.GetString()!, CultureInfo.InvariantCulture)
                    : value.GetInt64();
            }
        }

        [Fact]
        public async Task CanSendEventToIoTHubTestAsync()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(nameof(CanSendEventToIoTHubTestAsync),
                "./Resources/SimpleEvents.json", messageType: "ua-data", arguments: ["--mm=PubSub", "--dm=false"]);

            // Assert
            var payload = AssertSimpleEventNetworkMessage(messages);
            Assert.NotEmpty(payload.GetProperty(kEventId).GetProperty("Value").GetString());
            Assert.NotNull(metadata);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendEventToIoTHubTestFullFeaturedMessageAsync(bool useCurrentTime)
        {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(
                nameof(CanSendEventToIoTHubTestFullFeaturedMessageAsync), "./Resources/SimpleEvents.json",
                messageType: "ua-data", arguments: ["--mm=PubSub", "--fm=true", useCurrentTime ? "--mts=CurrentTimeUtc" : "--mts=PublishTime"]);

            // Assert
            var message = Assert.Single(messages).Message;
            var dataSetMessage = message.GetProperty("Messages")[0];
            var payload = dataSetMessage.GetProperty("Payload");
            Assert.NotEmpty(dataSetMessage.GetProperty("Timestamp").GetString());
            Assert.True(dataSetMessage.GetProperty("SequenceNumber").GetUInt32() > 0);
            Assert.NotEmpty(payload.GetProperty(kEventId).GetProperty("Value").GetString());
            Assert.NotEmpty(payload.GetProperty("ApplicationUri").GetProperty("Value").GetString());
        }

        [Fact]
        public async Task CanEncodeWithReversibleEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                nameof(CanEncodeWithReversibleEncodingTestAsync),
                "./Resources/SimpleEvents.json", TimeSpan.FromMinutes(2), 4, messageType: "ua-data",
                arguments: ["--mm=PubSub", "--me=JsonReversible", "--dm=false"]
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

            BasicPubSubIntegrationTests.AssertSimpleEventsMetadata(metadata);
        }

        [Fact]
        public async Task CanSendPendingConditionsToIoTHubTestAsync()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendPendingConditionsToIoTHubTestAsync), "./Resources/PendingAlarms.json",
                BasicPubSubIntegrationTests.GetAlarmCondition, messageType: "ua-data", arguments: ["--mm=PubSub", "--dm=false"]);

            // Assert
            AssertPendingAlarmDataSetMessage(Assert.Single(messages).Message);
            Assert.NotNull(metadata);
        }

        [Fact]
        public async Task CanSendDataItemToIoTHubTestWithDeviceMethodAsync()
        {
            const string name = nameof(CanSendDataItemToIoTHubTestWithDeviceMethodAsync);
            var testInput = GetEndpointsFromFile(name, "./Resources/DataItems.json");
            StartPublisher(name, arguments: ["--mm=FullNetworkMessages"]);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0]);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync(messageType: "ua-data");
                AssertDataItemNetworkMessage(Assert.Single(messages).Message);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                result = await PublisherApi.UnpublishNodesAsync(e);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                await StopPublisherAsync();
            }
        }

        [Fact]
        public async Task CanSendEventToIoTHubTestWithDeviceMethodAsync()
        {
            const string name = nameof(CanSendEventToIoTHubTestWithDeviceMethodAsync);
            var testInput = GetEndpointsFromFile(name, "./Resources/SimpleEvents.json");
            StartPublisher(name);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0]);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync(messageType: "ua-data");
                var payload = AssertSimpleEventNetworkMessage(messages);
                Assert.NotEmpty(payload.GetProperty(kEventId).GetProperty("Value").GetString());

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                result = await PublisherApi.UnpublishAllNodesAsync();
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                await StopPublisherAsync();
            }
        }

        [Fact]
        public async Task CanSendPendingConditionsToIoTHubTestWithDeviceMethodAsync()
        {
            const string name = nameof(CanSendPendingConditionsToIoTHubTestWithDeviceMethodAsync);
            var testInput = GetEndpointsFromFile(name, "./Resources/PendingAlarms.json");
            StartPublisher(name);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0]);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync(BasicPubSubIntegrationTests.GetAlarmCondition, messageType: "ua-data");
                messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));
                AssertPendingAlarmDataSetMessage(Assert.Single(messages).Message);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                result = await PublisherApi.UnpublishNodesAsync(testInput[0]);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                await StopPublisherAsync();
            }
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1)]
        public async Task CanSendDataItemToIoTHubTestWithDeviceMethod2Async(int maxMonitoredItems)
        {
            const string name = nameof(CanSendDataItemToIoTHubTestWithDeviceMethod2Async);
            var testInput1 = GetEndpointsFromFile(name, "./Resources/DataItems.json");
            var testInput2 = GetEndpointsFromFile(name, "./Resources/SimpleEvents.json");
            var testInput3 = GetEndpointsFromFile(name, "./Resources/PendingAlarms.json");
            StartPublisher(name, arguments: ["--xmi=" + maxMonitoredItems]);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);

                await PublisherApi.PublishNodesAsync(testInput1[0]);
                await PublisherApi.PublishNodesAsync(testInput2[0]);
                await PublisherApi.PublishNodesAsync(testInput3[0]);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                var e = Assert.Single(endpoints.Endpoints);
                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e);
                Assert.Equal(3, nodes.OpcNodes.Count);

                await PublisherApi.UnpublishAllNodesAsync();
                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);

                await PublisherApi.AddOrUpdateEndpointsAsync(new List<PublishedNodesEntryModel>
                {
                    new ()
                    {
                        OpcNodes = [.. nodes.OpcNodes],
                        EndpointUrl = e.EndpointUrl,
                        UseSecurity = e.UseSecurity,
                        DataSetWriterGroup = name
                    }
                });

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                e = Assert.Single(endpoints.Endpoints);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e);
                Assert.Equal(3, nodes.OpcNodes.Count);

                var messages1 = await WaitForMessagesAsync(GetDataFrame, messageType: "ua-data");
                AssertDataItemDataSetMessage(Assert.Single(messages1).Message);

                _output.WriteLine("Removing items...");
                await PublisherApi.UnpublishNodesAsync(testInput3[0]);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e);
                Assert.Equal(2, nodes.OpcNodes.Count);
                await PublisherApi.UnpublishNodesAsync(testInput2[0]);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e);
                Assert.Single(nodes.OpcNodes);

                _output.WriteLine("Waiting for remaining...");
                var messages = await WaitForMessagesAsync(GetDataFrame, messageType: "ua-data");
                AssertDataItemDataSetMessage(Assert.Single(messages).Message);

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                var diag = Assert.Single(diagnostics);
                Assert.Equal(e.EndpointUrl, diag.Endpoint.EndpointUrl);
            }
            finally
            {
                await StopPublisherAsync();
            }
        }

        [Fact]
        public async Task CanSendPendingConditionsToIoTHubTestWithDeviceMethod2Async()
        {
            const string name = nameof(CanSendPendingConditionsToIoTHubTestWithDeviceMethod2Async);
            var testInput = GetEndpointsFromFile(name, "./Resources/PendingAlarms.json");

            StartPublisher(name);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0]);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync(BasicPubSubIntegrationTests.GetAlarmCondition, messageType: "ua-data");
                messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));
                AssertPendingAlarmDataSetMessage(Assert.Single(messages).Message);

                testInput[0].OpcNodes[0].ConditionHandling = null;
                testInput[0].OpcNodes[0].DisplayName = "SimpleEvents";
                result = await PublisherApi.AddOrUpdateEndpointsAsync(new List<PublishedNodesEntryModel>
                {
                    testInput[0]
                });
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e);
                Assert.Single(nodes.OpcNodes);

                messages = await WaitForMessagesAsync(GetSimpleEvent, messageType: "ua-data");
                messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));

                var message = Assert.Single(messages).Message;
                var payload = message.GetProperty("Payload");
                if (message.TryGetProperty("DataSetWriterName", out var writerName))
                {
                    Assert.Equal("SimpleEvents", writerName.GetString()?.Split('|').Last());
                }
                Assert.True(payload.TryGetProperty("Severity", out var sev));
                Assert.True(sev.GetProperty("Value").GetInt32() != 0, $"{message.ToJsonString()}");

                result = await PublisherApi.UnpublishNodesAsync(testInput[0]);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                await StopPublisherAsync();
            }
        }

        private static void AssertDataItemNetworkMessage(JsonElement message)
        {
            Assert.Equal("ua-data", message.GetProperty("MessageType").GetString());
            AssertDataItemPayload(message.GetProperty("Messages")[0].GetProperty("Payload"));
        }

        private static void AssertDataItemDataSetMessage(JsonElement message)
        {
            AssertDataItemPayload(message.GetProperty("Payload"));
        }

        private static void AssertDataItemPayload(JsonElement payload)
        {
            var output = payload.GetProperty(kOutput);
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
        }

        private static JsonElement AssertSimpleEventNetworkMessage(List<JsonMessage> messages)
        {
            var message = Assert.Single(messages).Message;
            Assert.Equal("ua-data", message.GetProperty("MessageType").GetString());
            return message.GetProperty("Messages")[0].GetProperty("Payload");
        }

        private static void AssertPendingAlarmDataSetMessage(JsonElement message)
        {
            Assert.Equal(JsonValueKind.Object, message.ValueKind);
            var payload = message.GetProperty("Payload");
            Assert.True(payload.GetProperty("Severity").GetProperty("Value").GetInt32() >= 0);
        }

        private static void AssertDeadband<T>(IEnumerable<JsonElement> payloads, string fieldName,
            Func<JsonElement, T> read, Func<T, T, bool> isOutsideDeadband,
            Func<T, T, object> getDifference, string messageFormat)
        {
            var any = false;
            T previousValue = default!;
            DateTimeOffset? previousSourceTimestamp = null;
            foreach (var payload in payloads)
            {
                any = true;
                var value = payload.GetProperty(fieldName);
                var currentValue = read(value.GetProperty("Value"));
                var sourceTimestamp = value.GetProperty("SourceTimestamp").GetDateTimeOffset();
                if (sourceTimestamp == previousSourceTimestamp)
                {
                    continue;
                }
                if (previousSourceTimestamp != null)
                {
                    Assert.True(isOutsideDeadband(previousValue, currentValue), string.Format(CultureInfo.InvariantCulture,
                        messageFormat, getDifference(previousValue, currentValue), previousValue, currentValue));
                }
                previousValue = currentValue;
                previousSourceTimestamp = sourceTimestamp;
            }
            Assert.True(any, $"No {fieldName} values were sent");
        }

        private static JsonElement GetOnlyDataField(JsonElement payload)
        {
            var fields = payload.EnumerateObject()
                .Where(p => p.Name != "EndpointUrl" && p.Name != "ApplicationUri")
                .Select(p => p.Value)
                .ToArray();
            return Assert.Single(fields);
        }

        private static JsonElement GetDataFrame(JsonElement jsonElement)
        {
            var messages = jsonElement.GetProperty("Messages");
            return messages.ValueKind != JsonValueKind.Array
                ? default
                : messages.EnumerateArray().FirstOrDefault(element =>
                    element.GetProperty("Payload").TryGetProperty(kOutput, out _));
        }

        private static JsonElement GetSimpleEvent(JsonElement jsonElement)
        {
            var messages = jsonElement.GetProperty("Messages");
            return messages.ValueKind != JsonValueKind.Array
                ? default
                : messages.EnumerateArray().FirstOrDefault(element =>
                    element.TryGetProperty("Payload", out var payload) &&
                    payload.TryGetProperty("ReceiveTime", out var receiveTime) &&
                    receiveTime.GetProperty("Value").ValueKind == JsonValueKind.String);
        }
    }
}
