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
        public async Task SwitchServerWithSameWriterGroupTest()
        {
            var server = new ReferenceServer();
            EndpointUrl = server.EndpointUrl;

            const string name = nameof(SwitchServerWithSameWriterGroupTest);
            StartPublisher(name, "./Resources/DataItems.json", arguments: new string[] { "--mm=PubSub", "--dm=false" });
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
        public async Task SwitchServerWithDifferentWriterGroupTest()
        {
            var server = new ReferenceServer();
            EndpointUrl = server.EndpointUrl;
            const string name = nameof(SwitchServerWithDifferentWriterGroupTest);
            StartPublisher(name, "./Resources/DataItems2.json", arguments: new string[] { "--mm=PubSub", "--dm=false" });
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
                const string name2 = nameof(SwitchServerWithDifferentWriterGroupTest) + "new";
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
        [InlineData(false)]
        [InlineData(true)]
        public async Task AddNodeToDataSetWriterGroupWithNodeUsingDeviceMethod(bool differentPublishingInterval)
        {
            var server = new ReferenceServer();
            EndpointUrl = server.EndpointUrl;
            const string name = nameof(AddNodeToDataSetWriterGroupWithNodeUsingDeviceMethod);
            var testInput1 = GetEndpointsFromFile(name, "./Resources/DataItems.json");
            var testInput2 = GetEndpointsFromFile(name, "./Resources/DataItems2.json");
            if (!differentPublishingInterval)
            {
                // Set both to the same so that there is a single writer instead of 2
                testInput2[0].OpcNodes[0].OpcPublishingInterval = testInput1[0].OpcNodes[0].OpcPublishingInterval;
            }
            StartPublisher(name, arguments: new string[] { "--mm=PubSub", "--dm=false" });
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
        public async Task SwitchServerWithDifferentDataTest()
        {
            var server = new ReferenceServer();
            EndpointUrl = server.EndpointUrl;
            const string name = nameof(SwitchServerWithDifferentDataTest);
            StartPublisher(name, "./Resources/DataItems.json", arguments: new string[] { "--mm=PubSub", "--dm=false" });
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
        public async Task RestartConfigurationTest()
        {
            using var server = new ReferenceServer();
            EndpointUrl = server.EndpointUrl;
            for (var cycles = 0; cycles < 3; cycles++)
            {
                const string name = nameof(RestartConfigurationTest);
                StartPublisher(name, "./Resources/DataItems.json", arguments: new string[] { "--mm=PubSub", "--dm=false" });
                try
                {
                    // Arrange
                    // Act
                    await WaitForMessagesAndMetadataAsync(TimeSpan.FromSeconds(5), 1, messageType: "ua-data");

                    const string name2 = nameof(RestartConfigurationTest) + "new";
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
    }
}
