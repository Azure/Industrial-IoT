// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.ReferenceServer
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class AdvancedPubSubIntegrationTests : PublisherIntegrationTestBase
    {
        internal const string kEventId = "EventId";
        internal const string kMessage = "Message";
        internal const string kCycleId = "http://opcfoundation.org/SimpleEvents#CycleId";
        internal const string kCurrentStep = "http://opcfoundation.org/SimpleEvents#CurrentStep";
        private readonly ITestOutputHelper _output;

        public AdvancedPubSubIntegrationTests(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        [Fact]
        public async Task SwitchServerWithSameWriterGroupTest()
        {
            var server = new ReferenceServer();
            ServerPort = server.Port;

            const string name = nameof(SwitchServerWithSameWriterGroupTest);
            StartPublisher(name, "./Resources/DataItems.json", arguments: new string[] { "--mm=PubSub" });
            try
            {
                // Arrange
                // Act
                var (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data").ConfigureAwait(false);

                // Assert
                var message = Assert.Single(messages).Message;
                var output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
                Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
                Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
                Assert.NotNull(metadata);

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync().ConfigureAwait(false);
                var diag = Assert.Single(diagnostics);
                Assert.Equal(name, diag.Endpoint.DataSetWriterGroup);

                // Switch to new server
                var old = server;
                server = new ReferenceServer();
                ServerPort = server.Port;
                old?.Dispose();

                // Point to new server
                WritePublishedNodes(name, "./Resources/DataItems.json");

                // Now we should have torn down the other subscription

                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data").ConfigureAwait(false);

                message = Assert.Single(messages).Message;
                _output.WriteLine(message.ToString());

                output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
                Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
                Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

                diagnostics = await PublisherApi.GetDiagnosticInfoAsync().ConfigureAwait(false);
                diag = Assert.Single(diagnostics);
                Assert.Equal(name, diag.Endpoint.DataSetWriterGroup);
            }
            finally
            {
                server.Dispose();
                StopPublisher();
            }
        }

        [Fact]
        public async Task SwitchServerWithDifferentWriterGroupTest()
        {
            var server = new ReferenceServer();
            ServerPort = server.Port;
            const string name = nameof(SwitchServerWithDifferentWriterGroupTest);
            StartPublisher(name, "./Resources/DataItems2.json", arguments: new string[] { "--mm=PubSub" });
            try
            {
                // Arrange
                // Act
                var (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data").ConfigureAwait(false);

                // Assert
                var message = Assert.Single(messages).Message;
                var output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output2");
                Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
                Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
                Assert.NotNull(metadata);

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync().ConfigureAwait(false);
                var diag = Assert.Single(diagnostics);
                Assert.Equal(name, diag.Endpoint.DataSetWriterGroup);

                // Switch to new server
                var old = server;
                server = new ReferenceServer();
                ServerPort = server.Port;
                old?.Dispose();

                // Point to new server
                const string name2 = nameof(SwitchServerWithDifferentWriterGroupTest) + "new";
                WritePublishedNodes(name2, "./Resources/DataItems2.json");

                // Now we should have torn down the other subscription

                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    predicate: WaitUntilOutput2, messageType: "ua-data").ConfigureAwait(false);

                message = Assert.Single(messages).Message;
                _output.WriteLine(message.ToString());

                output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output2");
                Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
                Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

                diagnostics = await PublisherApi.GetDiagnosticInfoAsync().ConfigureAwait(false);
                for (var i = 0; i < 1000 &&
                    (diagnostics.Count != 1 || diagnostics[0].Endpoint.DataSetWriterGroup != name2); i++)
                {
                    _output.WriteLine($"######### {i}: Failed to get diagnosticsinfo.");
                    await Task.Delay(1000).ConfigureAwait(false);
                    diagnostics = await PublisherApi.GetDiagnosticInfoAsync().ConfigureAwait(false);
                }

                diag = Assert.Single(diagnostics);
                Assert.Equal(name2, diag.Endpoint.DataSetWriterGroup);
            }
            finally
            {
                StopPublisher();
                server.Dispose();
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AddNodeToDataSetWriterGroupWithNodeUsingDeviceMethod(bool differentPublishingInterval)
        {
            var server = new ReferenceServer();
            ServerPort = server.Port;
            const string name = nameof(AddNodeToDataSetWriterGroupWithNodeUsingDeviceMethod);
            var testInput1 = GetEndpointsFromFile(name, "./Resources/DataItems.json");
            var testInput2 = GetEndpointsFromFile(name, "./Resources/DataItems2.json");
            if (!differentPublishingInterval)
            {
                // Set both to the same so that there is a single writer instead of 2
                testInput2[0].OpcNodes[0].OpcPublishingInterval = testInput1[0].OpcNodes[0].OpcPublishingInterval;
            }
            StartPublisher(name, arguments: new string[] { "--mm=PubSub" });
            try
            {
                var endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                Assert.Empty(endpoints.Endpoints);

                var result = await PublisherApi.PublishNodesAsync(testInput1[0]).ConfigureAwait(false);
                Assert.NotNull(result);

                var (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data").ConfigureAwait(false);

                var message = Assert.Single(messages).Message;
                var output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
                Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
                Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
                Assert.NotNull(metadata);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                var e = Assert.Single(endpoints.Endpoints);

                var nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e).ConfigureAwait(false);
                var n = Assert.Single(nodes.OpcNodes);
                Assert.Equal(testInput1[0].OpcNodes[0].Id, n.Id);

                // Add another node
                result = await PublisherApi.PublishNodesAsync(testInput2[0]).ConfigureAwait(false);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                e = Assert.Single(endpoints.Endpoints);

                nodes = await PublisherApi.GetConfiguredNodesOnEndpointAsync(e).ConfigureAwait(false);
                Assert.Equal(2, nodes.OpcNodes.Count);
                Assert.Contains(testInput1[0].OpcNodes[0].Id, nodes.OpcNodes.Select(e => e.Id));
                Assert.Contains(testInput2[0].OpcNodes[0].Id, nodes.OpcNodes.Select(e => e.Id));

                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    predicate: WaitUntilOutput2, messageType: "ua-data").ConfigureAwait(false);

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync().ConfigureAwait(false);
                var diag = Assert.Single(diagnostics);
                Assert.Equal(0, diag.MonitoredOpcNodesFailedCount);
                Assert.Equal(2, diag.MonitoredOpcNodesSucceededCount);

                // Remove endpoint
                result = await PublisherApi.UnpublishNodesAsync(e).ConfigureAwait(false);
                Assert.NotNull(result);

                endpoints = await PublisherApi.GetConfiguredEndpointsAsync().ConfigureAwait(false);
                Assert.Empty(endpoints.Endpoints);
            }
            finally
            {
                StopPublisher();
                server.Dispose();
            }
        }

        [Fact]
        public async Task SwitchServerWithDifferentDataTest()
        {
            var server = new ReferenceServer();
            ServerPort = server.Port;
            const string name = nameof(SwitchServerWithDifferentDataTest);
            StartPublisher(name, "./Resources/DataItems.json", arguments: new string[] { "--mm=PubSub" });
            try
            {
                // Arrange
                // Act
                var (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data").ConfigureAwait(false);

                // Assert
                var message = Assert.Single(messages).Message;
                var output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
                Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
                Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
                Assert.NotNull(metadata);

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync().ConfigureAwait(false);
                var diag = Assert.Single(diagnostics);
                Assert.Equal(name, diag.Endpoint.DataSetWriterGroup);

                WritePublishedNodes(name, "./Resources/empty_pn.json");
                for (var i = 0; i < 10 && diagnostics.Count != 0; i++)
                {
                    diagnostics = await PublisherApi.GetDiagnosticInfoAsync().ConfigureAwait(false);
                    await Task.Delay(1000).ConfigureAwait(false);
                }
                Assert.Empty(diagnostics);

                // Switch to different server
                var old = server;
                server = new ReferenceServer();
                ServerPort = server.Port;
                old?.Dispose();

                // Point to new server
                WritePublishedNodes(name, "./Resources/DataItems2.json");

                // Now we should have torn down the other subscription

                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    predicate: WaitUntilOutput2, messageType: "ua-data").ConfigureAwait(false);

                message = Assert.Single(messages).Message;
                _output.WriteLine(message.ToString());

                output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output2");
                Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
                Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
                Assert.NotNull(metadata);

                diagnostics = await PublisherApi.GetDiagnosticInfoAsync().ConfigureAwait(false);
                diag = Assert.Single(diagnostics);
                Assert.Equal(name, diag.Endpoint.DataSetWriterGroup);
            }
            finally
            {
                server.Dispose();
                StopPublisher();
            }
        }

        [Fact]
        public async Task RestartConfigurationTest()
        {
            var server = new ReferenceServer();
            ServerPort = server.Port;
            for (var cycles = 0; cycles < 5; cycles++)
            {
                const string name = nameof(RestartConfigurationTest);
                StartPublisher(name, "./Resources/DataItems.json", arguments: new string[] { "--mm=PubSub" });
                try
                {
                    // Arrange
                    // Act
                    await WaitForMessagesAndMetadataAsync(TimeSpan.FromSeconds(10), 1, messageType: "ua-data").ConfigureAwait(false);

                    const string name2 = nameof(RestartConfigurationTest) + "new";
                    WritePublishedNodes(name2, "./Resources/DataItems2.json");
                    var diagnostics = await PublisherApi.GetDiagnosticInfoAsync().ConfigureAwait(false);
                    for (var i = 0; i < 1000 &&
                        (diagnostics.Count != 1 || diagnostics[0].Endpoint.DataSetWriterGroup != name2); i++)
                    {
                        _output.WriteLine($"######### {i}: Failed to get diagnosticsinfo.");
                        await Task.Delay(1000).ConfigureAwait(false);
                        diagnostics = await PublisherApi.GetDiagnosticInfoAsync().ConfigureAwait(false);
                    }
                    var diag = Assert.Single(diagnostics);
                    Assert.Equal(name2, diag.Endpoint.DataSetWriterGroup);
                }
                finally
                {
                    StopPublisher();
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
    }
}
