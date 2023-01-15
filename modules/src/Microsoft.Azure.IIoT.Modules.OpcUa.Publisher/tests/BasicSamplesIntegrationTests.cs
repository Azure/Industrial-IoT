// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Tests {
    using FluentAssertions;
    using Json.More;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Opc.Ua;
    using Opc.Ua.Encoders;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Currently, we create new independent instances of server, publisher and mocked IoT services for each test,
    /// this could be optimised e.g. create only single instance of server and publisher between tests in the same class.
    /// </summary>
    [Collection(ReadCollection.Name)]
    public class BasicSamplesIntegrationTests : PublisherIntegrationTestBase {
        private const string kEventId = "EventId";
        private const string kMessage = "Message";
        private const string kCycleId = "http://opcfoundation.org/SimpleEvents#CycleId";
        private const string kCurrentStep = "http://opcfoundation.org/SimpleEvents#CurrentStep";
        private readonly ITestOutputHelper _output;

        public BasicSamplesIntegrationTests(ReferenceServerFixture fixture, ITestOutputHelper output) : base(fixture) {
            _output = output;
        }

        [Fact]
        public async Task CanSendDataItemToIoTHubTest() {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(@"./PublishedNodes/DataItems.json").ConfigureAwait(false);

            // Assert
            var message = Assert.Single(messages);
            Assert.Equal("ns=21;i=1259", message.GetProperty("NodeId").GetString());
            Assert.InRange(message.GetProperty("Value").GetProperty("Value").GetDouble(),
                double.MinValue, double.MaxValue);
        }

        [Fact]
        public async Task CanSendDeadbandItemsToIoTHubTest() {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(@"./PublishedNodes/Deadband.json",
                TimeSpan.FromMinutes(2), 200, arguments: new[] { "--fm=True" } ).ConfigureAwait(false);

            // Assert
            var doubleValues = messages
                .Where(message => message.GetProperty("DisplayName").GetString() == "DoubleValues" &&
                    message.GetProperty("Value").TryGetProperty("Value", out _));
            double? dvalue = null;
            foreach (var message in doubleValues) {
                Assert.Equal("http://test.org/UA/Data/#i=11224", message.GetProperty("NodeId").GetString());
                var value1 = message.GetProperty("Value").GetProperty("Value").GetDouble();
                _output.WriteLine(JsonSerializer.Serialize(message));
                if (dvalue != null) {
                    var abs = Math.Abs(dvalue.Value - value1);
                    Assert.True(abs >= 5.0, $"Value within absolute deadband limit {abs} < 5");
                }
                dvalue = value1;
            }
            var int64Values = messages
                .Where(message => message.GetProperty("DisplayName").GetString() == "Int64Values" &&
                    message.GetProperty("Value").TryGetProperty("Value", out _));
            long? lvalue = null;
            foreach (var message in int64Values) {
                Assert.Equal("http://test.org/UA/Data/#i=11206", message.GetProperty("NodeId").GetString());
                var value1 = message.GetProperty("Value").GetProperty("Value").GetInt64();
                _output.WriteLine(JsonSerializer.Serialize(message));
                if (lvalue != null) {
                    var abs = Math.Abs(lvalue.Value - value1);
                    // TODO: Investigate this, it should be 10%
                    Assert.True(abs >= 3, $"Value within percent deadband limit {abs} < 3%");
                }
                lvalue = value1;
            }
        }

        [Fact]
        public async Task CanSendEventToIoTHubTest() {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(@"./PublishedNodes/SimpleEvents.json").ConfigureAwait(false);

            // Assert
            var message = Assert.Single(messages);
            Assert.Equal("i=2253", message.GetProperty("NodeId").GetString());
            Assert.False(message.TryGetProperty("ApplicationUri", out _));
            Assert.False(message.TryGetProperty("Timestamp", out _));
            Assert.False(message.TryGetProperty("SequenceNumber", out _));
            Assert.NotEmpty(message.GetProperty("Value").GetProperty("EventId").GetString());
        }

        [Fact]
        public async Task CanSendEventToIoTHubTestFulLFeaturedMessage() {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(@"./PublishedNodes/SimpleEvents.json",
                arguments: new string[] { "--fm=True" }).ConfigureAwait(false);

            // Assert
            var message = Assert.Single(messages);
            Assert.Equal("i=2253", message.GetProperty("NodeId").GetString());
            Assert.NotEmpty(message.GetProperty("ApplicationUri").GetString());
            Assert.NotEmpty(message.GetProperty("Timestamp").GetString());
            Assert.True(message.GetProperty("SequenceNumber").GetUInt32() > 0);
            Assert.NotEmpty(message.GetProperty("Value").GetProperty("EventId").GetString());
        }

        [Theory]
        [InlineData(@"./PublishedNodes/SimpleEvents.json")]
        public async Task CanEncodeWithReversibleEncodingSamplesTest(string publishedNodesFile) {
            // Arrange
            // Act
            var result = await ProcessMessagesAsync(
                publishedNodesFile,
                arguments: new[] { "--mm=Samples", "--me=JsonReversible" }
            ).ConfigureAwait(false);

            var m = Assert.Single(result);

            var value = m.GetProperty("Value");
            var type = value.GetProperty("Type").GetString();
            var body = value.GetProperty("Body");
            Assert.Equal("ExtensionObject", type);

            var typeId = body.GetProperty("TypeId").GetString();
            var encoding = body.GetProperty("Encoding").GetString();
            body = body.GetProperty("Body");
            Assert.Equal("http://microsoft.com/Industrial-IoT/OpcPublisher#i=1", typeId);
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

            using (var stream = new MemoryStream(buffer)) {
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
        public async Task CanSendPendingConditionsToIoTHubTest() {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(@"./PublishedNodes/PendingAlarms.json", GetAlarmCondition).ConfigureAwait(false);

            // Assert
            _output.WriteLine(messages.ToString());
            var evt = Assert.Single(messages);

            Assert.Equal(JsonValueKind.Object, evt.ValueKind);
            Assert.Equal("i=2253", evt.GetProperty("NodeId").GetString());
            Assert.Equal("PendingAlarms", evt.GetProperty("DisplayName").GetString());

            Assert.True(evt.TryGetProperty("Value", out var sev));
            Assert.True(sev.TryGetProperty("Severity", out sev));
            Assert.True(sev.TryGetProperty("Value", out sev));
            Assert.True(sev.GetInt32() >= 100);
        }

        [Fact]
        public async Task CanSendDataItemToIoTHubTestWithDeviceMethod() {
            var testInput = GetEndpointsFromFile(@"./PublishedNodes/DataItems.json");
            await StartPublisherAsync(arguments: new string[] { "--mm=FullSamples" }); // Alternative to --fm=True
            try {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(DeviceId, ModuleId, testInput[0]);
                Assert.NotNull(result);

                var messages = WaitForMessages();
                var message = Assert.Single(messages);
                Assert.Equal("ns=21;i=1259", message.GetProperty("NodeId").GetString());
                Assert.InRange(message.GetProperty("Value").GetProperty("Value").GetDouble(),
                    double.MinValue, double.MaxValue);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(DeviceId, ModuleId, e);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                result = await PublisherApi.UnpublishNodesAsync(DeviceId, ModuleId, e);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                Assert.Empty(endpoints.Endpoints);
            }
            finally {
                StopPublisher();
            }
        }

        [Fact]
        public async Task CanSendEventToIoTHubTestWithDeviceMethod() {
            var testInput = GetEndpointsFromFile(@"./PublishedNodes/SimpleEvents.json");
            await StartPublisherAsync();
            try {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(DeviceId, ModuleId, testInput[0]);
                Assert.NotNull(result);

                var messages = WaitForMessages();
                var message = Assert.Single(messages);
                Assert.Equal("i=2253", message.GetProperty("NodeId").GetString());
                Assert.NotEmpty(message.GetProperty("Value").GetProperty("EventId").GetString());

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(DeviceId, ModuleId, e);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                result = await PublisherApi.UnpublishAllNodesAsync(DeviceId, ModuleId, new PublishNodesEndpointApiModel());
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                Assert.Empty(endpoints.Endpoints);
            }
            finally {
                StopPublisher();
            }
        }

        [Fact]
        public async Task CanSendPendingConditionsToIoTHubTestWithDeviceMethod() {
            var testInput = GetEndpointsFromFile(@"./PublishedNodes/PendingAlarms.json");
            await StartPublisherAsync();
            try {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(DeviceId, ModuleId, testInput[0]);
                Assert.NotNull(result);

                var messages = WaitForMessages(GetAlarmCondition);
                _output.WriteLine(messages.ToString());

                var evt = Assert.Single(messages);
                Assert.Equal(JsonValueKind.Object, evt.ValueKind);
                Assert.Equal("i=2253", evt.GetProperty("NodeId").GetString());
                Assert.Equal("PendingAlarms", evt.GetProperty("DisplayName").GetString());

                Assert.True(evt.TryGetProperty("Value", out var sev));
                Assert.True(sev.TryGetProperty("Severity", out sev));
                Assert.True(sev.TryGetProperty("Value", out sev));
                Assert.True(sev.GetInt32() >= 100);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(DeviceId, ModuleId, e);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                result = await PublisherApi.UnpublishNodesAsync(DeviceId, ModuleId, testInput[0]);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                Assert.Empty(endpoints.Endpoints);
            }
            finally {
                StopPublisher();
            }
        }

        [Fact]
        public async Task CanSendDataItemToIoTHubTestWithDeviceMethod2() {
            var testInput1 = GetEndpointsFromFile(@"./PublishedNodes/DataItems.json");
            var testInput2 = GetEndpointsFromFile(@"./PublishedNodes/SimpleEvents.json");
            var testInput3 = GetEndpointsFromFile(@"./PublishedNodes/PendingAlarms.json");
            await StartPublisherAsync();
            try {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                Assert.Empty(endpoints.Endpoints);

                await PublisherApi.PublishNodesAsync(DeviceId, ModuleId, testInput1[0]);
                await PublisherApi.PublishNodesAsync(DeviceId, ModuleId, testInput2[0]);
                await PublisherApi.PublishNodesAsync(DeviceId, ModuleId, testInput3[0]);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                var e = Assert.Single(endpoints.Endpoints);
                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(DeviceId, ModuleId, e);
                Assert.Equal(3, nodes.OpcNodes.Count);

                await PublisherApi.UnpublishAllNodesAsync(DeviceId, ModuleId, new PublishNodesEndpointApiModel());
                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                Assert.Empty(endpoints.Endpoints);

                await PublisherApi.AddOrUpdateEndpointsAsync(DeviceId, ModuleId, new List<PublishNodesEndpointApiModel> {
                    new PublishNodesEndpointApiModel {
                        OpcNodes = nodes.OpcNodes,
                        EndpointUrl = e.EndpointUrl,
                        UseSecurity = e.UseSecurity
                    }
                });

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                e = Assert.Single(endpoints.Endpoints);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(DeviceId, ModuleId, e);
                Assert.Equal(3, nodes.OpcNodes.Count);

                await PublisherApi.UnpublishNodesAsync(DeviceId, ModuleId, testInput3[0]);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(DeviceId, ModuleId, e);
                Assert.Equal(2, nodes.OpcNodes.Count);
                await PublisherApi.UnpublishNodesAsync(DeviceId, ModuleId, testInput2[0]);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(DeviceId, ModuleId, e);
                Assert.Equal(1, nodes.OpcNodes.Count);

                var messages = WaitForMessages(GetDataFrame);
                var message = Assert.Single(messages);
                Assert.Equal("ns=21;i=1259", message.GetProperty("NodeId").GetString());
                Assert.InRange(message.GetProperty("Value").GetProperty("Value").GetDouble(),
                    double.MinValue, double.MaxValue);

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync(DeviceId, ModuleId);
                var diag = Assert.Single(diagnostics);
                Assert.Equal(e.EndpointUrl, diag.Endpoint.EndpointUrl);
            }
            finally {
                StopPublisher();
            }
        }

        [Fact]
        public async Task CanSendPendingConditionsToIoTHubTestWithDeviceMethod2() {
            var testInput = GetEndpointsFromFile(@"./PublishedNodes/PendingAlarms.json");
            await StartPublisherAsync();
            try {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(DeviceId, ModuleId, testInput[0]);
                Assert.NotNull(result);

                var messages = WaitForMessages(GetAlarmCondition);
                _output.WriteLine(messages.ToString());
                var evt = Assert.Single(messages);

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
                result = await PublisherApi.AddOrUpdateEndpointsAsync(DeviceId, ModuleId, new List<PublishNodesEndpointApiModel> {
                    testInput[0] });
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(DeviceId, ModuleId, e);
                var n = Assert.Single(nodes.OpcNodes);

                // Wait until it was applied and we receive normal events again
                messages = WaitForMessages(TimeSpan.FromMinutes(5), 1,
                    message => message.GetProperty("DisplayName").GetString() == "SimpleEvents"
                        && message.GetProperty("Value").GetProperty("ReceiveTime").ValueKind
                            == JsonValueKind.String ? message : default);
                _output.WriteLine(messages.ToString());

                var message = Assert.Single(messages);
                Assert.Equal("i=2253", message.GetProperty("NodeId").GetString());
                Assert.Equal("SimpleEvents", message.GetProperty("DisplayName").GetString());

                Assert.True(message.TryGetProperty("Value", out sev));
                Assert.True(sev.TryGetProperty("Severity", out sev));
                Assert.True(sev.GetInt32() >= 100, $"{message.ToJsonString()}");

                result = await PublisherApi.UnpublishNodesAsync(DeviceId, ModuleId, testInput[0]);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                Assert.Empty(endpoints.Endpoints);
            }
            finally {
                StopPublisher();
            }
        }

        private JsonElement GetDataFrame(JsonElement jsonElement) {
            return jsonElement.GetProperty("NodeId").GetString() != "i=2253"
                    ? jsonElement : default;
        }

        private static JsonElement GetAlarmCondition(JsonElement jsonElement) {
            return jsonElement
                .TryGetProperty("Value", out var node) && node
                .TryGetProperty("SourceNode", out node) && node
                .TryGetProperty("Value", out node) && node
                .GetString().StartsWith("http://opcfoundation.org/AlarmCondition#s=1%3a")
                    ? jsonElement : default;
        }
    }
}