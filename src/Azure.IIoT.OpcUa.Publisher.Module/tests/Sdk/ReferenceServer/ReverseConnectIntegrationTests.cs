// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.ReferenceServer
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Neovolve.Logging.Xunit;
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
#pragma warning disable CA2000 // Dispose objects before losing scope
            var server = ReferenceServer.Create(LogFactory.Create(_output, Logging.Config), useReverseConnect);
#pragma warning restore CA2000 // Dispose objects before losing scope
            EndpointUrl = server.EndpointUrl;

            var name = nameof(RegisteredReadTestAsync) + (useReverseConnect ? "WithReverseConnect" : "NoReverseConnect");
            StartPublisher(name, "./Resources/RegisteredRead.json", arguments: ["--mm=PubSub", "--dm=false"],
                reverseConnectPort: useReverseConnect ? server.ReverseConnectPort : null);
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

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                var diag = Assert.Single(diagnostics);
                Assert.Equal(name, diag.Endpoint.DataSetWriterGroup);
                Assert.NotNull(metadata);
            }
            finally
            {
                server.Dispose();
                await StopPublisherAsync();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task KeepAliveTestAsync(bool useReverseConnect)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            using var server = ReferenceServer.Create(LogFactory.Create(_output, Logging.Config), useReverseConnect);
#pragma warning restore CA2000 // Dispose objects before losing scope
            EndpointUrl = server.EndpointUrl;

            var name = nameof(KeepAliveTestAsync) + (useReverseConnect ? "WithReverseConnect" : "NoReverseConnect");
            StartPublisher(name, "./Resources/KeepAlive.json",
                reverseConnectPort: useReverseConnect ? server.ReverseConnectPort : null);
            try
            {
                // Arrange
                // Act
                var (metadata, messages) = await WaitForMessagesAndMetadataAsync(TimeSpan.FromMinutes(2), 1,
                    predicate: WaitUntilKeepAlive, messageType: "ua-data");

                // Assert
                var message = Assert.Single(messages).Message;
                Assert.True(message.GetProperty("Messages")[0].TryGetProperty("Payload", out var payload));
                Assert.Empty(payload.EnumerateObject());

                var diagnostics = await PublisherApi.GetDiagnosticInfoAsync();
                var diag = Assert.Single(diagnostics);
                Assert.Equal(name, diag.Endpoint.DataSetWriterGroup);
            }
            finally
            {
                await StopPublisherAsync();
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
