// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.PubSub
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.PubSub;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Logging.Abstractions;
    using Opc.Ua;
    using Opc.Ua.PubSub.Encoding;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class PubSubNotificationSinkTests
    {
        [Fact]
        public void TranslatesAllFieldsOfOneNotificationIntoOneOccurrence()
        {
            var notification = CreateNotification(MessageType.DeltaFrame,
            [
                CreateItem("counter", new DataValue(new Variant(42))),
                CreateItem("label", new DataValue(new Variant("ok")))
            ]);

            var managed = Assert.Single(PubSubNotificationSink.Translate(notification));

            //
            // The fields of one notification are the unit the writer path emits
            // as one message. Splitting them would destroy an event or condition
            // occurrence and would emit one message per changed value for data.
            //
            Assert.Equal("dataset", managed.DataSetName);
            Assert.Equal(ManagedPubSubNotificationKind.Data, managed.Kind);
            Assert.Equal(2, managed.Fields.Count);
            Assert.Equal("counter", managed.Fields[0].Name);
            Assert.Equal(42, Assert.IsType<int>(managed.Fields[0].Value.WrappedValue.Value));
            Assert.Equal("label", managed.Fields[1].Name);
            Assert.Equal("ok", Assert.IsType<string>(managed.Fields[1].Value.WrappedValue.Value));
        }

        [Fact]
        public void SeparatesRepeatedFieldNamesIntoOneOccurrenceEach()
        {
            //
            // A single writer notification can carry several occurrences, which
            // appear as repeated field names. Merging them would produce one
            // payload with duplicate keys where the writer path produces one
            // message per occurrence.
            //
            var notification = CreateNotification(MessageType.Event,
            [
                CreateItem("CycleId", new DataValue(new Variant("9"))),
                CreateItem("Severity", new DataValue(new Variant((ushort)100))),
                CreateItem("CycleId", new DataValue(new Variant("10"))),
                CreateItem("Severity", new DataValue(new Variant((ushort)200)))
            ]);

            var managed = PubSubNotificationSink.Translate(notification).ToList();

            Assert.Equal(2, managed.Count);
            Assert.All(managed, item => Assert.Equal(
                ["CycleId", "Severity"], item.Fields.Select(field => field.Name)));
            Assert.Equal("9", managed[0].Fields[0].Value.WrappedValue.Value);
            Assert.Equal("10", managed[1].Fields[0].Value.WrappedValue.Value);
        }

        [Fact]
        public void KeepsAnUnevenOccurrenceRatherThanPaddingIt()
        {
            //
            // A queued monitored item can report more values than another in
            // the same publish, so the last rounds carry fewer fields.
            //
            var notification = CreateNotification(MessageType.DeltaFrame,
            [
                CreateItem("fast", new DataValue(new Variant(1))),
                CreateItem("slow", new DataValue(new Variant(2))),
                CreateItem("fast", new DataValue(new Variant(3)))
            ]);

            var managed = PubSubNotificationSink.Translate(notification).ToList();

            Assert.Equal(2, managed.Count);
            Assert.Equal(["fast", "slow"], managed[0].Fields.Select(field => field.Name));
            Assert.Equal(["fast"], managed[1].Fields.Select(field => field.Name));
        }

        [Theory]
        [InlineData(MessageType.Event)]
        [InlineData(MessageType.Condition)]
        public void KeepsAllFieldsOfAnEventOccurrenceTogether(MessageType messageType)
        {
            //
            // An event occurrence is exactly a set of fields that belong
            // together, so it must never be split across messages.
            //
            var notification = CreateNotification(messageType,
            [
                CreateItem("EventId", new DataValue(new Variant(new byte[] { 1 }))),
                CreateItem("Severity", new DataValue(new Variant((ushort)500))),
                CreateItem("Message", new DataValue(new Variant("alarm")))
            ]);

            var managed = Assert.Single(PubSubNotificationSink.Translate(notification));

            Assert.Equal(3, managed.Fields.Count);
            Assert.Equal(["EventId", "Severity", "Message"],
                managed.Fields.Select(field => field.Name));
        }

        [Theory]
        [InlineData(MessageType.Event, ManagedPubSubNotificationKind.Event)]
        [InlineData(MessageType.Condition, ManagedPubSubNotificationKind.Condition)]
        [InlineData(MessageType.KeyFrame, ManagedPubSubNotificationKind.Data)]
        [InlineData(MessageType.DeltaFrame, ManagedPubSubNotificationKind.Data)]
        public void MapsMessageTypeToNotificationKind(MessageType messageType,
            ManagedPubSubNotificationKind expected)
        {
            var notification = CreateNotification(messageType,
                [CreateItem("field", new DataValue(new Variant(1)))]);

            var managed = Assert.Single(PubSubNotificationSink.Translate(notification));

            Assert.Equal(expected, managed.Kind);
        }

        [Theory]
        [InlineData(MessageType.KeepAlive)]
        [InlineData(MessageType.Metadata)]
        public void SkipsMessageTypesThatCarryNoFields(MessageType messageType)
        {
            var notification = CreateNotification(messageType,
                [CreateItem("field", new DataValue(new Variant(1)))]);

            Assert.Empty(PubSubNotificationSink.Translate(notification));
        }

        [Fact]
        public void SkipsNotificationsWithoutAWriterContext()
        {
            var notification = new OpcUaSubscriptionNotification(DateTimeOffset.UnixEpoch,
                notifications: [CreateItem("field", new DataValue(new Variant(1)))]);

            Assert.Empty(PubSubNotificationSink.Translate(notification));
        }

        [Fact]
        public void FallsBackFromFieldNameToIdentifierAndNodeId()
        {
            var withId = CreateItem(null, new DataValue(new Variant(1)));
            withId.Id = "identifier";
            var withNodeId = CreateItem(null, new DataValue(new Variant(2)));
            withNodeId.NodeId = "i=2258";
            var unnamed = CreateItem(null, new DataValue(new Variant(3)));

            var managed = Assert.Single(PubSubNotificationSink.Translate(
                CreateNotification(MessageType.DeltaFrame,
                    [withId, withNodeId, unnamed])));

            Assert.Equal(2, managed.Fields.Count);
            Assert.Equal("identifier", managed.Fields[0].Name);
            Assert.Equal("i=2258", managed.Fields[1].Name);
        }

        [Fact]
        public void ReportsBadNoDataWhenAFieldCarriesNoValue()
        {
            var notification = CreateNotification(MessageType.DeltaFrame,
                [CreateItem("field", null)]);

            var managed = Assert.Single(PubSubNotificationSink.Translate(notification));

            Assert.Equal(StatusCodes.BadNoData, managed.Value.StatusCode.Code);
        }

        [Fact]
        public void PreservesTheFieldStatusCode()
        {
            var notification = CreateNotification(MessageType.DeltaFrame,
            [
                CreateItem("field", new DataValue(Variant.Null,
                    StatusCodes.BadNotConnected, DateTimeUtc.From(DateTimeOffset.UnixEpoch)))
            ]);

            var managed = Assert.Single(PubSubNotificationSink.Translate(notification));

            Assert.Equal(StatusCodes.BadNotConnected, managed.Value.StatusCode.Code);
        }

        [Fact]
        public void DerivesTheDataSetNameFromTheWriterGroupWhenTheDataSetIsUnnamed()
        {
            var notification = CreateNotification(MessageType.DeltaFrame,
                [CreateItem("field", new DataValue(new Variant(1)))],
                dataSetName: null);

            var managed = Assert.Single(PubSubNotificationSink.Translate(notification));

            Assert.Equal("group:writer", managed.DataSetName);
        }

        [Fact]
        public async Task PublishesTypedFieldsThroughTheManagedSourceAsync()
        {
            var buffer = new ManagedPubSubNotificationBuffer(16);
            await using var provider = new ManagedPubSubNotificationDataSourceProvider(buffer);
            var managed = Assert.IsAssignableFrom<IManagedPubSubDataSource>(
                await provider.CreateAsync("dataset", new PublishedDataSetModel { Name = "dataset" }));
            await using var source = new ManagedPubSubDataSetSource("dataset", managed);
            source.Start();
            var metadata = source.BuildMetaData();

            await using var sink = new PubSubNotificationSink(buffer,
                NullLogger<PubSubNotificationSink>.Instance);
            sink.OnMessage(CreateNotification(MessageType.DeltaFrame,
                [CreateItem("counter", new DataValue(new Variant(7)))]));

            var field = await ReadFieldAsync(source, metadata);

            Assert.Equal("counter", field.Name);
            Assert.Equal(7, Assert.IsType<int>(field.Value.Value));
            Assert.Equal(0, sink.Dropped);
        }

        [Fact]
        public void SupportsSynchronousDisposalFromAContainerScope()
        {
            //
            // Writer group scopes are disposed synchronously; an async-only
            // disposable makes the container throw when the scope is torn down.
            //
            var buffer = new ManagedPubSubNotificationBuffer(4);
            var sink = new PubSubNotificationSink(buffer,
                NullLogger<PubSubNotificationSink>.Instance);

            sink.Dispose();
            sink.Dispose();
        }

        [Fact]
        public async Task SupportsAsynchronousDisposalAsync()
        {
            var buffer = new ManagedPubSubNotificationBuffer(4);
            var sink = new PubSubNotificationSink(buffer,
                NullLogger<PubSubNotificationSink>.Instance);

            await sink.DisposeAsync();
            await sink.DisposeAsync();
        }

        private static async Task<DataSetField> ReadFieldAsync(
            ManagedPubSubDataSetSource source, DataSetMetaDataType metadata)
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var snapshot = await source.SampleAsync(metadata);
                if (snapshot.Fields.Count != 0)
                {
                    return snapshot.Fields[0];
                }
                await Task.Delay(10);
            }
            throw new Xunit.Sdk.XunitException("The managed source did not receive a value.");
        }

        private static MonitoredItemNotificationModel CreateItem(string? fieldName,
            DataValue? value)
        {
            return new MonitoredItemNotificationModel
            {
                DataSetFieldName = fieldName,
                Value = value
            };
        }

        private static OpcUaSubscriptionNotification CreateNotification(
            MessageType messageType, IList<MonitoredItemNotificationModel> items,
            string? dataSetName = "dataset")
        {
            var writer = new DataSetWriterModel
            {
                Id = "writer",
                DataSet = new PublishedDataSetModel { Name = dataSetName }
            };
            var group = new WriterGroupModel { Id = "group" };
            return new OpcUaSubscriptionNotification(DateTimeOffset.UnixEpoch,
                notifications: items)
            {
                MessageType = messageType,
                Context = new DataSetWriterContext
                {
                    DataSetWriterId = 1,
                    Topic = "topic",
                    Qos = null,
                    PublisherId = "publisher",
                    Writer = writer,
                    WriterName = "writer",
                    MetaData = null,
                    ExtensionFields = [],
                    NextWriterSequenceNumber = () => 1,
                    WriterGroup = group,
                    Schema = null,
                    CloudEvent = null
                }
            };
        }
    }
}
