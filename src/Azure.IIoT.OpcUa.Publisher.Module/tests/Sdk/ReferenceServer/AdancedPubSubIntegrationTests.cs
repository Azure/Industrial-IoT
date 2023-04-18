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
                diag = Assert.Single(diagnostics);
                Assert.Equal(name2, diag.Endpoint.DataSetWriterGroup);
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
