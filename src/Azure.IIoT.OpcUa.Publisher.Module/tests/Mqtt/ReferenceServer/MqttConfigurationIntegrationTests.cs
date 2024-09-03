// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.ReferenceServer
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Furly.Extensions.Mqtt;
    using Json.More;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class MqttConfigurationIntegrationTests : PublisherIntegrationTestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly ReferenceServer _fixture;

        public MqttConfigurationIntegrationTests(ITestOutputHelper output)
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendDataItemToTopicConfiguredWithMethod(bool useMqtt5)
        {
            var name = nameof(CanSendDataItemToTopicConfiguredWithMethod) + (useMqtt5 ? "v5" : "v311");
            var testInput = GetEndpointsFromFile(name, "./Resources/DataItems.json");
            StartPublisher(name, arguments: new string[] { "--mm=FullSamples" }, // Alternative to --fm=True
                version: useMqtt5 ? MqttVersion.v5 : MqttVersion.v311);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0], Ct);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync();
                var message = Assert.Single(messages);
                Assert.Equal("ns=23;i=1259", message.Message.GetProperty("NodeId").GetString());
                Assert.InRange(message.Message.GetProperty("Value").GetProperty("Value").GetDouble(),
                    double.MinValue, double.MaxValue);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e, Ct);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                result = await PublisherApi.UnpublishNodesAsync(e, Ct);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                await StopPublisherAsync();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendEventToTopicConfiguredWithMethod(bool useMqtt5)
        {
            var name = nameof(CanSendEventToTopicConfiguredWithMethod) + (useMqtt5 ? "v5" : "v311");
            var testInput = GetEndpointsFromFile(name, "./Resources/SimpleEvents.json");
            StartPublisher(name, version: useMqtt5 ? MqttVersion.v5 : MqttVersion.v311);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0], Ct);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync();
                var message = Assert.Single(messages);
                Assert.Equal("i=2253", message.Message.GetProperty("NodeId").GetString());
                Assert.NotEmpty(message.Message.GetProperty("Value").GetProperty("EventId").GetString());

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e, Ct);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                result = await PublisherApi.UnpublishAllNodesAsync(ct: Ct);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                await StopPublisherAsync();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendPendingConditionsToTopicConfiguredWithMethod(bool useMqtt5)
        {
            var name = nameof(CanSendPendingConditionsToTopicConfiguredWithMethod) + (useMqtt5 ? "v5" : "v311");
            var testInput = GetEndpointsFromFile(name, "./Resources/PendingAlarms.json");
            StartPublisher(name, version: useMqtt5 ? MqttVersion.v5 : MqttVersion.v311);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0], Ct);
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

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e, Ct);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                result = await PublisherApi.UnpublishNodesAsync(testInput[0], Ct);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                await StopPublisherAsync();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendDataItemToTopicConfiguredWithMethod2(bool useMqtt5)
        {
            var name = nameof(CanSendDataItemToTopicConfiguredWithMethod2) + (useMqtt5 ? "v5" : "v311");
            var testInput1 = GetEndpointsFromFile(name, "./Resources/DataItems.json");
            var testInput2 = GetEndpointsFromFile(name, "./Resources/SimpleEvents.json");
            var testInput3 = GetEndpointsFromFile(name, "./Resources/PendingAlarms.json");
            StartPublisher(name, version: useMqtt5 ? MqttVersion.v5 : MqttVersion.v311);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);

                await PublisherApi.PublishNodesAsync(testInput1[0], Ct);
                await PublisherApi.PublishNodesAsync(testInput2[0], Ct);
                await PublisherApi.PublishNodesAsync(testInput3[0], Ct);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                var e = Assert.Single(endpoints.Endpoints);
                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e, Ct);
                Assert.Equal(3, nodes.OpcNodes.Count);

                await PublisherApi.UnpublishAllNodesAsync(ct: Ct);
                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);

                await PublisherApi.AddOrUpdateEndpointsAsync(new List<PublishedNodesEntryModel>
                {
                    new() {
                        OpcNodes = nodes.OpcNodes.ToList(),
                        EndpointUrl = e.EndpointUrl,
                        UseSecurity = e.UseSecurity,
                        DataSetWriterGroup = name
                    }
                }, Ct);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                e = Assert.Single(endpoints.Endpoints);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e, Ct);
                Assert.Equal(3, nodes.OpcNodes.Count);

                _output.WriteLine("Removing items...");
                await PublisherApi.UnpublishNodesAsync(testInput3[0], Ct);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e, Ct);
                Assert.Equal(2, nodes.OpcNodes.Count);
                await PublisherApi.UnpublishNodesAsync(testInput2[0], Ct);
                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e, Ct);
                Assert.Single(nodes.OpcNodes);

                _output.WriteLine("Waiting for remaining...");
                var messages = await WaitForMessagesAsync(GetDataFrame);
                var message = Assert.Single(messages);
                Assert.Equal("ns=23;i=1259", message.Message.GetProperty("NodeId").GetString());
                Assert.InRange(message.Message.GetProperty("Value").GetProperty("Value").GetDouble(),
                    double.MinValue, double.MaxValue);

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync(Ct);
                var diag = Assert.Single(diagnostics);
                Assert.Equal(e.EndpointUrl, diag.Endpoint.EndpointUrl);
            }
            finally
            {
                await StopPublisherAsync();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanSendPendingConditionsToTopicConfiguredWithMethod2(bool useMqtt5)
        {
            var name = nameof(CanSendPendingConditionsToTopicConfiguredWithMethod2) + (useMqtt5 ? "v5" : "v311");
            var testInput = GetEndpointsFromFile(name, "./Resources/PendingAlarms.json");

            StartPublisher(name, version: useMqtt5 ? MqttVersion.v5 : MqttVersion.v311);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0], Ct);
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
                result = await PublisherApi.AddOrUpdateEndpointsAsync(
                    new List<PublishedNodesEntryModel> { testInput[0] }, Ct);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e, Ct);
                var n = Assert.Single(nodes.OpcNodes);

                // TODO: Fix
                if (n != null) return;

                // Wait until it was applied and we receive normal events again
                messages = await WaitForMessagesAsync(
                    message => message.GetProperty("DisplayName").GetString() == "SimpleEvents"
                        && message.GetProperty("Value").GetProperty("ReceiveTime").ValueKind
                            == JsonValueKind.String ? message : default);
                messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));

                var message = Assert.Single(messages);
                Assert.Equal("i=2253", message.Message.GetProperty("NodeId").GetString());
                Assert.Equal("SimpleEvents", message.Message.GetProperty("DisplayName").GetString());

                Assert.True(message.Message.TryGetProperty("Value", out sev));
                Assert.True(sev.TryGetProperty("Severity", out sev));
                Assert.True(sev.GetInt32() > 0, $"{message.Message.ToJsonString()}");

                result = await PublisherApi.UnpublishNodesAsync(testInput[0], Ct);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
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
