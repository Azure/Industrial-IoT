// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Opc.Ua;
    using Opc.Ua.Encoders;
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Currently, we create new independent instances of server, publisher and mocked IoT services for each test,
    /// this could be optimised e.g. create only single instance of server and publisher between tests in the same class.
    /// </summary>
    [Collection(ReadCollection.Name)]
    public class ReversibleEncodingIntegrationTests : PublisherIntegrationTestBase {
        private const string kEventId = "EventId";
        private const string kMessage = "Message";
        private const string kCycleId = "http://opcfoundation.org/SimpleEvents#CycleId";
        private const string kCurrentStep = "http://opcfoundation.org/SimpleEvents#CurrentStep";

        public ReversibleEncodingIntegrationTests(ReferenceServerFixture fixture) : base(fixture) { }

        [Theory]
        [InlineData(@"./PublishedNodes/SimpleEvents.json")]
        public async Task CanEncodeWithoutReversibleEncodingTest(string publishedNodesFile) {
            // Arrange
            // Act
            var result = await ProcessMessagesAsync(
                publishedNodesFile, messageType: "ua-data",
                arguments: new[] { "--mm=PubSub" }
            ).ConfigureAwait(false);

            Assert.Single(result);

            var messages = result
                .SelectMany(x => x.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m => {
                var value = m.GetProperty("Payload");
                var eventId = value.GetProperty(kEventId);
                var message = value.GetProperty(kMessage);
                var cycleId = value.GetProperty(kCycleId);
                var currentStep = value.GetProperty(kCurrentStep);

                Assert.Equal(JsonValueKind.String, eventId.ValueKind);
                Assert.Equal(JsonValueKind.String, message.ValueKind);
                Assert.Equal(JsonValueKind.String, cycleId.ValueKind);
                Assert.Equal(JsonValueKind.String, currentStep.GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, currentStep.GetProperty("Duration").ValueKind);
            });
        }

        [Theory]
        [InlineData(@"./PublishedNodes/SimpleEvents.json")]
        public async Task CanEncodeWithReversibleEncodingTest(string publishedNodesFile) {
            // Arrange
            // Act
            var result = await ProcessMessagesAsync(
                publishedNodesFile, TimeSpan.FromMinutes(2), 4, messageType: "ua-data",
                arguments: new[] { "--mm=PubSub", "--UseReversibleEncoding=True" }
            ).ConfigureAwait(false);

            var messages = result
                .SelectMany(x => x.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m => {
                var body = m.GetProperty("Payload");
                var eventId = body.GetProperty(kEventId);
                Assert.Equal("ByteString", eventId.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, eventId.GetProperty("Body").ValueKind);

                var message = body.GetProperty(kMessage);
                Assert.Equal("LocalizedText", message.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, message.GetProperty("Body").GetProperty("Text").ValueKind);
                Assert.Equal("en-US", message.GetProperty("Body").GetProperty("Locale").GetString());

                var cycleId = body.GetProperty(kCycleId);
                Assert.Equal("String", cycleId.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, cycleId.GetProperty("Body").ValueKind);

                var currentStep = body.GetProperty(kCurrentStep);
                body = currentStep.GetProperty("Body");
                Assert.Equal("ExtensionObject", currentStep.GetProperty("Type").GetString());
                Assert.Equal("http://opcfoundation.org/SimpleEvents#i=183", body.GetProperty("TypeId").GetString());
                Assert.Equal("Json", body.GetProperty("Encoding").GetString());
                Assert.Equal(JsonValueKind.String, body.GetProperty("Body").GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, body.GetProperty("Body").GetProperty("Duration").ValueKind);
            });
        }

        [Theory]
        [InlineData(@"./PublishedNodes/SimpleEvents.json")]
        public async Task CanEncodeWithReversibleEncodingSamplesTest(string publishedNodesFile) {
            // Arrange
            // Act
            var result = await ProcessMessagesAsync(
                publishedNodesFile,
                arguments: new[] { "--mm=Samples", "--UseReversibleEncoding=True" }
            ).ConfigureAwait(false);

            var m = Assert.Single(result);

            var value = m.GetProperty("Value");
            var type = value.GetProperty("Type").GetString();
            var body = value.GetProperty("Body");
            Assert.Equal("ExtensionObject", type);

            var typeId = body.GetProperty("TypeId").GetString();
            var encoding = body.GetProperty("Encoding").GetString();
            body = body.GetProperty("Body");
            Assert.Equal("http://microsoft.com/Industrial-IoT/OpcPublisher#i=1", typeId);
            Assert.Equal("Json", encoding);

            var eventId = body.GetProperty(kEventId);
            Assert.Equal("ByteString", eventId.GetProperty("Type").GetString());
            Assert.Equal(JsonValueKind.String, eventId.GetProperty("Body").ValueKind);

            var message = body.GetProperty(kMessage);
            Assert.Equal("LocalizedText", message.GetProperty("Type").GetString());
            Assert.Equal(JsonValueKind.String, message.GetProperty("Body").GetProperty("Text").ValueKind);
            Assert.Equal("en-US", message.GetProperty("Body").GetProperty("Locale").GetString());

            var cycleId = body.GetProperty(kCycleId);
            Assert.Equal("String", cycleId.GetProperty("Type").GetString());
            Assert.Equal(JsonValueKind.String, cycleId.GetProperty("Body").ValueKind);

            var currentStep = body.GetProperty(kCurrentStep);
            body = currentStep.GetProperty("Body");
            Assert.Equal("ExtensionObject", currentStep.GetProperty("Type").GetString());
            Assert.Equal("http://opcfoundation.org/SimpleEvents#i=183", body.GetProperty("TypeId").GetString());
            Assert.Equal("Json", body.GetProperty("Encoding").GetString());
            Assert.Equal(JsonValueKind.String, body.GetProperty("Body").GetProperty("Name").ValueKind);
            Assert.Equal(JsonValueKind.Number, body.GetProperty("Body").GetProperty("Duration").ValueKind);

            var json = value
                        .GetProperty("Body")
                        .GetProperty("Body")
                        .GetRawText();
            var buffer = Encoding.UTF8.GetBytes(json);

            var serviceMessageContext = new ServiceMessageContext();
            serviceMessageContext.Factory.AddEncodeableType(typeof(EncodeableDictionary));

            using (var stream = new MemoryStream(buffer)) {
                using var decoder = new JsonDecoderEx(stream, serviceMessageContext);
                var actual = new EncodeableDictionary();
                actual.Decode(decoder);

                Assert.Equal(4, actual.Count);
                Assert.Equal(new[] { kEventId, kMessage, kCycleId, kCurrentStep }, actual.Select(x => x.Key));
                Assert.All(actual.Select(x => x.Value?.Value), Assert.NotNull);

                var eof = decoder.ReadDataValue(null);
                Assert.Null(eof);
            }
        }
    }
}