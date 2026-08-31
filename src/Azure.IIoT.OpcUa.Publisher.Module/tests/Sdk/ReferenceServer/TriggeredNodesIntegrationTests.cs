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

    /// <summary>
    /// Server side triggering, end to end. A node configured with
    /// <c>TriggeredNodes</c> reports the triggered node's value alongside its
    /// own, because the server sends both when the triggering item fires.
    /// </summary>
    /// <remarks>
    /// The converter tests prove the configuration reaches the monitored item
    /// template. This proves the template reaches the server: the publisher
    /// has to call SetTriggering for the triggered value to arrive at all,
    /// since nothing else subscribes to it.
    /// </remarks>
    public class TriggeredNodesIntegrationTests : PublisherIntegrationTestBase,
        IClassFixture<ReferenceServer>
    {
        private readonly ReferenceServer _fixture;
        private readonly ITestOutputHelper _output;

        public TriggeredNodesIntegrationTests(ReferenceServer fixture, ITestOutputHelper output)
            : base(output)
        {
            _output = output;
            _fixture = fixture;
            EndpointUrl = _fixture.EndpointUrl;
        }

        [Fact]
        public async Task TriggeredNodeIsReportedAlongsideItsTriggerAsync()
        {
            var messages = await ProcessMessagesAsync(
                nameof(TriggeredNodeIsReportedAlongsideItsTriggerAsync),
                "./Resources/TriggeredNodes.json", TimeSpan.FromMinutes(2), 5,
                messageType: "ua-data", arguments: ["--mm=PubSub", "--dm=false"]);

            Assert.NotEmpty(messages);
            messages.ForEach(m => _output.WriteLine(m.Message.ToJsonString()));

            var payloads = messages
                .SelectMany(m => m.Message.GetProperty("Messages").EnumerateArray())
                .Select(m => m.GetProperty("Payload"))
                .ToList();

            //
            // The trigger is a reporting item and publishes on its own.
            //
            Assert.Contains(payloads, p => p.TryGetProperty("CurrentTime", out _));

            //
            // The triggered item is in sampling mode and its value - the
            // server's running state - never changes, so nothing about it
            // would ever be published. It can only appear because the server
            // was told to report it when the trigger fires, which is what
            // SetTriggering does and what nothing but this test observes.
            //
            Assert.Contains(payloads, p => p.TryGetProperty("ServerState", out _));
        }
    }
}
