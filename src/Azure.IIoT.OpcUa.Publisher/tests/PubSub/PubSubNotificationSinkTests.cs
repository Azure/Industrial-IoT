// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.PubSub
{
    using Azure.IIoT.OpcUa.Encoders.PubSub;
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
        public void TranslatesEachFieldIntoATypedManagedNotification()
        {
            var notification = CreateNotification(MessageType.DeltaFrame,
            [
                CreateItem("counter", new DataValue(new Variant(42))),
                CreateItem("label", new DataValue(new Variant("ok")))
            ]);

            var managed = PubSubNotificationSink.Translate(notification).ToList();

            Assert.Equal(2, managed.Count);
            Assert.All(managed, item => Assert.Equal("dataset", item.DataSetName));
            Assert.All(managed, item =>
                Assert.Equal(ManagedPubSubNotificationKind.Data, item.Kind));
            Assert.Equal("counter", managed[0].FieldName);
            Assert.Equal(42, Assert.IsType<int>(managed[0].Value.WrappedValue.Value));
            Assert.Equal("label", managed[1].FieldName);
            Assert.Equal("ok", Assert.IsType<string>(managed[1].Value.WrappedValue.Value));
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

            var managed = PubSubNotificationSink.Translate(
                CreateNotification(MessageType.DeltaFrame,
                    [withId, withNodeId, unnamed])).ToList();

            Assert.Equal(2, managed.Count);
            Assert.Equal("identifier", managed[0].FieldName);
            Assert.Equal("i=2258", managed[1].FieldName);
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
                await provider.CreateAsync(new PublishedDataSetModel { Name = "dataset" }));
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
