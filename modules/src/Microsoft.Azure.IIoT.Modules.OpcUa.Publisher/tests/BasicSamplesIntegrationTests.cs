// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using System;
    using System.Collections.Generic;
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
    public class BasicSamplesIntegrationTests : PublisherIntegrationTestBase {
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
            Assert.Single(messages);
            Assert.Equal("ns=21;i=1259", messages[0].RootElement[0].GetProperty("NodeId").GetString());
            Assert.InRange(messages[0].RootElement[0].GetProperty("Value").GetProperty("Value").GetDouble(),
                double.MinValue, double.MaxValue);
        }

        [Fact]
        public async Task CanSendEventToIoTHubTest() {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(@"./PublishedNodes/SimpleEvents.json").ConfigureAwait(false);

            // Assert
            var message = Assert.Single(messages).RootElement[0];
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
                arguments: new string[] {"--fm=True"}).ConfigureAwait(false);

            // Assert
            var message = Assert.Single(messages).RootElement[0];
            Assert.Equal("i=2253", message.GetProperty("NodeId").GetString());
            Assert.NotEmpty(message.GetProperty("ApplicationUri").GetString());
            Assert.NotEmpty(message.GetProperty("Timestamp").GetString());
            Assert.True(message.GetProperty("SequenceNumber").GetUInt32() > 0);
            Assert.NotEmpty(message.GetProperty("Value").GetProperty("EventId").GetString());
        }

        [Fact]
        public async Task CanSendPendingAlarmsToIoTHubTest() {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(@"./PublishedNodes/PendingAlarms.json", WithPendingAlarms).ConfigureAwait(false);

            // Assert
            var message = Assert.Single(messages).RootElement[0];

            _output.WriteLine(message.ToString());
            Assert.Equal("i=2253", message.GetProperty("NodeId").GetString());
            Assert.Equal("PendingAlarms", message.GetProperty("DisplayName").GetString());

            var evt = GetAlarmCondition(message.GetProperty("Value"));
            Assert.NotEqual(JsonValueKind.Null, evt.ValueKind);
            Assert.True(evt.GetProperty("Severity").GetInt32() >= 100);
        }

        [Fact]
        public async Task CanSendDataItemToIoTHubTestWithDeviceMethod() {
            var testInput = GetEndpointsFromFile(@"./PublishedNodes/DataItems.json");
            await StartPublisherAsync(arguments: new string[] { "--fm=True" });
            try {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(DeviceId, ModuleId, testInput[0]);
                Assert.NotNull(result);

                var messages = WaitForMessages();
                Assert.Single(messages);
                Assert.Equal("ns=21;i=1259", messages[0].RootElement[0].GetProperty("NodeId").GetString());
                Assert.InRange(messages[0].RootElement[0].GetProperty("Value").GetProperty("Value").GetDouble(),
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
                Assert.Single(messages);
                Assert.Equal("i=2253", messages[0].RootElement[0].GetProperty("NodeId").GetString());
                Assert.NotEmpty(messages[0].RootElement[0].GetProperty("Value").GetProperty("EventId").GetString());

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
        public async Task CanSendPendingAlarmsToIoTHubTestWithDeviceMethod() {
            var testInput = GetEndpointsFromFile(@"./PublishedNodes/PendingAlarms.json");
            await StartPublisherAsync();
            try {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(DeviceId, ModuleId, testInput[0]);
                Assert.NotNull(result);

                var messages = WaitForMessages(WithPendingAlarms);
                var message = Assert.Single(messages).RootElement[0];

                _output.WriteLine(message.ToString());
                Assert.Equal("i=2253", message.GetProperty("NodeId").GetString());
                Assert.Equal("PendingAlarms", message.GetProperty("DisplayName").GetString());

                var evt = GetAlarmCondition(message.GetProperty("Value"));
                Assert.NotEqual(JsonValueKind.Null, evt.ValueKind);
                Assert.True(evt.GetProperty("Severity").GetInt32() >= 100);

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

                var messages = WaitForMessages();
                Assert.Single(messages);
                Assert.Equal("ns=21;i=1259", messages[0].RootElement[0].GetProperty("NodeId").GetString());
                Assert.InRange(messages[0].RootElement[0].GetProperty("Value").GetProperty("Value").GetDouble(),
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
        public async Task CanSendPendingAlarmsToIoTHubTestWithDeviceMethod2() {
            var testInput = GetEndpointsFromFile(@"./PublishedNodes/PendingAlarms.json");
            await StartPublisherAsync();
            try {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(DeviceId, ModuleId, testInput[0]);
                Assert.NotNull(result);

                var messages = WaitForMessages(WithPendingAlarms);
                var message = Assert.Single(messages).RootElement[0];

                _output.WriteLine(message.ToString());
                Assert.Equal("i=2253", message.GetProperty("NodeId").GetString());
                Assert.Equal("PendingAlarms", message.GetProperty("DisplayName").GetString());

                var evt = GetAlarmCondition(message.GetProperty("Value"));
                Assert.NotEqual(JsonValueKind.Null, evt.ValueKind);
                Assert.True(evt.GetProperty("Severity").GetInt32() >= 100);

                // Disable pending alarms
                testInput[0].OpcNodes[0].EventFilter.PendingAlarms.IsEnabled = false;
                result = await PublisherApi.AddOrUpdateEndpointsAsync(DeviceId, ModuleId, new List<PublishNodesEndpointApiModel> {
                    testInput[0] });
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(DeviceId, ModuleId, e);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.False(n.EventFilter.PendingAlarms.IsEnabled);

                // Wait until it was applied and we receive normal events again
                messages = WaitForMessages(TimeSpan.FromMinutes(5), 1, WithRegularEvent);
                message = Assert.Single(messages).RootElement[0];
                Assert.Equal("i=2253", message.GetProperty("NodeId").GetString());
                Assert.Equal("PendingAlarms", message.GetProperty("DisplayName").GetString());

                evt = message.GetProperty("Value");
                Assert.True(evt.GetProperty("Severity").GetInt32() >= 100);

                result = await PublisherApi.UnpublishNodesAsync(DeviceId, ModuleId, testInput[0]);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(DeviceId, ModuleId);
                Assert.Empty(endpoints.Endpoints);
            }
            finally {
                StopPublisher();
            }
        }

        private static JsonElement GetAlarmCondition(JsonElement jsonElement) {
            Assert.Equal(JsonValueKind.Array, jsonElement.ValueKind);
            return jsonElement.EnumerateArray().FirstOrDefault(element =>
                element.TryGetProperty("SourceNode", out var node) &&
                    node.GetString().StartsWith("http://opcfoundation.org/AlarmCondition#s=1%3a"));
        }

        private static bool WithPendingAlarms(JsonDocument message) {
            if (WithRegularEvent(message)) {
                return false;
            }
            var value = message.RootElement[0].GetProperty("Value");
            return value.GetArrayLength() > 0;
        }

        private static bool WithRegularEvent(JsonDocument message) {
            var value = message.RootElement[0].GetProperty("Value");
            return value.ValueKind != JsonValueKind.Array;
        }
    }
}