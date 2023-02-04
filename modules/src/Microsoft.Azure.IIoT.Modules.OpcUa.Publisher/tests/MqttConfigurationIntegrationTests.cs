// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using System;
    using System.Collections.Generic;
    using Json.More;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Currently, we create new independent instances of server, publisher and mocked IoT services for each test,
    /// this could be optimised e.g. create only single instance of server and publisher between tests in the same class.
    /// </summary>
    [Collection(ReadCollection.Name)]
    public class MqttConfigurationIntegrationTests : PublisherMqttIntegrationTestBase {
        private readonly ITestOutputHelper _output;

        public MqttConfigurationIntegrationTests(ReferenceServerFixture fixture, ITestOutputHelper output) : base(fixture) {
            _output = output;
        }

#if DEBUG
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendDataItemToTopicConfiguredWithMethod(bool useMqtt5) {
            var testInput = GetEndpointsFromFile(@"./PublishedNodes/DataItems.json");
            await StartPublisherAsync(useMqtt5, arguments: new string[] { "--mm=FullSamples" }); // Alternative to --fm=True
            try {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(null, null, testInput[0]);
                Assert.NotNull(result);

                var messages = WaitForMessages();
                var message = Assert.Single(messages);
                Assert.Equal("ns=21;i=1259", message.Message.GetProperty("NodeId").GetString());
                Assert.InRange(message.Message.GetProperty("Value").GetProperty("Value").GetDouble(),
                    double.MinValue, double.MaxValue);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(null, null, e);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                result = await PublisherApi.UnpublishNodesAsync(null, null, e);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
                Assert.Empty(endpoints.Endpoints);
            }
            finally {
                StopPublisher();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendEventToTopicConfiguredWithMethod(bool useMqtt5) {
            var testInput = GetEndpointsFromFile(@"./PublishedNodes/SimpleEvents.json");
            await StartPublisherAsync(useMqtt5);
            try {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(null, null, testInput[0]);
                Assert.NotNull(result);

                var messages = WaitForMessages();
                var message = Assert.Single(messages);
                Assert.Equal("i=2253", message.Message.GetProperty("NodeId").GetString());
                Assert.NotEmpty(message.Message.GetProperty("Value").GetProperty("EventId").GetString());

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(null, null, e);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                result = await PublisherApi.UnpublishAllNodesAsync(null, null, new PublishNodesEndpointApiModel());
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
                Assert.Empty(endpoints.Endpoints);
            }
            finally {
                StopPublisher();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendPendingConditionsToTopicConfiguredWithMethod(bool useMqtt5) {
            var testInput = GetEndpointsFromFile(@"./PublishedNodes/PendingAlarms.json");
            await StartPublisherAsync(useMqtt5);
            try {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(null, null, testInput[0]);
                Assert.NotNull(result);

                var messages = WaitForMessages(GetAlarmCondition);
                _output.WriteLine(messages.ToString());

                var evt = Assert.Single(messages).Message;
                Assert.Equal(JsonValueKind.Object, evt.ValueKind);
                Assert.Equal("i=2253", evt.GetProperty("NodeId").GetString());
                Assert.Equal("PendingAlarms", evt.GetProperty("DisplayName").GetString());

                Assert.True(evt.TryGetProperty("Value", out var sev));
                Assert.True(sev.TryGetProperty("Severity", out sev));
                Assert.True(sev.TryGetProperty("Value", out sev));
                Assert.True(sev.GetInt32() >= 100);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(null, null, e);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                result = await PublisherApi.UnpublishNodesAsync(null, null, testInput[0]);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
                Assert.Empty(endpoints.Endpoints);
            }
            finally {
                StopPublisher();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendDataItemToTopicConfiguredWithMethod2(bool useMqtt5) {
            var testInput1 = GetEndpointsFromFile(@"./PublishedNodes/DataItems.json");
            var testInput2 = GetEndpointsFromFile(@"./PublishedNodes/SimpleEvents.json");
            var testInput3 = GetEndpointsFromFile(@"./PublishedNodes/PendingAlarms.json");
            await StartPublisherAsync(useMqtt5);
            try {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
                Assert.Empty(endpoints.Endpoints);

                await PublisherApi.PublishNodesAsync(null, null, testInput1[0]);
                await PublisherApi.PublishNodesAsync(null, null, testInput2[0]);
                await PublisherApi.PublishNodesAsync(null, null, testInput3[0]);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
                var e = Assert.Single(endpoints.Endpoints);
                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(null, null, e);
                Assert.Equal(3, nodes.OpcNodes.Count);

                await PublisherApi.UnpublishAllNodesAsync(null, null, new PublishNodesEndpointApiModel());
                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
                Assert.Empty(endpoints.Endpoints);

                await PublisherApi.AddOrUpdateEndpointsAsync(null, null, new List<PublishNodesEndpointApiModel> {
                    new PublishNodesEndpointApiModel {
                        OpcNodes = nodes.OpcNodes,
                        EndpointUrl = e.EndpointUrl,
                        UseSecurity = e.UseSecurity
                    }
                });

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
                e = Assert.Single(endpoints.Endpoints);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(null, null, e);
                Assert.Equal(3, nodes.OpcNodes.Count);

                await PublisherApi.UnpublishNodesAsync(null, null, testInput3[0]);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(null, null, e);
                Assert.Equal(2, nodes.OpcNodes.Count);
                await PublisherApi.UnpublishNodesAsync(null, null, testInput2[0]);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(null, null, e);
                Assert.Equal(1, nodes.OpcNodes.Count);

                var messages = WaitForMessages(GetDataFrame);
                var message = Assert.Single(messages);
                Assert.Equal("ns=21;i=1259", message.Message.GetProperty("NodeId").GetString());
                Assert.InRange(message.Message.GetProperty("Value").GetProperty("Value").GetDouble(),
                    double.MinValue, double.MaxValue);

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync(null, null);
                var diag = Assert.Single(diagnostics);
                Assert.Equal(e.EndpointUrl, diag.Endpoint.EndpointUrl);
            }
            finally {
                StopPublisher();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendPendingConditionsToTopicConfiguredWithMethod2(bool useMqtt5) {
            var testInput = GetEndpointsFromFile(@"./PublishedNodes/PendingAlarms.json");
            await StartPublisherAsync(useMqtt5);
            try {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(null, null, testInput[0]);
                Assert.NotNull(result);

                var messages = WaitForMessages(GetAlarmCondition);
                _output.WriteLine(messages.ToString());
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
                result = await PublisherApi.AddOrUpdateEndpointsAsync(null, null, new List<PublishNodesEndpointApiModel> {
                    testInput[0] });
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(null, null, e);
                var n = Assert.Single(nodes.OpcNodes);

                // Wait until it was applied and we receive normal events again
                messages = WaitForMessages(TimeSpan.FromMinutes(5), 1,
                    message => message.GetProperty("DisplayName").GetString() == "SimpleEvents"
                        && message.GetProperty("Value").GetProperty("ReceiveTime").ValueKind
                            == JsonValueKind.String ? message : default);
                _output.WriteLine(messages.ToString());

                var message = Assert.Single(messages);
                Assert.Equal("i=2253", message.Message.GetProperty("NodeId").GetString());
                Assert.Equal("SimpleEvents", message.Message.GetProperty("DisplayName").GetString());

                Assert.True(message.Message.TryGetProperty("Value", out sev));
                Assert.True(sev.TryGetProperty("Severity", out sev));
                Assert.True(sev.GetInt32() >= 100, $"{message.Message.ToJsonString()}");

                result = await PublisherApi.UnpublishNodesAsync(null, null, testInput[0]);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(null, null);
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
#endif
    }
}