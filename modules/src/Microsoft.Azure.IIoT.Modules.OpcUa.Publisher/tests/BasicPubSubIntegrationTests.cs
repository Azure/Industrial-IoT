// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Currently, we create new independent instances of server, publisher and mocked IoT services for each test,
    /// this could be optimised e.g. create only single instance of server and publisher between tests in the same class.
    /// </summary>
    [Collection(ReadCollection.Name)]
    public class BasicPubSubIntegrationTests : PublisherIntegrationTestBase {
        private readonly ITestOutputHelper _output;

        public BasicPubSubIntegrationTests(ReferenceServerFixture fixture, ITestOutputHelper output) : base(fixture) {
            _output = output;
        }

        [Fact]
        public async Task CanSendDataItemToIoTHubTest() {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(@"./PublishedNodes/DataItems.json",
                messageType: "ua-data", arguments: new string[] { "--mm=PubSub" }).ConfigureAwait(false);

            // Assert
            var message = Assert.Single(messages);
            var output = message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(),
                double.MinValue, double.MaxValue);
        }

        [Fact]
        public async Task CanSendPendingConditionsToIoTHubTest() {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(@"./PublishedNodes/PendingAlarms.json", GetAlarmCondition,
                messageType: "ua-data", arguments: new string[] { "--mm=PubSub" }).ConfigureAwait(false);

            // Assert
            _output.WriteLine(messages.ToString());
            var evt = Assert.Single(messages);

            Assert.Equal(JsonValueKind.Object, evt.ValueKind);
            Assert.True(evt.GetProperty("Payload").GetProperty("Severity").GetInt32() >= 100);
        }

        private static JsonElement GetAlarmCondition(JsonElement jsonElement) {
            var messages = jsonElement.GetProperty("Messages");
            if (messages.ValueKind != JsonValueKind.Array) {
                return default;
            }
            return messages.EnumerateArray().FirstOrDefault(element => {
                return element.GetProperty("MessageType").GetString() == "ua-condition" &&
                       element.GetProperty("Payload").TryGetProperty("SourceNode", out var node) &&
                        node.GetString().StartsWith("http://opcfoundation.org/AlarmCondition#s=1%3a");
            });
        }
    }
}