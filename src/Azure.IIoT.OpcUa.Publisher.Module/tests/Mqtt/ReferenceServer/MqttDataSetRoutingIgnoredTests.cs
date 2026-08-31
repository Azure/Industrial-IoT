// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.ReferenceServer
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// 3.0 removed automatic topic routing from browse paths. The topic was
    /// built per notification from a path discovered from the server at
    /// runtime, and the native PubSub runtime has no per-message topic - both
    /// of its data send paths publish to the transport's configured topic.
    ///
    /// The option is accepted and ignored rather than rejected, so that a 2.9
    /// command line or published nodes file still starts. That promise is what
    /// these tests hold: a configuration asking for routing publishes, and it
    /// publishes to the configured topic rather than to a browse path.
    /// </summary>
    [Collection(MqttReferenceServerCollection.Name)]
    public class MqttDataSetRoutingIgnoredTests : PublisherIntegrationTestBase,
        IClassFixture<ReferenceServer>
    {
        private readonly ReferenceServer _fixture;
        private readonly ITestOutputHelper _output;

        public MqttDataSetRoutingIgnoredTests(ReferenceServer fixture, ITestOutputHelper output)
            : base(output)
        {
            _output = output;
            _fixture = fixture;
            EndpointUrl = _fixture.EndpointUrl;
        }

        [Theory]
        [InlineData("UseBrowseNames")]
        [InlineData("UseBrowseNamesWithNamespaceIndex")]
        public async Task RoutingModeIsAcceptedAndPublishesToTheConfiguredTopicAsync(string mode)
        {
            var name = nameof(RoutingModeIsAcceptedAndPublishesToTheConfiguredTopicAsync) + mode;

            var (_, messages) = await ProcessMessagesAndMetadataAsync(
                name, "./Resources/DataItems1.json",
                messageCollectionTimeout: TimeSpan.FromMinutes(1), messageCount: 1,
                arguments: ["--mm=SingleRawDataSet", "--uns=" + mode], version: MqttVersion.v5);

            messages.ForEach(m => _output.WriteLine(m.Topic));
            Assert.NotEmpty(messages);

            //
            // Every message lands on the writer group topic. Under the removed
            // routing mode each would have carried the item's browse path
            // appended - "/Objects/Server/ServerStatus/CurrentTime" and the
            // like - so a topic with more segments than the configured root is
            // exactly the regression this guards against.
            //
            Assert.All(messages, message =>
                Assert.EndsWith(name, message.Topic, StringComparison.Ordinal));
        }
    }
}
