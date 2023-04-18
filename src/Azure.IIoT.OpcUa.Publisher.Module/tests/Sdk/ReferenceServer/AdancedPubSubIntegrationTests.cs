// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.ReferenceServer
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Divergic.Logging.Xunit;
    using Microsoft.AspNetCore.Hosting.Server;
    using Microsoft.Extensions.Logging;
    using System;
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
        private readonly ILoggerFactory _loggerFactory;

        public AdvancedPubSubIntegrationTests(ITestOutputHelper output) : base(output)
        {
            _loggerFactory = LogFactory.Create(output, Logging.Config);
        }

        [Fact]
        public async Task SwitchServerTestWithSameWriterGroup()
        {
            var server = ReferenceServer.Create(_loggerFactory);
            ServerPort = server.Port;

            const string name = nameof(SwitchServerTestWithSameWriterGroup);
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
                server = ReferenceServer.Create(_loggerFactory);
                ServerPort = server.Port;
                old?.Dispose();

                // Point to new server
                WritePublishedNodes(name, "./Resources/DataItems.json");

                await Task.Delay(100).ConfigureAwait(false);
                // Now we should have torn down the other subscription

                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data").ConfigureAwait(false);

                message = Assert.Single(messages).Message;
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
        public async Task SwitchServerTestWithDifferentWriterGroup()
        {
            var server = ReferenceServer.Create(_loggerFactory);
            ServerPort = server.Port;
            const string name = nameof(SwitchServerTestWithDifferentWriterGroup);
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
                server = ReferenceServer.Create(_loggerFactory);
                ServerPort = server.Port;
                old?.Dispose();

                // Point to new server
                const string name2 = nameof(SwitchServerTestWithDifferentWriterGroup) + "new";
                WritePublishedNodes(name2, "./Resources/DataItems2.json");

                await Task.Delay(100).ConfigureAwait(false);
                // Now we should have torn down the other subscription

                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data").ConfigureAwait(false);

                message = Assert.Single(messages).Message;
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
        public async Task SwitchServerTestWithDifferentData()
        {
            var server = ReferenceServer.Create(_loggerFactory);
            ServerPort = server.Port;
            const string name = nameof(SwitchServerTestWithDifferentWriterGroup);
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
                server = ReferenceServer.Create(_loggerFactory);
                ServerPort = server.Port;
                old?.Dispose();

                // Point to new server
                WritePublishedNodes(name, "./Resources/DataItems2.json");

                await Task.Delay(100).ConfigureAwait(false);
                // Now we should have torn down the other subscription

                (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    messageType: "ua-data").ConfigureAwait(false);

                message = Assert.Single(messages).Message;
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
    }
}
