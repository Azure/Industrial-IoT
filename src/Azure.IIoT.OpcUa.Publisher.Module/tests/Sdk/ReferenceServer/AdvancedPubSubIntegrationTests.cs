// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.ReferenceServer
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Json.More;
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class AdvancedPubSubIntegrationTests : PublisherIntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public AdvancedPubSubIntegrationTests(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        [Fact]
        public async Task RestartServerTestAsync()
        {
            var server = new ReferenceServer();
            EndpointUrl = server.EndpointUrl;
            const string name = nameof(RestartServerTestAsync);
            StartPublisher(name, "./Resources/Fixedvalue.json",
                arguments: ["--mm=PubSub", "--dm=false"], keepAliveInterval: 1);
            try
            {
                // Arrange
                // Act
                var (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data");

                // Assert
                var message = Assert.Single(messages).Message;
                AssertFixedValueMessage(message);
                Assert.NotNull(metadata);

                await server.RestartAsync(WaitUntilDisconnectedAsync);
                _output.WriteLine("Restarted server");

                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data");

                message = Assert.Single(messages).Message;
                AssertFixedValueMessage(message);
                Assert.Null(metadata);
            }
            finally
            {
                server.Dispose();
                await StopPublisherAsync();
            }
        }

        [Fact]
        public async Task RestartServerWithHeartbeatTestAsync()
        {
            var server = new ReferenceServer();
            EndpointUrl = server.EndpointUrl;
            const string name = nameof(RestartServerWithHeartbeatTestAsync);
            StartPublisher(name, "./Resources/Heartbeat2.json",
                arguments: ["--mm=PubSub", "--dm=false", "--bs=1"], keepAliveInterval: 1);
            try
            {
                // Arrange
                // Act
                var (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data");

                // Assert
                var message = Assert.Single(messages).Message;
                Assert.NotNull(metadata);

                await server.RestartAsync(WaitUntilDisconnectedAsync);
                _output.WriteLine("Restarted server");

                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromSeconds(10), 1000,
                    messageType: "ua-data");
                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromSeconds(10), 1,
                    messageType: "ua-data");

                message = Assert.Single(messages).Message;
                _output.WriteLine(message.ToJsonString());
                var output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
                Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
                Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
            }
            finally
            {
                server.Dispose();
                await StopPublisherAsync();
            }
        }

        [Fact]
        public async Task RestartServerWithCyclicReadTestAsync()
        {
            var server = new ReferenceServer();
            EndpointUrl = server.EndpointUrl;
            const string name = nameof(RestartServerWithCyclicReadTestAsync);
            StartPublisher(name, "./Resources/CyclicRead.json",
                arguments: ["--mm=PubSub", "--dm=false"], keepAliveInterval: 1);
            try
            {
                // Arrange
                // Act
                var (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data");

                // Assert
                var message = Assert.Single(messages).Message;
                Assert.NotNull(metadata);

                await server.RestartAsync(WaitUntilDisconnectedAsync);
                _output.WriteLine("Restarted server");

                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromSeconds(10), 1000,
                    messageType: "ua-data");
                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data");

                message = Assert.Single(messages).Message;
            }
            finally
            {
                server.Dispose();
                await StopPublisherAsync();
            }
        }

        [Fact]
        public async Task SwitchServerWithSameWriterGroupTestAsync()
        {
            var server = new ReferenceServer();
            EndpointUrl = server.EndpointUrl;

            const string name = nameof(SwitchServerWithSameWriterGroupTestAsync);
            StartPublisher(name, "./Resources/DataItems.json", arguments: ["--mm=PubSub", "--dm=false"]);
            try
            {
                // Arrange
                // Act
                var (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data");

                // Assert
                var message = Assert.Single(messages).Message;
                var output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
                Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
                Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
                Assert.NotNull(metadata);

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                var diag = Assert.Single(diagnostics);
                Assert.Equal(name, diag.Endpoint.DataSetWriterGroup);

                // Switch to new server
                var old = server;
                server = new ReferenceServer();
                EndpointUrl = server.EndpointUrl;
                old.Dispose();

                // Point to new server
                WritePublishedNodes(name, "./Resources/DataItems.json");

                // Now we should have torn down the other subscription

                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data");

                message = Assert.Single(messages).Message;
                _output.WriteLine(message.ToJsonString());

                output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
                Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
                Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

                diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                for (var i = 0; i < 10 && diagnostics.Count == 0; i++)
                {
                    _output.WriteLine($"######### {i}: Failed to get diagnosticsinfo.");
                    await Task.Delay(1000);
                    diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                }

                diag = Assert.Single(diagnostics);
                Assert.Equal(name, diag.Endpoint.DataSetWriterGroup);
            }
            finally
            {
                server.Dispose();
                await StopPublisherAsync();
            }
        }

        [Fact]
        public async Task SwitchServerWithDifferentWriterGroupTestAsync()
        {
            var server = new ReferenceServer();
            EndpointUrl = server.EndpointUrl;
            const string name = nameof(SwitchServerWithDifferentWriterGroupTestAsync);
            StartPublisher(name, "./Resources/DataItems2.json", arguments: ["--mm=PubSub", "--dm=false"]);
            try
            {
                // Arrange
                // Act
                var (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data");

                // Assert
                var message = Assert.Single(messages).Message;
                var output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output2");
                Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
                Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
                Assert.NotNull(metadata);

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                var diag = Assert.Single(diagnostics);
                Assert.Equal(name, diag.Endpoint.DataSetWriterGroup);

                // Switch to new server
                var old = server;
                server = new ReferenceServer();
                EndpointUrl = server.EndpointUrl;
                old.Dispose();

                // Point to new server
                const string name2 = nameof(SwitchServerWithDifferentWriterGroupTestAsync) + "new";
                WritePublishedNodes(name2, "./Resources/DataItems2.json");

                // Now we should have torn down the other subscription

                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    predicate: WaitUntilOutput2, messageType: "ua-data");

                message = Assert.Single(messages).Message;
                _output.WriteLine(message.ToJsonString());

                output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output2");
                Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
                Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

                diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                for (var i = 0; i < 10 &&
                    (diagnostics.Count != 1 || diagnostics[0].Endpoint.DataSetWriterGroup != name2); i++)
                {
                    _output.WriteLine($"######### {i}: Failed to get diagnosticsinfo.");
                    await Task.Delay(1000);
                    diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                }

                diag = Assert.Single(diagnostics);
                Assert.Equal(name2, diag.Endpoint.DataSetWriterGroup);
            }
            finally
            {
                await StopPublisherAsync();
                server.Dispose();
            }
        }

        [Theory]
        [InlineData(false, 100)]
        [InlineData(true, 100)]
        [InlineData(false, 1)]
        [InlineData(true, 1)]
        public async Task AddNodeToDataSetWriterGroupWithNodeUsingDeviceMethodAsync(bool differentPublishingInterval,
            int maxMonitoredItems)
        {
            var server = new ReferenceServer();
            EndpointUrl = server.EndpointUrl;
            const string name = nameof(AddNodeToDataSetWriterGroupWithNodeUsingDeviceMethodAsync);
            var testInput1 = GetEndpointsFromFile(name, "./Resources/DataItems.json");
            var testInput2 = GetEndpointsFromFile(name, "./Resources/DataItems2.json");
            if (!differentPublishingInterval)
            {
                // Set both to the same so that there is a single writer instead of 2
                testInput2[0].OpcNodes[0].OpcPublishingInterval = testInput1[0].OpcNodes[0].OpcPublishingInterval;
            }
            StartPublisher(name, arguments: ["--mm=PubSub", "--dm=false", "--xmi=" + maxMonitoredItems]);
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput1[0]);
                Assert.NotNull(result);

                var (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data");

                var message = Assert.Single(messages).Message;
                var output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
                Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
                Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
                Assert.NotNull(metadata);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput1[0].OpcNodes[0].Id, n.Id);

                // Add another node
                result = await PublisherApi.PublishNodesAsync(testInput2[0]);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                e = Assert.Single(endpoints.Endpoints);

                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e);
                Assert.Equal(2, nodes.OpcNodes.Count);
                Assert.Contains(testInput1[0].OpcNodes[0].Id, nodes.OpcNodes.Select(e => e.Id));
                Assert.Contains(testInput2[0].OpcNodes[0].Id, nodes.OpcNodes.Select(e => e.Id));

                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    predicate: WaitUntilOutput2, messageType: "ua-data");

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                var diag = Assert.Single(diagnostics);
                Assert.Equal(0, diag.MonitoredOpcNodesFailedCount);
                Assert.Equal(2, diag.MonitoredOpcNodesSucceededCount);

                // Remove endpoint
                result = await PublisherApi.UnpublishNodesAsync(e);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync();
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                await StopPublisherAsync();
                server.Dispose();
            }
        }

        [Fact]
        public async Task SwitchServerWithDifferentDataTestAsync()
        {
            var server = new ReferenceServer();
            EndpointUrl = server.EndpointUrl;
            const string name = nameof(SwitchServerWithDifferentDataTestAsync);
            StartPublisher(name, "./Resources/DataItems.json", arguments: ["--mm=PubSub", "--dm=false"]);
            try
            {
                // Arrange
                // Act
                var (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data");

                // Assert
                var message = Assert.Single(messages).Message;
                var output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
                Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
                Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
                Assert.NotNull(metadata);

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                var diag = Assert.Single(diagnostics);
                Assert.Equal(name, diag.Endpoint.DataSetWriterGroup);

                WritePublishedNodes(name, "./Resources/empty_pn.json");
                for (var i = 0; i < 10 && diagnostics.Count != 0; i++)
                {
                    diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                    await Task.Delay(1000);
                }
                Assert.Empty(diagnostics);

                // Switch to different server
                var old = server;
                server = new ReferenceServer();
                EndpointUrl = server.EndpointUrl;
                old.Dispose();

                // Point to new server
                WritePublishedNodes(name, "./Resources/DataItems2.json");

                // Now we should have torn down the other subscription

                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    predicate: WaitUntilOutput2, messageType: "ua-data");

                message = Assert.Single(messages).Message;
                _output.WriteLine(message.ToJsonString());

                output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output2");
                Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
                Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
                Assert.NotNull(metadata);

                diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                diag = Assert.Single(diagnostics);
                Assert.Equal(name, diag.Endpoint.DataSetWriterGroup);
            }
            finally
            {
                server.Dispose();
                await StopPublisherAsync();
            }
        }

        [Fact]
        public async Task SwitchSecuritySettingsTestAsync()
        {
            var server = new ReferenceServer();
            EndpointUrl = server.EndpointUrl;
            const string name = nameof(SwitchSecuritySettingsTestAsync);
            StartPublisher(name, "./Resources/Fixedvalue.json", arguments: ["--mm=PubSub", "--dm=false", "--aa"],
                securityMode: Models.SecurityMode.SignAndEncrypt);
            try
            {
                // Arrange
                // Act
                var (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data");

                // Assert
                var message = Assert.Single(messages).Message;
                AssertFixedValueMessage(message);
                Assert.NotNull(metadata);

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                var diag = Assert.Single(diagnostics);
                Assert.Equal(Models.SecurityMode.SignAndEncrypt, diag.Endpoint.EndpointSecurityMode);

                WritePublishedNodes(name, "./Resources/Fixedvalue.json", securityMode: Models.SecurityMode.None);
                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data");

                // Assert
                message = Assert.Single(messages).Message;
                AssertFixedValueMessage(message);
                Assert.NotNull(metadata);

                diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                diag = Assert.Single(diagnostics);
                Assert.Null(diag.Endpoint.EndpointSecurityMode);

                WritePublishedNodes(name, "./Resources/Fixedvalue.json", securityMode: Models.SecurityMode.Sign);
                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data");

                // Assert
                message = Assert.Single(messages).Message;
                AssertFixedValueMessage(message);
                Assert.NotNull(metadata);

                diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                diag = Assert.Single(diagnostics);
                Assert.Equal(Models.SecurityMode.Sign, diag.Endpoint.EndpointSecurityMode);
            }
            finally
            {
                server.Dispose();
                await StopPublisherAsync();
            }
        }

        [Fact]
        public async Task RestartConfigurationTestAsync()
        {
            using var server = new ReferenceServer();
            EndpointUrl = server.EndpointUrl;
            for (var cycles = 0; cycles < 3; cycles++)
            {
                const string name = nameof(RestartConfigurationTestAsync);
                StartPublisher(name, "./Resources/DataItems.json", arguments: ["--mm=PubSub", "--dm=false"]);
                try
                {
                    // Arrange
                    // Act
                    await WaitForMessagesAndMetadataAsync(TimeSpan.FromSeconds(5), 1, messageType: "ua-data");

                    const string name2 = nameof(RestartConfigurationTestAsync) + "new";
                    WritePublishedNodes(name2, "./Resources/DataItems2.json");
                    var diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                    for (var i = 0; i < 60 &&
                        (diagnostics.Count != 1 || diagnostics[0].Endpoint.DataSetWriterGroup != name2); i++)
                    {
                        _output.WriteLine($"######### {i}: Failed to get diagnosticsinfo.");
                        await Task.Delay(500);
                        diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                    }
                    var diag = Assert.Single(diagnostics);
                    Assert.Equal(name2, diag.Endpoint.DataSetWriterGroup);
                }
                finally
                {
                    await StopPublisherAsync();
                }
            }
        }

        internal static JsonElement WaitUntilOutput2(JsonElement jsonElement)
        {
            var messages = jsonElement.GetProperty("Messages");
            if (messages.ValueKind == JsonValueKind.Array)
            {
                var element = messages.EnumerateArray().FirstOrDefault();
                if (element.GetProperty("Payload").TryGetProperty("Output2", out _))
                {
                    return jsonElement;
                }
            }
            return default;
        }

        private async Task WaitUntilDisconnectedAsync()
        {
            using var cts = new CancellationTokenSource(60000);
            while (true)
            {
                cts.Token.ThrowIfCancellationRequested();
                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync(cts.Token);
                var diag = Assert.Single(diagnostics);
                if (!diag.OpcEndpointConnected)
                {
                    _output.WriteLine("Disconnected!");
                    break;
                }
                await Task.Delay(1000, cts.Token);
            }
        }

        internal static void AssertFixedValueMessage(JsonElement message)
        {
            var m = message.GetProperty("Messages")[0];
            var type = m.GetProperty("MessageType").GetString();
            // TODO       Assert.Equal("ua-keyframe", type);
            var payload1 = m.GetProperty("Payload");
            var items1 = new[]
            {
                payload1.GetProperty("LocaleIdArray"),
                payload1.GetProperty("ServerArray"),
                payload1.GetProperty("NamespaceArray")
            };
            Assert.All(items1, item =>
                Assert.Equal(JsonValueKind.Array, item.GetProperty("Value").ValueKind));
        }
    }
}
