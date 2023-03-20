// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Furly.Extensions.Mqtt;
    using Json.More;
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Currently, we create new independent instances of server, publisher and services for each test,
    /// this could be optimised e.g. create only single instance of server and publisher between
    /// tests in the same class.
    /// </summary>
    [Collection(ReferenceServerReadCollection.Name)]
    public class MqttConfigurationIntegrationTests : PublisherIntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public MqttConfigurationIntegrationTests(ReferenceServer fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendDataItemToTopicConfiguredWithMethod(bool useMqtt5)
        {
            var testInput = GetEndpointsFromFile("./Resources/DataItems.json");
            StartPublisher(arguments: new string[] { "--mm=FullSamples" }, // Alternative to --fm=True
                version: useMqtt5 ? MqttVersion.v5 : MqttVersion.v311);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0]).ConfigureAwait(false);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync().ConfigureAwait(false);
                var message = Assert.Single(messages);
                Assert.Equal("ns=23;i=1259", message.Message.GetProperty("NodeId").GetString());
                Assert.InRange(message.Message.GetProperty("Value").GetProperty("Value").GetDouble(),
                    double.MinValue, double.MaxValue);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e).ConfigureAwait(false);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                result = await PublisherApi.UnpublishNodesAsync(e).ConfigureAwait(false);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                StopPublisher();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendEventToTopicConfiguredWithMethod(bool useMqtt5)
        {
            var testInput = GetEndpointsFromFile("./Resources/SimpleEvents.json");
            StartPublisher(version: useMqtt5 ? MqttVersion.v5 : MqttVersion.v311);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0]).ConfigureAwait(false);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync().ConfigureAwait(false);
                var message = Assert.Single(messages);
                Assert.Equal("i=2253", message.Message.GetProperty("NodeId").GetString());
                Assert.NotEmpty(message.Message.GetProperty("Value").GetProperty("EventId").GetString());

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e).ConfigureAwait(false);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                result = await PublisherApi.UnpublishAllNodesAsync(new PublishedNodesEntryModel()).ConfigureAwait(false);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                StopPublisher();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendPendingConditionsToTopicConfiguredWithMethod(bool useMqtt5)
        {
            var testInput = GetEndpointsFromFile("./Resources/PendingAlarms.json");
            StartPublisher(version: useMqtt5 ? MqttVersion.v5 : MqttVersion.v311);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0]).ConfigureAwait(false);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync(GetAlarmCondition).ConfigureAwait(false);
                _output.WriteLine(messages.ToString());

                var evt = Assert.Single(messages).Message;
                Assert.Equal(JsonValueKind.Object, evt.ValueKind);
                Assert.Equal("i=2253", evt.GetProperty("NodeId").GetString());
                Assert.Equal("PendingAlarms", evt.GetProperty("DisplayName").GetString());

                Assert.True(evt.TryGetProperty("Value", out var sev));
                Assert.True(sev.TryGetProperty("Severity", out sev));
                Assert.True(sev.TryGetProperty("Value", out sev));
                Assert.True(sev.GetInt32() >= 100);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e).ConfigureAwait(false);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                result = await PublisherApi.UnpublishNodesAsync(testInput[0]).ConfigureAwait(false);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                StopPublisher();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendDataItemToTopicConfiguredWithMethod2(bool useMqtt5)
        {
            var testInput1 = GetEndpointsFromFile("./Resources/DataItems.json");
            var testInput2 = GetEndpointsFromFile("./Resources/SimpleEvents.json");
            var testInput3 = GetEndpointsFromFile("./Resources/PendingAlarms.json");
            StartPublisher(version: useMqtt5 ? MqttVersion.v5 : MqttVersion.v311);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                Assert.Empty(endpoints.Endpoints);

                await PublisherApi.PublishNodesAsync(testInput1[0]).ConfigureAwait(false);
                await PublisherApi.PublishNodesAsync(testInput2[0]).ConfigureAwait(false);
                await PublisherApi.PublishNodesAsync(testInput3[0]).ConfigureAwait(false);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                var e = Assert.Single(endpoints.Endpoints);
                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e).ConfigureAwait(false);
                Assert.Equal(3, nodes.OpcNodes.Count);

                await PublisherApi.UnpublishAllNodesAsync(new PublishedNodesEntryModel()).ConfigureAwait(false);
                endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                Assert.Empty(endpoints.Endpoints);

                await PublisherApi.AddOrUpdateEndpointsAsync(new List<PublishedNodesEntryModel> {
                    new PublishedNodesEntryModel {
                        OpcNodes = nodes.OpcNodes,
                        EndpointUrl = e.EndpointUrl,
                        UseSecurity = e.UseSecurity
                    }
                }).ConfigureAwait(false);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                e = Assert.Single(endpoints.Endpoints);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e).ConfigureAwait(false);
                Assert.Equal(3, nodes.OpcNodes.Count);

                await PublisherApi.UnpublishNodesAsync(testInput3[0]).ConfigureAwait(false);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e).ConfigureAwait(false);
                Assert.Equal(2, nodes.OpcNodes.Count);
                await PublisherApi.UnpublishNodesAsync(testInput2[0]).ConfigureAwait(false);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e).ConfigureAwait(false);
                Assert.Single(nodes.OpcNodes);

                var messages = await WaitForMessagesAsync(GetDataFrame).ConfigureAwait(false);
                var message = Assert.Single(messages);
                Assert.Equal("ns=23;i=1259", message.Message.GetProperty("NodeId").GetString());
                Assert.InRange(message.Message.GetProperty("Value").GetProperty("Value").GetDouble(),
                    double.MinValue, double.MaxValue);

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync().ConfigureAwait(false);
                var diag = Assert.Single(diagnostics);
                Assert.Equal(e.EndpointUrl, diag.Endpoint.EndpointUrl);
            }
            finally
            {
                StopPublisher();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendPendingConditionsToTopicConfiguredWithMethod2(bool useMqtt5)
        {
            var testInput = GetEndpointsFromFile("./Resources/PendingAlarms.json");
            StartPublisher(version: useMqtt5 ? MqttVersion.v5 : MqttVersion.v311);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0]).ConfigureAwait(false);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync(GetAlarmCondition).ConfigureAwait(false);
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
                result = await PublisherApi.AddOrUpdateEndpointsAsync(new List<PublishedNodesEntryModel> {
                    testInput[0] }).ConfigureAwait(false);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e).ConfigureAwait(false);
                var n = Assert.Single(nodes.OpcNodes);

                // Wait until it was applied and we receive normal events again
                messages = await WaitForMessagesAsync(TimeSpan.FromMinutes(5), 1,
                    message => message.GetProperty("DisplayName").GetString() == "SimpleEvents"
                        && message.GetProperty("Value").GetProperty("ReceiveTime").ValueKind
                            == JsonValueKind.String ? message : default).ConfigureAwait(false);
                _output.WriteLine(messages.ToString());

                var message = Assert.Single(messages);
                Assert.Equal("i=2253", message.Message.GetProperty("NodeId").GetString());
                Assert.Equal("SimpleEvents", message.Message.GetProperty("DisplayName").GetString());

                Assert.True(message.Message.TryGetProperty("Value", out sev));
                Assert.True(sev.TryGetProperty("Severity", out sev));
                Assert.True(sev.GetInt32() >= 100, $"{message.Message.ToJsonString()}");

                result = await PublisherApi.UnpublishNodesAsync(testInput[0]).ConfigureAwait(false);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                StopPublisher();
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
