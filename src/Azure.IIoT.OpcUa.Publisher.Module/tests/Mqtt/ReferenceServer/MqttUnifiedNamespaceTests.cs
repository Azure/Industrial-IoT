// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.ReferenceServer
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Furly.Extensions.Mqtt;
    using Json.More;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class MqttUnifiedNamespaceTests : PublisherIntegrationTestBase
    {
        private readonly ReferenceServer _fixture;
        private readonly ITestOutputHelper _output;

        public MqttUnifiedNamespaceTests(ITestOutputHelper output) : base(output)
        {
            _output = output;
            _fixture = new ReferenceServer();
            EndpointUrl = _fixture.EndpointUrl;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _fixture.Dispose();
            }
        }

        [Fact]
        public async Task CanSendAddressSpaceDataToUnifiedNamespace()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendAddressSpaceDataToUnifiedNamespace), "./Resources/DataItems1.json",
                messageCollectionTimeout: TimeSpan.FromMinutes(1), messageCount: 10,
                arguments: new string[] { "--mm=SingleRawDataSet", "--uns=UseBrowseNamesWithNamespaceIndex" }, version: MqttVersion.v5);

            // Assert
            Assert.NotEmpty(messages);
            var currentTimes = messages.Where(m => m.Topic
                .EndsWith("CanSendAddressSpaceDataToUnifiedNamespace/Objects/Server/ServerStatus/CurrentTime",
                StringComparison.InvariantCulture)).ToList();
            var outputs = messages.Where(m => m.Topic
                .EndsWith("CanSendAddressSpaceDataToUnifiedNamespace/Objects/23:Boilers/23:Boiler \\x231/23:DrumX001/23:LIX001/23:Output",
                StringComparison.InvariantCulture)).ToList();
            Assert.NotEmpty(currentTimes);
            Assert.NotEmpty(outputs);
            if (currentTimes.Count + outputs.Count != messages.Count)
            {
                messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));
            }
            // TODO: Fix  Assert.Equal(messages.Count, currentTimes.Count + outputs.Count);
            Assert.All(currentTimes, a =>
            {
                Assert.True(a.Message.TryGetProperty("i=2258", out var dateTimeValue));
                Assert.True(dateTimeValue.TryGetDateTime(out _));
            });
            Assert.All(outputs, a =>
            {
                Assert.True(a.Message.TryGetProperty("ns=23;i=1259", out var doubleValue));
                Assert.True(doubleValue.TryGetDouble(out _));
            });
        }

        [Fact]
        public async Task CanSendAddressSpaceDataToUnifiedNamespaceRaw()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendAddressSpaceDataToUnifiedNamespaceRaw), "./Resources/DataItems1.json",
                messageCollectionTimeout: TimeSpan.FromMinutes(1), messageCount: 10,
                arguments: new string[] { "--mm=SingleRawDataSet", "--uns=UseBrowseNames" }, version: MqttVersion.v311);

            // Assert
            Assert.NotEmpty(messages);
            var currentTimes = messages.Where(m => m.Topic
                .EndsWith("CanSendAddressSpaceDataToUnifiedNamespaceRaw/Objects/Server/ServerStatus/CurrentTime",
                StringComparison.InvariantCulture)).ToList();
            var outputs = messages.Where(m => m.Topic
                .EndsWith("CanSendAddressSpaceDataToUnifiedNamespaceRaw/Objects/Boilers/Boiler \\x231/DrumX001/LIX001/Output",
                StringComparison.InvariantCulture)).ToList();
            Assert.NotEmpty(currentTimes);
            Assert.NotEmpty(outputs);
            if (currentTimes.Count + outputs.Count != messages.Count)
            {
                messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));
            }
            // TODO: Fix Assert.Equal(messages.Count, currentTimes.Count + outputs.Count);
            Assert.All(currentTimes, a =>
            {
                Assert.True(a.Message.TryGetProperty("i=2258", out var dateTimeValue));
                Assert.True(dateTimeValue.TryGetDateTime(out _));
            });
            Assert.All(outputs, a =>
            {
                Assert.True(a.Message.TryGetProperty("ns=23;i=1259", out var doubleValue));
                Assert.True(doubleValue.TryGetDouble(out _));
            });
        }

        [Fact]
        public async Task CanSendAddressSpaceDataToUnifiedNamespacePerWriterWithRawDataSets()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendAddressSpaceDataToUnifiedNamespacePerWriterWithRawDataSets), "./Resources/UnifiedNamespace.json",
                messageCollectionTimeout: TimeSpan.FromMinutes(1), messageCount: 10, version: MqttVersion.v5);

            // Assert
            Assert.NotEmpty(messages);
            var currentTimes = messages.Where(m => m.Topic
                .EndsWith("CanSendAddressSpaceDataToUnifiedNamespacePerWriterWithRawDataSets/Objects/Server/ServerStatus/CurrentTime",
                StringComparison.InvariantCulture)).ToList();
            var outputs = messages.Where(m => m.Topic
                .EndsWith("CanSendAddressSpaceDataToUnifiedNamespacePerWriterWithRawDataSets/Objects/Boilers/Boiler \\x231/DrumX001/LIX001/Output",
                StringComparison.InvariantCulture)).ToList();
            Assert.NotEmpty(currentTimes);
            Assert.NotEmpty(outputs);
            if (currentTimes.Count + outputs.Count != messages.Count)
            {
                messages.ForEach(m => _output.WriteLine(m.Topic + m.Message.ToJsonString()));
            }

            // TODO: Fix  Assert.Equal(messages.Count, currentTimes.Count + outputs.Count);
            Assert.All(currentTimes, a =>
            {
                Assert.True(a.Message.TryGetProperty("i=2258", out var dateTimeValue));
                Assert.True(dateTimeValue.TryGetDateTime(out _));
            });
            Assert.All(outputs, a =>
            {
                Assert.True(a.Message.TryGetProperty("ns=23;i=1259", out var doubleValue));
                Assert.True(doubleValue.TryGetDouble(out _));
            });
        }

        [Fact]
        public async Task CanSendModelChangeEventsToUnifiedNamespace()
        {
            // TODO: Fix
            await Task.Delay(1);
            return; // TODO FIX
#if FALSE
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendModelChangeEventsToUnifiedNamespace), "./Resources/ModelChanges.json",
                messageCollectionTimeout: TimeSpan.FromMinutes(1), messageCount: 10,
                arguments: new[] { "--mm=SingleRawDataSet", "--uns=UseBrowseNamesWithNamespaceIndex" }, version: MqttVersion.v5);

            // Assert
            Assert.NotEmpty(messages);

            var payload1 = messages[0].Message;
            _output.WriteLine(payload1.ToString());
            Assert.NotEqual(JsonValueKind.Null, payload1.ValueKind);
            Assert.True(Guid.TryParse(payload1.GetProperty("EventId").GetString(), out _));
            Assert.Equal("http://www.microsoft.com/opc-publisher#s=ReferenceChange",
                payload1.GetProperty("EventType").GetString());
            Assert.Equal("i=84", payload1.GetProperty("SourceNode").GetString());
            Assert.True(DateTime.TryParse(payload1.GetProperty("Time").GetString(), out _));
            Assert.True(payload1.GetProperty("Change").GetProperty("IsForward").GetBoolean());
            Assert.Equal("Objects", payload1.GetProperty("Change").GetProperty("DisplayName").GetString());
            Assert.EndsWith("/messages/<<UnknownWriterGroup>>", messages[0].Topic, StringComparison.Ordinal);

            var payload2 = messages[1].Message;
            _output.WriteLine(payload2.ToString());
            Assert.NotEqual(JsonValueKind.Null, payload1.ValueKind);
            Assert.True(Guid.TryParse(payload2.GetProperty("EventId").GetString(), out _));
            Assert.Equal("http://www.microsoft.com/opc-publisher#s=NodeChange",
                payload2.GetProperty("EventType").GetString());
            Assert.Equal("i=85", payload2.GetProperty("SourceNode").GetString());
            Assert.True(DateTime.TryParse(payload2.GetProperty("Time").GetString(), out _));
            Assert.Equal("Objects", payload2.GetProperty("Change").GetProperty("DisplayName").GetString());
            Assert.EndsWith("/messages/<<UnknownWriterGroup>>/Objects", messages[1].Topic, StringComparison.Ordinal);

            // TODO: currently metadata is sent later
            // Assert.NotNull(metadata);
#endif
        }
    }
}
