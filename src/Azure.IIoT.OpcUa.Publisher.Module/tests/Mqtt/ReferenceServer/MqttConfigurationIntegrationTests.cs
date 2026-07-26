// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.ReferenceServer
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt;
    using Json.More;
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(MqttReferenceServerCollection.Name)]
    public class MqttConfigurationIntegrationTests : PublisherIntegrationTestBase, IClassFixture<ReferenceServer>
    {
        private const string kEventId = "EventId";
        private const string kOutput = "Output";
        private readonly ITestOutputHelper _output;
        private readonly ReferenceServer _fixture;

        public MqttConfigurationIntegrationTests(ReferenceServer fixture, ITestOutputHelper output)
            : base(output)
        {
            _output = output;
            _fixture = fixture;
            EndpointUrl = _fixture.EndpointUrl;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public Task CanSendDataItemToTopicConfiguredWithMethodAsync(bool useMqtt5) => ExecuteWithMqttRetryAsync(async () =>
        {
            var name = nameof(CanSendDataItemToTopicConfiguredWithMethodAsync) + (useMqtt5 ? "v5" : "v311");
            var testInput = GetEndpointsFromFile(name, "./Resources/DataItems.json");
            StartPublisher(name, arguments: ["--mm=FullNetworkMessages"],
                version: useMqtt5 ? MqttVersion.v5 : MqttVersion.v311);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0], Ct);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync();
                AssertDataItemNetworkMessage(Assert.Single(messages).Message);

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
        });

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public Task CanSendEventToTopicConfiguredWithMethodAsync(bool useMqtt5) => ExecuteWithMqttRetryAsync(async () =>
        {
            var name = nameof(CanSendEventToTopicConfiguredWithMethodAsync) + (useMqtt5 ? "v5" : "v311");
            var testInput = GetEndpointsFromFile(name, "./Resources/SimpleEvents.json");
            StartPublisher(name, arguments: ["--mm=PubSub"],
                version: useMqtt5 ? MqttVersion.v5 : MqttVersion.v311);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0], Ct);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync();
                var payload = AssertSimpleEventNetworkMessage(messages);
                Assert.NotEmpty(payload.GetProperty(kEventId).GetProperty("Value").GetString());

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e, Ct);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                _output.WriteLine("Unpublishing nodes...");
                result = await PublisherApi.UnpublishAllNodesAsync(ct: Ct);
                Assert.NotNull(result);

                _output.WriteLine("Checking endpoints...");
                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                _output.WriteLine("Stopping publisher...");
                await StopPublisherAsync();
            }
        });

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public Task CanSendPendingConditionsToTopicConfiguredWithMethodAsync(bool useMqtt5) => ExecuteWithMqttRetryAsync(async () =>
        {
            var name = nameof(CanSendPendingConditionsToTopicConfiguredWithMethodAsync) + (useMqtt5 ? "v5" : "v311");
            var testInput = GetEndpointsFromFile(name, "./Resources/PendingAlarms.json");
            StartPublisher(name, arguments: ["--mm=PubSub"],
                version: useMqtt5 ? MqttVersion.v5 : MqttVersion.v311);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0], Ct);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync(GetAlarmCondition);
                messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));

                AssertPendingAlarmDataSetMessage(Assert.Single(messages).Message);

                _output.WriteLine("GetConfigured 1");
                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e, Ct);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput[0].OpcNodes[0].Id, n.Id);

                _output.WriteLine("Unpublish");
                result = await PublisherApi.UnpublishNodesAsync(testInput[0], Ct);
                Assert.NotNull(result);

                _output.WriteLine("GetConfigured 2");
                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                await StopPublisherAsync();
            }
        });

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public Task CanSendDataItemToTopicConfiguredWithMethod2Async(bool useMqtt5) => ExecuteWithMqttRetryAsync(async () =>
        {
            var name = nameof(CanSendDataItemToTopicConfiguredWithMethod2Async) + (useMqtt5 ? "v5" : "v311");
            var testInput1 = GetEndpointsFromFile(name, "./Resources/DataItems.json");
            var testInput2 = GetEndpointsFromFile(name, "./Resources/SimpleEvents.json");
            var testInput3 = GetEndpointsFromFile(name, "./Resources/PendingAlarms.json");
            StartPublisher(name, arguments: ["--mm=PubSub"],
                version: useMqtt5 ? MqttVersion.v5 : MqttVersion.v311);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);

                _output.WriteLine("Publishing 1");
                await PublisherApi.PublishNodesAsync(testInput1[0], Ct);
                _output.WriteLine("Publishing 2");
                await PublisherApi.PublishNodesAsync(testInput2[0], Ct);
                _output.WriteLine("Publishing 3");
                await PublisherApi.PublishNodesAsync(testInput3[0], Ct);

                _output.WriteLine("Checking endpoints...");
                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                var e = Assert.Single(endpoints.Endpoints);
                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e, Ct);
                Assert.Equal(3, nodes.OpcNodes.Count);

                _output.WriteLine("Unpublishing all...");
                await PublisherApi.UnpublishAllNodesAsync(ct: Ct);
                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);

                _output.WriteLine("Re-adding with AddOrUpdate...");
                await PublisherApi.AddOrUpdateEndpointsAsync(new List<PublishedNodesEntryModel>
                {
                    new() {
                        OpcNodes = [.. nodes.OpcNodes],
                        EndpointUrl = e.EndpointUrl,
                        UseSecurity = e.UseSecurity,
                        DataSetWriterGroup = name
                    }
                }, Ct);

                _output.WriteLine("Checking endpoints...");
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
                AssertDataItemDataSetMessage(Assert.Single(messages).Message);

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync(Ct);
                var diag = Assert.Single(diagnostics);
                Assert.Equal(e.EndpointUrl, diag.Endpoint.EndpointUrl);
            }
            finally
            {
                await StopPublisherAsync();
            }
        });

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public Task CanSendPendingConditionsToTopicConfiguredWithMethod2Async(bool useMqtt5) => ExecuteWithMqttRetryAsync(async () =>
        {
            var name = nameof(CanSendPendingConditionsToTopicConfiguredWithMethod2Async) + (useMqtt5 ? "v5" : "v311");
            var testInput = GetEndpointsFromFile(name, "./Resources/PendingAlarms.json");

            StartPublisher(name, arguments: ["--mm=PubSub"],
                version: useMqtt5 ? MqttVersion.v5 : MqttVersion.v311);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput[0], Ct);
                Assert.NotNull(result);

                var messages = await WaitForMessagesAsync(GetAlarmCondition);
                messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));
                AssertPendingAlarmDataSetMessage(Assert.Single(messages).Message);

                // Disable pending alarms
                testInput[0].OpcNodes[0].ConditionHandling = null;
                testInput[0].OpcNodes[0].DisplayName = "SimpleEvents";
                result = await PublisherApi.AddOrUpdateEndpointsAsync(
                    new List<PublishedNodesEntryModel> { testInput[0] }, Ct);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e, Ct);
                Assert.Single(nodes.OpcNodes);

                // Wait until it was applied and we receive normal events again
                messages = await WaitForMessagesAsync(GetSimpleEvent);
                messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));

                var message = Assert.Single(messages).Message;
                var payload = message.GetProperty("Payload");
                if (message.TryGetProperty("DataSetWriterName", out var writerName))
                {
                    Assert.True(writerName.GetString()?.EndsWith("|SimpleEvents", StringComparison.Ordinal),
                        $"{message.ToJsonString()}");
                }

                Assert.True(payload.TryGetProperty("Severity", out var sev));
                Assert.True(sev.GetProperty("Value").GetInt32() > 0, $"{message.ToJsonString()}");

                result = await PublisherApi.UnpublishNodesAsync(testInput[0], Ct);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync(ct: Ct);
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                await StopPublisherAsync();
            }
        });

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
            var dataSetMessage = message.GetProperty("Messages")[0];
            if (dataSetMessage.TryGetProperty("DataSetWriterName", out var writerName))
            {
                Assert.True(writerName.GetString()?.EndsWith("|SimpleEvents", StringComparison.Ordinal),
                    $"{message.ToJsonString()}");
            }
            return dataSetMessage.GetProperty("Payload");
        }

        private static void AssertPendingAlarmDataSetMessage(JsonElement message)
        {
            Assert.Equal(JsonValueKind.Object, message.ValueKind);
            var payload = message.GetProperty("Payload");
            Assert.True(payload.GetProperty("SourceNode").ValueKind != JsonValueKind.Null);
            Assert.True(payload.GetProperty("Severity").GetProperty("Value").GetInt32() >= 0);
            if (message.TryGetProperty("DataSetWriterName", out var writerName))
            {
                Assert.True(writerName.GetString()?.EndsWith("|PendingAlarms", StringComparison.Ordinal),
                    $"{message.ToJsonString()}");
            }
        }

        private static JsonElement GetDataFrame(JsonElement jsonElement)
        {
            if (!jsonElement.TryGetProperty("Messages", out var messages) ||
                messages.ValueKind != JsonValueKind.Array)
            {
                return default;
            }

            foreach (var element in messages.EnumerateArray())
            {
                if (element.TryGetProperty("Payload", out var payload) &&
                    payload.TryGetProperty(kOutput, out _))
                {
                    return element;
                }
            }
            return default;
        }

        private static JsonElement GetAlarmCondition(JsonElement jsonElement)
        {
            if (!jsonElement.TryGetProperty("Messages", out var messages) ||
                messages.ValueKind != JsonValueKind.Array)
            {
                return default;
            }

            foreach (var element in messages.EnumerateArray())
            {
                if (element.GetProperty("MessageType").GetString() != "ua-condition" ||
                    !element.GetProperty("Payload").TryGetProperty("SourceNode", out var node))
                {
                    continue;
                }
                if (node.ValueKind == JsonValueKind.Object &&
                    node.TryGetProperty("Value", out var value))
                {
                    node = value;
                }
                if (node.ValueKind != JsonValueKind.Null)
                {
                    return element;
                }
            }
            return default;
        }

        private static JsonElement GetSimpleEvent(JsonElement jsonElement)
        {
            if (!jsonElement.TryGetProperty("Messages", out var messages) ||
                messages.ValueKind != JsonValueKind.Array)
            {
                return default;
            }

            foreach (var element in messages.EnumerateArray())
            {
                if (element.TryGetProperty("Payload", out var payload) &&
                    payload.TryGetProperty("ReceiveTime", out var receiveTime) &&
                    receiveTime.GetProperty("Value").ValueKind == JsonValueKind.String)
                {
                    return element;
                }
            }
            return default;
        }
    }
}
