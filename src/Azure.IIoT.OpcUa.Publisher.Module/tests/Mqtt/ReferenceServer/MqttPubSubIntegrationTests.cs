// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.ReferenceServer
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.ReferenceServer;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt;
    using Json.More;
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(MqttReferenceServerCollection.Name)]
    public class MqttPubSubIntegrationTests : PublisherIntegrationTestBase, IClassFixture<ReferenceServer>
    {
        private readonly ReferenceServer _fixture;
        private readonly ITestOutputHelper _output;

        public MqttPubSubIntegrationTests(ReferenceServer fixture, ITestOutputHelper output) : base(output)
        {
            _output = output;
            _fixture = fixture;
            EndpointUrl = _fixture.EndpointUrl;
        }

        [Fact]
        public async Task CanSendDataItemToMqttBrokerTestAsync()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemToMqttBrokerTestAsync), "./Resources/DataItems.json",
                messageType: "ua-data", arguments: ["--mm=PubSub", "--mdt={TelemetryTopic}/metadatamessage", "--dm=False"],
                version: MqttVersion.v311);

            // Assert
            var message = Assert.Single(messages);
            var output = message.Message.GetProperty("Messages")[0].GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

            Assert.NotNull(metadata);
            Assert.EndsWith("/metadatamessage", metadata.Value.Topic, StringComparison.Ordinal);
        }

        [Fact]
        public async Task NativePubSubRuntimePublishesDataItemsToMqttBrokerAsync()
        {
            //
            // Preview path: the same scenario routed through the native OPC UA
            // PubSub runtime instead of the custom encoder sink. MQTT is used
            // because the native egress requires a transport that declares
            // quality of service and schema capabilities.
            //
            // Act
            var (_, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(NativePubSubRuntimePublishesDataItemsToMqttBrokerAsync),
                "./Resources/DataItems.json", TimeSpan.FromMinutes(2), 20,
                messageType: "ua-data",
                arguments: ["--mm=PubSub", "--dm=False", "--ps=False"],
                version: MqttVersion.v5);

            // Assert
            Assert.NotEmpty(messages);
            //
            // The runtime emits an initial key frame before any value has been
            // observed, so the first message carries an empty payload.
            //
            var carrying = messages
                .Select(message => message.Message)
                .First(message => message.GetProperty("Messages")[0]
                    .GetProperty("Payload").TryGetProperty("Output", out _));
            _output.WriteLine("native raw: " + carrying.ToJsonString());
            var output = carrying.GetProperty("Messages")[0]
                .GetProperty("Payload").GetProperty("Output");

            Assert.NotEqual(JsonValueKind.Undefined, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(),
                double.MinValue, double.MaxValue);
        }

        [Fact]
        public async Task CanSendDataItemButNotMetaDataWhenMetaDataIsDisabledTestAsync()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemButNotMetaDataWhenMetaDataIsDisabledTestAsync), "./Resources/DataItems.json",
                arguments: ["--dm", "--mm=DataSetMessages"],
                version: MqttVersion.v5);

            // Assert
            var message = Assert.Single(messages);
            var output = message.Message.GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

            Assert.Null(metadata);
        }

        [Fact]
        public async Task CanSendDataItemAsDataSetMessagesToMqttBrokerWithCompliantEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemAsDataSetMessagesToMqttBrokerWithCompliantEncodingTestAsync),
                "./Resources/DataItems.json", messageType: "ua-deltaframe",
                arguments: ["-c", "--mm=DataSetMessages"],
                version: MqttVersion.v311);

            // Assert
            var message = Assert.Single(messages);
            var output = message.Message.GetProperty("Payload").GetProperty("Output");
            Assert.NotEqual(JsonValueKind.Null, output.ValueKind);
            Assert.InRange(output.GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);

            Assert.NotNull(metadata);
        }

        [Fact]
        public async Task CanSendDataItemAsRawDataSetsToMqttBrokerWithCompliantEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(CanSendDataItemAsRawDataSetsToMqttBrokerWithCompliantEncodingTestAsync),
                "./Resources/DataItems.json", messageType: "ua-deltaframe",
                arguments: ["-c", "--dm=False", "--mm=RawDataSets", "--mdt"],
                version: MqttVersion.v5);

            // Assert
            var output = Assert.Single(messages);
            Assert.NotEqual(JsonValueKind.Null, output.Message.ValueKind);
            Assert.InRange(output.Message.GetProperty("Output").GetDouble(),
                double.MinValue, double.MaxValue);

            // Explicitely enabled metadata despite messaging profile
            Assert.NotNull(metadata);
            Assert.EndsWith("/metadata", metadata.Value.Topic, StringComparison.Ordinal);
        }

        [Fact]
        public async Task CanEncodeWithoutReversibleEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(nameof(CanEncodeWithoutReversibleEncodingTestAsync),
                "./Resources/SimpleEvents.json", messageType: "ua-data", arguments: ["--mm=PubSub", "--me=Json", "--dm=false"],
                version: MqttVersion.v5);

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
                var eventId = value.GetProperty(BasicPubSubIntegrationTests.EventId).GetProperty("Value");
                var message = value.GetProperty(BasicPubSubIntegrationTests.Message).GetProperty("Value");
                var cycleId = value.GetProperty(BasicPubSubIntegrationTests.CycleIdUri).GetProperty("Value");
                var currentStep = value.GetProperty(BasicPubSubIntegrationTests.CurrentStepUri).GetProperty("Value");

                Assert.Equal(JsonValueKind.String, eventId.ValueKind);
                AssertLocalizedText(message);
                Assert.Equal(JsonValueKind.String, cycleId.ValueKind);
                Assert.Equal(JsonValueKind.String, currentStep.GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, currentStep.GetProperty("Duration").ValueKind);
            });

            Assert.NotNull(metadata);
            BasicPubSubIntegrationTests.AssertSimpleEventsMetadata(metadata.Value);
        }

        [Fact]
        public async Task CanEncodeWithReversibleEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(nameof(CanEncodeWithReversibleEncodingTestAsync),
                "./Resources/SimpleEvents.json", TimeSpan.FromMinutes(2), 4, messageType: "ua-data",
                arguments: ["--mm=PubSub", "--me=JsonReversible", "--dm=False"],
                version: MqttVersion.v311);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                var body = m.GetProperty("Payload");
                var eventId = AssertDataValue(
                    body.GetProperty(BasicPubSubIntegrationTests.EventId), 15, "ByteString");
                Assert.Equal(JsonValueKind.String, eventId.ValueKind);

                var message = AssertDataValue(
                    body.GetProperty(BasicPubSubIntegrationTests.Message), 21, "LocalizedText");
                Assert.Equal(JsonValueKind.String, message.GetProperty("Text").ValueKind);
                Assert.Equal("en-US", message.GetProperty("Locale").GetString());

                var cycleId = AssertDataValue(
                    body.GetProperty(BasicPubSubIntegrationTests.CycleIdUri), 12, "String");
                Assert.Equal(JsonValueKind.String, cycleId.ValueKind);

                var currentStep = AssertDataValue(
                    body.GetProperty(BasicPubSubIntegrationTests.CurrentStepUri), 22,
                    "ExtensionObject");
                Assert.Equal(JsonValueKind.String, currentStep.GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, currentStep.GetProperty("Duration").ValueKind);
            });

            Assert.NotNull(metadata);
            BasicPubSubIntegrationTests.AssertSimpleEventsMetadata(metadata.Value);
        }

        [Fact]
        public async Task CanEncodeEventWithCompliantEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(nameof(CanEncodeEventWithCompliantEncodingTestAsync),
                "./Resources/SimpleEvents.json", messageType: "ua-data", arguments: ["-c", "--mm=PubSub", "--me=Json"],
                version: MqttVersion.v5);

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
                var eventId = value.GetProperty(BasicPubSubIntegrationTests.EventId).GetProperty("Value");
                var message = value.GetProperty(BasicPubSubIntegrationTests.Message).GetProperty("Value");
                var cycleId = value.GetProperty(BasicPubSubIntegrationTests.CycleIdExpanded).GetProperty("Value");
                var currentStep = value.GetProperty(BasicPubSubIntegrationTests.CurrentStepExpanded).GetProperty("Value");

                Assert.Equal(JsonValueKind.String, eventId.ValueKind);
                AssertLocalizedText(message);
                Assert.Equal(JsonValueKind.String, cycleId.ValueKind);
                Assert.Equal(JsonValueKind.String, currentStep.GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, currentStep.GetProperty("Duration").ValueKind);
            });

            Assert.NotNull(metadata);
            BasicPubSubIntegrationTests.AssertCompliantSimpleEventsMetadata(metadata.Value);
        }

        [Fact]
        public async Task CanEncodeWithReversibleEncodingAndWithCompliantEncodingTestAsync()
        {
            // Arrange
            // Act
            var (metadata, result) = await ProcessMessagesAndMetadataAsync(nameof(CanEncodeWithReversibleEncodingAndWithCompliantEncodingTestAsync),
                "./Resources/SimpleEvents.json", TimeSpan.FromMinutes(2), 4, messageType: "ua-data",
                arguments: ["-c", "--mm=PubSub", "--me=JsonReversible"],
                version: MqttVersion.v311);

            var messages = result
                .SelectMany(x => x.Message.GetProperty("Messages").EnumerateArray())
                .ToArray();

            // Assert
            Assert.NotEmpty(messages);
            Assert.All(messages, m =>
            {
                var body = m.GetProperty("Payload");
                var eventId = AssertDataValue(
                    body.GetProperty(BasicPubSubIntegrationTests.EventId), 15, "ByteString");
                Assert.Equal(JsonValueKind.String, eventId.ValueKind);

                var message = AssertDataValue(
                    body.GetProperty(BasicPubSubIntegrationTests.Message), 21, "LocalizedText");
                Assert.Equal(JsonValueKind.String, message.GetProperty("Text").ValueKind);
                Assert.Equal("en-US", message.GetProperty("Locale").GetString());

                var cycleId = AssertDataValue(
                    body.GetProperty(BasicPubSubIntegrationTests.CycleIdExpanded), 12, "String");
                Assert.Equal(JsonValueKind.String, cycleId.ValueKind);

                var currentStep = AssertDataValue(
                    body.GetProperty(BasicPubSubIntegrationTests.CurrentStepExpanded), 22,
                    "ExtensionObject");
                Assert.Equal(JsonValueKind.String, currentStep.GetProperty("Name").ValueKind);
                Assert.Equal(JsonValueKind.Number, currentStep.GetProperty("Duration").ValueKind);
            });

            Assert.NotNull(metadata);
            BasicPubSubIntegrationTests.AssertCompliantSimpleEventsMetadata(metadata.Value);
        }

        /// <summary>
        /// Asserts a DataValue field the way the active path spells it.
        /// </summary>
        /// <param name="field">The DataValue field object.</param>
        /// <param name="builtInType">Expected built-in type identifier.</param>
        /// <param name="builtInTypeName">Its name, which the custom encoder
        /// writes instead of the identifier unless compliant encoding is on.</param>
        private static JsonElement AssertDataValue(JsonElement field, int builtInType,
            string builtInTypeName)
        {
            return BasicPubSubIntegrationTests.AssertDataValue(field, builtInType,
                builtInTypeName);
        }

        /// <summary>
        /// Asserts a LocalizedText field the way the active path spells it.
        /// </summary>
        /// <param name="message">The `Message` field value.</param>
        private static void AssertLocalizedText(JsonElement message)
        {
            BasicPubSubIntegrationTests.AssertLocalizedText(message);
        }

        [Fact]
        public async Task CanSendPendingConditionsToMqttBrokerTestAsync()
        {
            // Arrange
            // Act
            var (metadata, messages) = await ProcessMessagesAndMetadataAsync(nameof(CanSendPendingConditionsToMqttBrokerTestAsync),
                "./Resources/PendingAlarms.json", BasicPubSubIntegrationTests.GetAlarmCondition, messageType: "ua-data",
                arguments: ["--mm=PubSub", "--dm=False"], version: MqttVersion.v311);

            // Assert
            var message = Assert.Single(messages);
            _output.WriteLine(message.Topic + message.Message.ToJsonString());

            Assert.Equal(JsonValueKind.Object, message.Message.ValueKind);
            Assert.True(message.Message.GetProperty("Payload").GetProperty("Severity").GetProperty("Value").GetInt32() >= 0);

            Assert.NotNull(metadata);
        }

        /// <summary>
        /// The native runtime publishing UADP over the broker. UADP is binary,
        /// so this asserts the path functionally rather than comparing it
        /// against the writer path: the message must arrive, decode, and carry
        /// the writer group the configuration named.
        /// </summary>
        /// <remarks>
        /// The recorded decision is that UADP is validated functionally. The
        /// cost is stated plainly: this would not catch a content mask
        /// regression of the kind the JSON parity gate caught.
        /// </remarks>
        [Fact]
        public async Task NativePubSubRuntimePublishesUadpToMqttBrokerAsync()
        {
            var messages = await ProcessRawMessagesAsync(
                nameof(NativePubSubRuntimePublishesUadpToMqttBrokerAsync),
                "./Resources/DataItems.json", TimeSpan.FromMinutes(2), 1,
                arguments: ["--mm=PubSub", "--me=Uadp", "--dm=False", "--ps=False"],
                version: MqttVersion.v5);

            var message = Assert.Single(messages);
            Assert.Equal("application/octet-stream", message.ContentType);
            Assert.NotEmpty(message.Payload);
            //
            // The low nibble of the first header byte is the UADP version,
            // which the encoder always writes as 1, so a payload that does not
            // start with it is not a UADP network message at all.
            //
            Assert.Equal(1, message.Payload[0] & 0x0F);
        }

        /// <summary>
        /// Asserts the wire shape the native runtime publishes for each retained
        /// messaging mode. Structure is compared rather than values, because
        /// timestamps, sequence numbers and identifiers legitimately differ
        /// between runs, while a missing envelope member or a value written as a
        /// bare number instead of a DataValue envelope is a real break for every
        /// consumer.
        /// </summary>
        /// <remarks>
        /// Every expected shape here was captured from the custom encoder, on
        /// this fixture, immediately before 3.0 removed it, and each was
        /// observed to match the native runtime exactly at that point. The gate
        /// used to run both paths and compare them; with only one path left the
        /// captured shape is what carries the comparison forward, and it is
        /// written out in full rather than stored as a fixture so that changing
        /// it is a visible edit to an assertion.
        ///
        /// The shapes normalise three recorded differences rather than
        /// asserting on them - see the comments on the normalisation helpers.
        /// The fixture publishes two variables so the comparison is anchored on
        /// a multi-field frame; a single-variable dataset cannot distinguish a
        /// source that publishes an occurrence from one that publishes a field.
        /// It publishes the same node twice rather than two different nodes,
        /// because the simulated nodes do not all stamp a server timestamp on
        /// every update, which makes the envelope shape differ between runs
        /// rather than between paths.
        ///
        /// DataSetMessages and SingleDataSetMessage expect the same shape. Both
        /// publish a bare data set message with no network message envelope, and
        /// the difference between them is whether several are batched into one
        /// message, which a single frame cannot show.
        /// </remarks>
        /// <param name="messagingMode"></param>
        /// <param name="expected"></param>
        [Theory]
        [InlineData("PubSub",
            "{MessageId:String,MessageType:String,Messages:[{MessageType:String," +
            "MetaDataVersion:{MajorVersion:Number,MinorVersion:Number}," +
            "Payload:{Output1:{ServerTimestamp:String,SourceTimestamp:String,Value:Number}," +
            "Output2:{ServerTimestamp:String,SourceTimestamp:String,Value:Number}}," +
            "SequenceNumber:Number,Timestamp:String}],PublisherId:String,WriterGroupName:String}")]
        [InlineData("FullNetworkMessages",
            "{MessageId:String,MessageType:String,Messages:[{DataSetWriterId:Accepted," +
            "MessageType:String,MetaDataVersion:{MajorVersion:Number,MinorVersion:Number}," +
            "Payload:{ApplicationUri:{Value:String},EndpointUrl:{Value:String}," +
            "Output1:{ServerTimestamp:String,SourceTimestamp:String,Value:Number}," +
            "Output2:{ServerTimestamp:String,SourceTimestamp:String,Value:Number}}," +
            "SequenceNumber:Number,Timestamp:String}],PublisherId:String,WriterGroupName:String}")]
        [InlineData("DataSetMessages",
            "{MessageType:String,MetaDataVersion:{MajorVersion:Number,MinorVersion:Number}," +
            "Payload:{Output1:{ServerTimestamp:String,SourceTimestamp:String,Value:Number}," +
            "Output2:{ServerTimestamp:String,SourceTimestamp:String,Value:Number}}," +
            "SequenceNumber:Number,Timestamp:String}")]
        [InlineData("SingleDataSetMessage",
            "{MessageType:String,MetaDataVersion:{MajorVersion:Number,MinorVersion:Number}," +
            "Payload:{Output1:{ServerTimestamp:String,SourceTimestamp:String,Value:Number}," +
            "Output2:{ServerTimestamp:String,SourceTimestamp:String,Value:Number}}," +
            "SequenceNumber:Number,Timestamp:String}")]
        [InlineData("RawDataSets", "{Output1:Number,Output2:Number}")]
        public async Task NativePubSubMatchesTheCustomEncoderWireShapeAsync(
            string messagingMode, string expected)
        {
            var native = await CaptureShapeAsync(messagingMode);

            _output.WriteLine("expected: " + expected);
            _output.WriteLine("native:   " + native);
            Assert.Equal(expected, native);
        }

        private async Task<string> CaptureShapeAsync(string messagingMode)
        {
            string[] arguments = ["--mm=" + messagingMode, "--dm=False", "--ps=False"];
            var (_, messages) = await ProcessMessagesAndMetadataAsync(
                nameof(NativePubSubMatchesTheCustomEncoderWireShapeAsync) + messagingMode,
                "./Resources/MultipleDataItems.json", TimeSpan.FromMinutes(2), 20,
                arguments: arguments, version: MqttVersion.v5);

            //
            // The dataset publishes two variables so that the multi-field frame
            // is actually exercised. A source that emits one field per message
            // would otherwise reproduce a single-variable dataset exactly and
            // this gate would report parity it does not have.
            //
            // The runtime may emit an initial key frame before any value has been
            // observed and a delta may legitimately carry only the field that
            // changed, so the first message carrying both fields is the one that
            // describes the wire shape.
            //
            var carrying = messages
                .Select(message => message.Message)
                .FirstOrDefault(message => FindPayload(message).ValueKind != JsonValueKind.Undefined);
            Assert.NotEqual(JsonValueKind.Undefined, carrying.ValueKind);
            _output.WriteLine("native " + messagingMode +
                " raw: " + carrying.ToJsonString());
            return Shape(carrying);
        }

        /// <summary>
        /// Finds the payload object carrying every published field, so that the
        /// comparison is anchored on a complete multi-field frame on both paths.
        /// </summary>
        /// <param name="element"></param>
        private static JsonElement FindPayload(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("Output1", out _) &&
                    element.TryGetProperty("Output2", out _))
                {
                    return element;
                }
                foreach (var property in element.EnumerateObject())
                {
                    var found = FindPayload(property.Value);
                    if (found.ValueKind != JsonValueKind.Undefined)
                    {
                        return found;
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var found = FindPayload(item);
                    if (found.ValueKind != JsonValueKind.Undefined)
                    {
                        return found;
                    }
                }
            }
            return default;
        }

        private static string Shape(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    return "{" + string.Join(",", element.EnumerateObject()
                        .Where(property => !IsAcceptedArtifact(property.Name))
                        .Select(property => (Name: Normalize(property.Name), property.Value))
                        .OrderBy(property => property.Name, StringComparer.Ordinal)
                        .Select(property => property.Name + ":" +
                            (IsAcceptedValueDifference(property.Name)
                                ? "Accepted" : Shape(property.Value)))) + "}";
                case JsonValueKind.Array:
                    //
                    // Message counts differ between runs, so only the shape of the
                    // first element is compared.
                    //
                    return element.GetArrayLength() == 0
                        ? "[]" : "[" + Shape(element[0]) + "]";
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return "Boolean";
                default:
                    return element.ValueKind.ToString();
            }
        }

        //
        // The three differences below are recorded decisions rather than
        // defects, so the comparison normalises them instead of asserting on
        // them. They are legacy versus specification differences the writer
        // path introduced and the native stack does not reproduce, and they are
        // documented as 3.0 wire changes.
        //
        //   UaType             Part 6 5.4.2.18 Table 42 defines a DataValue as
        //                      a Variant with extra fields, flattened, carrying
        //                      UaType and Value in both compact and verbose.
        //                      The writer path's Type/Body envelope is the 1.04
        //                      reversible Variant, which 1.05 replaced
        //   DataSetWriterGroup the writer path's name for the member the stack
        //                      calls WriterGroupName
        //   DataSetWriterId    written as the writer name by the writer path and
        //                      as its numeric identifier by the stack
        //
        // Two further differences are outside this gate because they are not
        // reachable from a data set of variables:
        //
        //   LocalizedText      Part 6 5.4.2.15 requires the object form with
        //                      Locale and Text unconditionally; the bare string
        //                      was the 1.04 non-reversible form
        //   ua-condition       not a Part 14 7.2.5.4 message type, so a
        //                      condition snapshot is published as the event
        //                      occurrence it is
        //
        private static bool IsAcceptedArtifact(string name)
        {
            return name == "UaType";
        }

        private static string Normalize(string name)
        {
            return name == "DataSetWriterGroup" ? "WriterGroupName" : name;
        }

        private static bool IsAcceptedValueDifference(string name)
        {
            return name == "DataSetWriterId";
        }
    }
}
