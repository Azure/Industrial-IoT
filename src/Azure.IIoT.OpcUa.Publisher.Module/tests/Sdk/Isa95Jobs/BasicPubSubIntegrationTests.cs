// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.Isa95Jobs
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using FluentAssertions;
    using Google.Protobuf.WellKnownTypes;
    using Json.More;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class BasicPubSubIntegrationTests : PublisherIntegrationTestBase
    {
        internal const string EventId = "EventId";
        internal const string Message = "Message";
        internal const string JobResponseExpanded = "nsu=http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/;JobResponse";
        internal const string JobStateExpanded = "nsu=http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/;JobState";
        internal const string JobResponseUri = "http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/#JobResponse";
        internal const string JobStateUri = "http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/#JobState";
        private readonly ITestOutputHelper _output;
        private readonly Isa95JobsServer _fixture;

        public BasicPubSubIntegrationTests(ITestOutputHelper output)
            : base(output)
        {
            _output = output;
            _fixture = new Isa95JobsServer();
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
        public async Task CanEncodeWithReversibleEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                nameof(CanEncodeWithReversibleEncodingTestAsync),
                "./Resources/Isa95Jobs.json", TimeSpan.FromMinutes(2), 4, messageType: "ua-data",
                arguments: ["--mm=PubSub", "--me=JsonReversible", "--dm=false"]
            );

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                var payload = m.GetProperty("Payload");
                var eventId = payload.GetProperty(EventId).GetProperty("Value");
                Assert.Equal("ByteString", eventId.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, eventId.GetProperty("Body").ValueKind);

                var message = payload.GetProperty(Message).GetProperty("Value");
                Assert.Equal("LocalizedText", message.GetProperty("Type").GetString());
                Assert.Equal(JsonValueKind.String, message.GetProperty("Body").GetProperty("Text").ValueKind);
                Assert.Equal("en-US", message.GetProperty("Body").GetProperty("Locale").GetString());

                var jobState = payload.GetProperty(JobStateUri).GetProperty("Value");
                Assert.Equal("ExtensionObject", jobState.GetProperty("Type").GetString());
                var jobResponse = payload.GetProperty(JobResponseUri).GetProperty("Value");
                Assert.Equal("ExtensionObject", jobResponse.GetProperty("Type").GetString());

                var extensionObject = jobResponse.GetProperty("Body");
                Assert.Equal("http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/#i=3013", extensionObject.GetProperty("TypeId").GetString());
                Assert.Equal("Json", extensionObject.GetProperty("Encoding").GetString());
                var body = extensionObject.GetProperty("Body");
                var equipmentActuals = body.GetProperty("EquipmentActuals");
                var materialActuals = body.GetProperty("MaterialActuals");
                Assert.Equal(JsonValueKind.Array, equipmentActuals.ValueKind);
                Assert.Equal(JsonValueKind.Array, materialActuals.ValueKind);

                Assert.Equal(2, equipmentActuals.GetArrayLength());
                Assert.Equal(2, materialActuals.GetArrayLength());

                Assert.All(equipmentActuals.EnumerateArray(), e =>
                {
                    Assert.Equal(JsonValueKind.String, e.GetProperty("EquipmentUse").ValueKind);
                    Assert.Equal("consumable", e.GetProperty("EquipmentUse").GetString());
                    Assert.Equal(JsonValueKind.String, e.GetProperty("Quantity").ValueKind);
                });
                Assert.All(materialActuals.EnumerateArray(), e =>
                {
                    Assert.Equal(JsonValueKind.String, e.GetProperty("MaterialClassID").ValueKind);
                    Assert.True(e.GetProperty("MaterialClassID").TryGetGuid(out _));
                    Assert.Equal(JsonValueKind.String, e.GetProperty("MaterialUse").ValueKind);
                    Assert.Equal("consumable", e.GetProperty("MaterialUse").GetString());
                    Assert.Equal(JsonValueKind.String, e.GetProperty("Quantity").ValueKind);
                });

                extensionObject = jobState.GetProperty("Body");
                Assert.Equal("http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/#i=3015", extensionObject.GetProperty("TypeId").GetString());
                Assert.Equal("Json", extensionObject.GetProperty("Encoding").GetString());
                body = extensionObject.GetProperty("Body");
                var state = body.GetProperty("State");
                Assert.Equal(JsonValueKind.Array, state.ValueKind);
                Assert.Equal(3, state.GetArrayLength());
                Assert.All(state.EnumerateArray(), e =>
                {
                    Assert.Equal(JsonValueKind.Array, e.GetProperty("BrowsePath").GetProperty("Elements").ValueKind);
                    Assert.True(e.GetProperty("StateNumber").TryGetInt32(out _));
                    Assert.Equal(JsonValueKind.String, e.GetProperty("StateText").GetProperty("Text").ValueKind);
                    Assert.Equal("en-US", e.GetProperty("StateText").GetProperty("Locale").GetString());
                });
            });
        }

        [Fact]
        public async Task CanEncodeEventWithCompliantEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                nameof(CanEncodeEventWithCompliantEncodingTestAsync),
                "./Resources/Isa95Jobs.json", messageType: "ua-data",
                arguments: ["-c", "--mm=PubSub", "--me=Json"]);

            Assert.Single(result);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                var value = m.GetProperty("Payload");

                // Variant encoding is the default
                var eventId = value.GetProperty(EventId).GetProperty("Value");
                var message = value.GetProperty(Message).GetProperty("Value");
                var jobResponse = value.GetProperty(JobResponseExpanded).GetProperty("Value");

                var equipmentActuals = jobResponse.GetProperty("EquipmentActuals");
                var materialActuals = jobResponse.GetProperty("MaterialActuals");

                Assert.Equal(JsonValueKind.String, eventId.ValueKind);
                Assert.Equal(JsonValueKind.String, message.ValueKind);
                Assert.Equal(JsonValueKind.Array, equipmentActuals.ValueKind);
                Assert.Equal(JsonValueKind.Array, materialActuals.ValueKind);

                Assert.Equal(2, equipmentActuals.GetArrayLength());
                Assert.Equal(2, materialActuals.GetArrayLength());

                Assert.All(equipmentActuals.EnumerateArray(), e =>
                {
                    Assert.Equal(JsonValueKind.String, e.GetProperty("EquipmentUse").ValueKind);
                    Assert.Equal("consumable", e.GetProperty("EquipmentUse").GetString());
                    Assert.Equal(JsonValueKind.String, e.GetProperty("Quantity").ValueKind);
                });
                Assert.All(materialActuals.EnumerateArray(), e =>
                {
                    Assert.Equal(JsonValueKind.String, e.GetProperty("MaterialClassID").ValueKind);
                    Assert.True(e.GetProperty("MaterialClassID").TryGetGuid(out _));
                    Assert.Equal(JsonValueKind.String, e.GetProperty("MaterialUse").ValueKind);
                    Assert.Equal("consumable", e.GetProperty("MaterialUse").GetString());
                    Assert.Equal(JsonValueKind.String, e.GetProperty("Quantity").ValueKind);
                });

                var jobState = value.GetProperty(JobStateExpanded).GetProperty("Value");
                var state = jobState.GetProperty("State");
                Assert.Equal(JsonValueKind.Array, state.ValueKind);
                Assert.Equal(3, state.GetArrayLength());
                Assert.All(state.EnumerateArray(), e =>
                {
                    Assert.Equal(JsonValueKind.Array, e.GetProperty("BrowsePath").GetProperty("Elements").ValueKind);
                    Assert.True(e.GetProperty("StateNumber").TryGetInt32(out _));
                    Assert.Equal(JsonValueKind.String, e.GetProperty("StateText").ValueKind);
                });
            });
        }

        [Fact]
        public async Task CanEncodeWithReversibleEncodingAndWithCompliantEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(
                nameof(CanEncodeWithReversibleEncodingAndWithCompliantEncodingTestAsync),
                "./Resources/Isa95Jobs.json", TimeSpan.FromMinutes(2), 4, messageType: "ua-data",
                arguments: ["-c", "--mm=PubSub", "--me=JsonReversible"]);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                var payload = m.GetProperty("Payload");
                var eventId = payload.GetProperty(EventId).GetProperty("Value");
                Assert.Equal(15, eventId.GetProperty("Type").GetInt32());
                Assert.Equal(JsonValueKind.String, eventId.GetProperty("Body").ValueKind);

                var message = payload.GetProperty(Message).GetProperty("Value");
                Assert.Equal(21, message.GetProperty("Type").GetInt32());
                Assert.Equal(JsonValueKind.String, message.GetProperty("Body").GetProperty("Text").ValueKind);
                Assert.Equal("en-US", message.GetProperty("Body").GetProperty("Locale").GetString());

                var jobResponse = payload.GetProperty(JobResponseExpanded).GetProperty("Value");
                Assert.Equal(22, jobResponse.GetProperty("Type").GetInt32());
                var jobState = payload.GetProperty(JobStateExpanded).GetProperty("Value");
                Assert.Equal(22, jobState.GetProperty("Type").GetInt32());

                var extensionObject = jobResponse.GetProperty("Body");
                Assert.Equal(3013, extensionObject.GetProperty("TypeId").GetProperty("Id").GetInt32());
                Assert.Equal(2, extensionObject.GetProperty("TypeId").GetProperty("Namespace").GetInt32());
                var body = extensionObject.GetProperty("Body");
                var equipmentActuals = body.GetProperty("EquipmentActuals");
                var materialActuals = body.GetProperty("MaterialActuals");
                Assert.Equal(JsonValueKind.Array, equipmentActuals.ValueKind);
                Assert.Equal(JsonValueKind.Array, materialActuals.ValueKind);

                Assert.Equal(2, equipmentActuals.GetArrayLength());
                Assert.Equal(2, materialActuals.GetArrayLength());

                Assert.All(equipmentActuals.EnumerateArray(), e =>
                {
                    Assert.Equal(JsonValueKind.String, e.GetProperty("EquipmentUse").ValueKind);
                    Assert.Equal("consumable", e.GetProperty("EquipmentUse").GetString());
                    Assert.Equal(JsonValueKind.String, e.GetProperty("Quantity").ValueKind);
                });
                Assert.All(materialActuals.EnumerateArray(), e =>
                {
                    Assert.Equal(JsonValueKind.String, e.GetProperty("MaterialClassID").ValueKind);
                    Assert.True(e.GetProperty("MaterialClassID").TryGetGuid(out _));
                    Assert.Equal(JsonValueKind.String, e.GetProperty("MaterialUse").ValueKind);
                    Assert.Equal("consumable", e.GetProperty("MaterialUse").GetString());
                    Assert.Equal(JsonValueKind.String, e.GetProperty("Quantity").ValueKind);
                });

                extensionObject = jobState.GetProperty("Body");
                Assert.Equal(3015, extensionObject.GetProperty("TypeId").GetProperty("Id").GetInt32());
                Assert.Equal(2, extensionObject.GetProperty("TypeId").GetProperty("Namespace").GetInt32());
                body = extensionObject.GetProperty("Body");
                var state = body.GetProperty("State");
                Assert.Equal(JsonValueKind.Array, state.ValueKind);
                Assert.Equal(3, state.GetArrayLength());
                Assert.All(state.EnumerateArray(), e =>
                {
                    Assert.Equal(JsonValueKind.Array, e.GetProperty("BrowsePath").GetProperty("Elements").ValueKind);
                    Assert.True(e.GetProperty("StateNumber").TryGetInt32(out _));
                    Assert.Equal(JsonValueKind.String, e.GetProperty("StateText").GetProperty("Text").ValueKind);
                    Assert.Equal("en-US", e.GetProperty("StateText").GetProperty("Locale").GetString());
                });
            });
        }
    }
}
