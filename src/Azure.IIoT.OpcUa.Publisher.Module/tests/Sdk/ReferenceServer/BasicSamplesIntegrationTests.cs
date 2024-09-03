// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.ReferenceServer
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Encoders.Models;
    using FluentAssertions;
    using Json.More;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class BasicSamplesIntegrationTests : PublisherIntegrationTestBase
    {
        private const string kEventId = "EventId";
        private const string kMessage = "Message";
        private const string kCycleId = "http://opcfoundation.org/SimpleEvents#CycleId";
        private const string kCurrentStep = "http://opcfoundation.org/SimpleEvents#CurrentStep";
        private readonly ITestOutputHelper _output;
        private readonly ReferenceServer _fixture;

        public BasicSamplesIntegrationTests(ITestOutputHelper output)
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
            var messages = await ProcessMessagesAsync(nameof(CanSendDataItemToIoTHubTest),
                "./Resources/DataItems.json");

            // Assert
            var message = Assert.Single(messages);
            Assert.Equal("ns=23;i=1259", message.Message.GetProperty("NodeId").GetString());
            Assert.InRange(message.Message.GetProperty("Value").GetProperty("Value").GetDouble(),
                double.MinValue, double.MaxValue);
        }

        [Theory]
        [InlineData(MessageTimestamp.EncodingTimeUtc, HeartbeatBehavior.WatchdogLKV)]
        [InlineData(MessageTimestamp.EncodingTimeUtc, HeartbeatBehavior.WatchdogLKG)]
        [InlineData(MessageTimestamp.CurrentTimeUtc, HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps)]
        [InlineData(MessageTimestamp.PublishTime, HeartbeatBehavior.PeriodicLKV)]
        public async Task CanSendHeartbeatToIoTHubTest(MessageTimestamp timestamp, HeartbeatBehavior behavior)
        {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(nameof(CanSendHeartbeatToIoTHubTest) + timestamp, "./Resources/Heartbeat.json",
                TimeSpan.FromMinutes(2), 5, arguments: new[] { "--fm=True", $"--mts={timestamp}", $"--hbb={behavior}" });

            // Assert
            Assert.True(messages.Count > 1);
            var message = messages[0].Message;
            Assert.Equal("i=2271", message.GetProperty("NodeId").GetString());
            Assert.NotEmpty(message.GetProperty("ApplicationUri").GetString());
            Assert.NotEmpty(message.GetProperty("Timestamp").GetString());
            Assert.True(message.GetProperty("SequenceNumber").GetUInt32() > 0);
            _output.WriteLine(message.ToJsonString());
            Assert.Equal("en-US", message.GetProperty("Value").GetProperty("Value").EnumerateArray().First().GetString());

            var timestamps = new HashSet<DateTime> { message.GetProperty("Timestamp").GetDateTime() };
            for (var i = 1; i < messages.Count; i++)
            {
                message = messages[i].Message;
                Assert.Equal("i=2271", message.GetProperty("NodeId").GetString());
                Assert.NotEmpty(message.GetProperty("ApplicationUri").GetString());
                Assert.True(message.GetProperty("SequenceNumber").GetUInt32() > 0);
                Assert.Equal("en-US", message.GetProperty("Value").GetProperty("Value").EnumerateArray().First().GetString());

                if (timestamp == MessageTimestamp.PublishTime)
                {
                    Assert.False(message.TryGetProperty("Timestamp", out _));
                }
                else
                {
                    Assert.NotEmpty(message.GetProperty("Timestamp").GetString());
                    timestamps.Add(message.GetProperty("Timestamp").GetDateTime());
                }
            }

            Assert.Equal(timestamp != MessageTimestamp.PublishTime ? messages.Count : 1, timestamps.Count);
        }

        [Theory]
        [InlineData(HeartbeatBehavior.WatchdogLKV)]
        [InlineData(HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps)]
        [InlineData(HeartbeatBehavior.PeriodicLKV)]
        public async Task CanSendHeartbeatWithMIErrorToIoTHubTest(HeartbeatBehavior behavior)
        {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(nameof(CanSendHeartbeatWithMIErrorToIoTHubTest),
                "./Resources/HeartbeatErrors.json",
                TimeSpan.FromMinutes(2), 5, arguments: new[] { "--fm=True", $"--hbb={behavior}" });

            // Assert
            Assert.True(messages.Count > 1);
            var message = messages[0].Message;
            _output.WriteLine(message.ToJsonString());

            Assert.Equal("i=932534", message.GetProperty("NodeId").GetString());
            Assert.NotEmpty(message.GetProperty("ApplicationUri").GetString());
            Assert.True(message.GetProperty("SequenceNumber").GetUInt32() > 0);
            Assert.Equal("BadNodeIdUnknown", message.GetProperty("Value")
                .GetProperty("StatusCode").GetProperty("Symbol").GetString());
        }

        [Fact]
        public async Task CanSendDeadbandItemsToIoTHubTest()
        {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(nameof(CanSendDeadbandItemsToIoTHubTest),
                "./Resources/Deadband.json",
                TimeSpan.FromMinutes(2), 20, arguments: new[] { "--fm=True" });

            // Assert
            messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));
            var doubleValues = messages
                .Where(message => message.Message.GetProperty("DisplayName").GetString() == "DoubleValues" &&
                    message.Message.GetProperty("Value").TryGetProperty("Value", out _));
            double? dvalue = null;
            foreach (var message in doubleValues)
            {
                Assert.Equal("http://test.org/UA/Data/#i=11224", message.Message.GetProperty("NodeId").GetString());
                var value1 = message.Message.GetProperty("Value").GetProperty("Value").GetDouble();
                _output.WriteLine(JsonSerializer.Serialize(message));
                if (dvalue != null)
                {
                    var abs = Math.Abs(dvalue.Value - value1);
                    Assert.True(abs >= 5.0, $"Value within absolute deadband limit {abs} < 5 ({dvalue.Value}/{value1})");
                }
                dvalue = value1;
            }
            var int64Values = messages
                .Where(message => message.Message.GetProperty("DisplayName").GetString() == "Int64Values" &&
                    message.Message.GetProperty("Value").TryGetProperty("Value", out _));
            long? lvalue = null;
            foreach (var message in int64Values)
            {
                Assert.Equal("http://test.org/UA/Data/#i=11206", message.Message.GetProperty("NodeId").GetString());
                var value1 = message.Message.GetProperty("Value").GetProperty("Value").GetInt64();
                _output.WriteLine(JsonSerializer.Serialize(message));
                if (lvalue != null)
                {
                    var abs = Math.Abs(lvalue.Value - value1);
                    // TODO: Investigate this, it should be 10%
                    Assert.True(abs >= 3, $"Value within percent deadband limit {abs} < 3% ({lvalue.Value}/{value1})");
                }
                lvalue = value1;
            }
        }

        [Fact]
        public async Task CanSendEventToIoTHubTest()
        {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(nameof(CanSendEventToIoTHubTest),
                "./Resources/SimpleEvents.json");

            // Assert
            var message = Assert.Single(messages).Message;
            Assert.Equal("i=2253", message.GetProperty("NodeId").GetString());
            Assert.False(message.TryGetProperty("ApplicationUri", out _));
            Assert.False(message.TryGetProperty("Timestamp", out _));
            Assert.False(message.TryGetProperty("SequenceNumber", out _));
            Assert.NotEmpty(message.GetProperty("Value").GetProperty("EventId").GetString());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendEventToIoTHubTestFullFeaturedMessage(bool useCurrentTime)
        {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(
                nameof(CanSendEventToIoTHubTestFullFeaturedMessage), "./Resources/SimpleEvents.json",
                arguments: new string[] { "--fm=true", useCurrentTime ? "--mts=CurrentTimeUtc" : "--mts=PublishTime" });

            // Assert
            var message = Assert.Single(messages).Message;
            Assert.Equal("i=2253", message.GetProperty("NodeId").GetString());
            Assert.NotEmpty(message.GetProperty("ApplicationUri").GetString());
            Assert.NotEmpty(message.GetProperty("Timestamp").GetString());
            Assert.True(message.GetProperty("SequenceNumber").GetUInt32() > 0);
            Assert.NotEmpty(message.GetProperty("Value").GetProperty("EventId").GetString());
        }

        [Fact]
        public async Task CanEncodeWithReversibleEncodingSamplesTest()
        {
            // Arrange
            // Act
            var result = await ProcessMessagesAsync(
                nameof(CanEncodeWithReversibleEncodingSamplesTest), "./Resources/SimpleEvents.json",
                arguments: new[] { "--mm=Samples", "--me=JsonReversible" }
            );

            var m = Assert.Single(result).Message;

            var value = m.GetProperty("Value");
            var type = value.GetProperty("Type").GetString();
            var body = value.GetProperty("Body");
            Assert.Equal("ExtensionObject", type);

            var encoding = body.GetProperty("Encoding").GetString();
            body = body.GetProperty("Body");
            Assert.Equal("Json", encoding);

            var eventId = body.GetProperty(kEventId);
            Assert.Equal("ByteString", eventId.GetProperty("Type").GetString());
            Assert.Equal(JsonValueKind.String, eventId.GetProperty("Body").ValueKind);

            var message = body.GetProperty(kMessage);
            Assert.Equal("LocalizedText", message.GetProperty("Type").GetString());
            Assert.Equal(JsonValueKind.String, message.GetProperty("Body").GetProperty("Text").ValueKind);
            Assert.Equal("en-US", message.GetProperty("Body").GetProperty("Locale").GetString());

            var cycleId = body.GetProperty(kCycleId);
            Assert.Equal("String", cycleId.GetProperty("Type").GetString());
            Assert.Equal(JsonValueKind.String, cycleId.GetProperty("Body").ValueKind);

            var currentStep = body.GetProperty(kCurrentStep);
            body = currentStep.GetProperty("Body");
            Assert.Equal("ExtensionObject", currentStep.GetProperty("Type").GetString());
            Assert.Equal("http://opcfoundation.org/SimpleEvents#i=183", body.GetProperty("TypeId").GetString());
            Assert.Equal("Json", body.GetProperty("Encoding").GetString());
            Assert.Equal(JsonValueKind.String, body.GetProperty("Body").GetProperty("Name").ValueKind);
            Assert.Equal(JsonValueKind.Number, body.GetProperty("Body").GetProperty("Duration").ValueKind);

            var json = value
                        .GetProperty("Body")
                        .GetProperty("Body")
                        .GetRawText();
            var buffer = Encoding.UTF8.GetBytes(json);

            var serviceMessageContext = new ServiceMessageContext();
            serviceMessageContext.Factory.AddEncodeableType(typeof(EncodeableDictionary));

            await using (var stream = new MemoryStream(buffer))
            {
                using var decoder = new JsonDecoderEx(stream, serviceMessageContext);
                var actual = new EncodeableDictionary();
                actual.Decode(decoder);

                Assert.Equal(4, actual.Count);
                Assert.Equal(new[] { kEventId, kMessage, kCycleId, kCurrentStep }, actual.Select(x => x.Key));
                Assert.All(actual.Select(x => x.Value?.Value), Assert.NotNull);

                var eof = decoder.ReadDataValue(null);
                Assert.Null(eof);
            }
        }

        [Fact]
        public async Task CanSendPendingConditionsToIoTHubTest()
        {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(nameof(CanSendPendingConditionsToIoTHubTest),
                "./Resources/PendingAlarms.json", GetAlarmCondition);

            // Assert
            messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));
            var evt = Assert.Single(messages).Message;

            Assert.Equal(JsonValueKind.Object, evt.ValueKind);
            Assert.Equal("i=2253", evt.GetProperty("NodeId").GetString());
            Assert.Equal("PendingAlarms", evt.GetProperty("DisplayName").GetString());

            Assert.True(evt.TryGetProperty("Value", out var sev));
            Assert.True(sev.TryGetProperty("Severity", out sev));
            Assert.True(sev.TryGetProperty("Value", out sev));
            Assert.True(sev.GetInt32() >= 100);
        }

        [Fact]
        public async Task CanSendDataItemToIoTHubTestWithDeviceMethod()
        {
            const string name = nameof(CanSendDataItemToIoTHubTestWithDeviceMethod);
            var testInput = GetEndpointsFromFile(name, "./Resources/DataItems.json");
            StartPublisher(name, arguments: new string[] { "--mm=FullSamples" }); // Alternative to --fm=True
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0]);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync();
                var message = Assert.Single(messages).Message;
                Assert.Equal("ns=23;i=1259", message.GetProperty("NodeId").GetString());
                Assert.InRange(message.GetProperty("Value").GetProperty("Value").GetDouble(),
                    double.MinValue, double.MaxValue);

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
        public async Task CanSendEventToIoTHubTestWithDeviceMethod()
        {
            const string name = nameof(CanSendEventToIoTHubTestWithDeviceMethod);
            var testInput = GetEndpointsFromFile(name, "./Resources/SimpleEvents.json");
            StartPublisher(name);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0]);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync();
                var message = Assert.Single(messages).Message;
                Assert.Equal("i=2253", message.GetProperty("NodeId").GetString());
                Assert.NotEmpty(message.GetProperty("Value").GetProperty("EventId").GetString());

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
        public async Task CanSendPendingConditionsToIoTHubTestWithDeviceMethod()
        {
            const string name = nameof(CanSendPendingConditionsToIoTHubTestWithDeviceMethod);
            var testInput = GetEndpointsFromFile(name, "./Resources/PendingAlarms.json");
            StartPublisher(name);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0]);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync(GetAlarmCondition);
                messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));

                var evt = Assert.Single(messages).Message;
                Assert.Equal(JsonValueKind.Object, evt.ValueKind);
                Assert.Equal("i=2253", evt.GetProperty("NodeId").GetString());
                Assert.Equal("PendingAlarms", evt.GetProperty("DisplayName").GetString());

                Assert.True(evt.TryGetProperty("Value", out var sev));
                Assert.True(sev.TryGetProperty("Severity", out sev));
                Assert.True(sev.TryGetProperty("Value", out sev));
                Assert.True(sev.GetInt32() >= 100);

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

        [Fact]
        public async Task CanSendDataItemToIoTHubTestWithDeviceMethod2()
        {
            const string name = nameof(CanSendDataItemToIoTHubTestWithDeviceMethod2);
            var testInput1 = GetEndpointsFromFile(name, "./Resources/DataItems.json");
            var testInput2 = GetEndpointsFromFile(name, "./Resources/SimpleEvents.json");
            var testInput3 = GetEndpointsFromFile(name, "./Resources/PendingAlarms.json");
            StartPublisher(name);
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

                await PublisherApi.AddOrUpdateEndpointsAsync(new List<PublishedNodesEntryModel> {
                    new()
                    {
                        OpcNodes = nodes.OpcNodes.ToList(),
                        EndpointUrl = e.EndpointUrl,
                        UseSecurity = e.UseSecurity,
                        DataSetWriterGroup = name
                    }
                });

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                e = Assert.Single(endpoints.Endpoints);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e);
                Assert.Equal(3, nodes.OpcNodes.Count);

                _output.WriteLine("Removing items...");
                await PublisherApi.UnpublishNodesAsync(testInput3[0]);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e);
                Assert.Equal(2, nodes.OpcNodes.Count);
                await PublisherApi.UnpublishNodesAsync(testInput2[0]);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e);
                Assert.Single(nodes.OpcNodes);

                _output.WriteLine("Waiting for remaining...");
                var messages = await WaitForMessagesAsync(GetDataFrame);
                var message = Assert.Single(messages).Message;
                Assert.Equal("ns=23;i=1259", message.GetProperty("NodeId").GetString());
                Assert.InRange(message.GetProperty("Value").GetProperty("Value").GetDouble(),
                    double.MinValue, double.MaxValue);

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
        public async Task CanSendPendingConditionsToIoTHubTestWithDeviceMethod2()
        {
            const string name = nameof(CanSendPendingConditionsToIoTHubTestWithDeviceMethod2);
            var testInput = GetEndpointsFromFile(name, "./Resources/PendingAlarms.json");

            StartPublisher(name);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0]);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync(GetAlarmCondition);
                messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));
                var evt = Assert.Single(messages).Message;

                Assert.Equal(JsonValueKind.Object, evt.ValueKind);
                Assert.Equal("i=2253", evt.GetProperty("NodeId").GetString());
                Assert.Equal("PendingAlarms", evt.GetProperty("DisplayName").GetString());

                Assert.True(evt.TryGetProperty("Value", out var sev));
                Assert.True(sev.TryGetProperty("Severity", out sev));
                Assert.True(sev.TryGetProperty("Value", out sev));
                Assert.True(sev.GetInt32() >= 100);

                // Disable pending alarms
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
                var n = Assert.Single(nodes.OpcNodes);

                // Wait until it was applied and we receive normal events again
                messages = await WaitForMessagesAsync(
                    message => message.GetProperty("DisplayName").GetString() == "SimpleEvents"
                        && message.GetProperty("Value").GetProperty("ReceiveTime").ValueKind
                            == JsonValueKind.String ? message : default);
                messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));

                var message = Assert.Single(messages).Message;
                Assert.Equal("i=2253", message.GetProperty("NodeId").GetString());
                Assert.Equal("SimpleEvents", message.GetProperty("DisplayName").GetString());

                Assert.True(message.TryGetProperty("Value", out sev));
                Assert.True(sev.TryGetProperty("Severity", out sev));
                Assert.True(sev.GetInt32() != 0, $"{message.ToJsonString()}");

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

        private JsonElement GetDataFrame(JsonElement jsonElement)
        {
            return jsonElement.GetProperty("NodeId").GetString() != "i=2253"
                    ? jsonElement : default;
        }

        private static JsonElement GetAlarmCondition(JsonElement jsonElement)
        {
            return jsonElement
                .TryGetProperty("Value", out var node) && node
                .TryGetProperty("SourceNode", out node) && node
                .TryGetProperty("Value", out node) && node
                .GetString().StartsWith("http://opcfoundation.org/AlarmCondition#s=1%3a",
                    StringComparison.InvariantCulture) ? jsonElement : default;
        }
    }
}
