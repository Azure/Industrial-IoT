// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.ReferenceServer
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Divergic.Logging.Xunit;
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class ReverseConnectIntegrationTests : PublisherIntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public ReverseConnectIntegrationTests(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RegisteredReadTestAsync(bool useReverseConnect)
        {
            var server = ReferenceServer.Create(LogFactory.Create(_output, Logging.Config), useReverseConnect);
            EndpointUrl = server.EndpointUrl;

            var name = nameof(RegisteredReadTestAsync) + (useReverseConnect ? "WithReverseConnect" : "NoReverseConnect");
            StartPublisher(name, "./Resources/RegisteredRead.json", arguments: new string[] { "--mm=PubSub" },
                reverseConnectPort: useReverseConnect ? server.ReverseConnectPort : null);
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
            }
            finally
            {
                server.Dispose();
                StopPublisher();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task KeepAliveTestAsync(bool useReverseConnect)
        {
            using var server = ReferenceServer.Create(LogFactory.Create(_output, Logging.Config), useReverseConnect);
            EndpointUrl = server.EndpointUrl;

            var name = nameof(KeepAliveTestAsync) + (useReverseConnect ? "WithReverseConnect" : "NoReverseConnect");
            StartPublisher(name, "./Resources/KeepAlive.json",
                reverseConnectPort: useReverseConnect ? server.ReverseConnectPort : null);
            try
            {
                // Arrange
                // Act
                var (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    predicate: WaitUntilKeepAlive, messageType: "ua-data").ConfigureAwait(false);

                // Assert
                var message = Assert.Single(messages).Message;
                Assert.True(message.GetProperty("Messages")[0].TryGetProperty("Payload", out var payload));
                Assert.Empty(payload.EnumerateObject());

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync().ConfigureAwait(false);
                var diag = Assert.Single(diagnostics);
                Assert.Equal(name, diag.Endpoint.DataSetWriterGroup);
            }
            finally
            {
                StopPublisher();
            }

            static JsonElement WaitUntilKeepAlive(JsonElement jsonElement)
            {
                var messages = jsonElement.GetProperty("Messages");
                if (messages.ValueKind == JsonValueKind.Array)
                {
                    var element = messages.EnumerateArray().FirstOrDefault();
                    if (element.GetProperty("MessageType").GetString() == "ua-keepalive")
                    {
                        return jsonElement;
                    }
                }
                return default;
            }
        }
    }
}
