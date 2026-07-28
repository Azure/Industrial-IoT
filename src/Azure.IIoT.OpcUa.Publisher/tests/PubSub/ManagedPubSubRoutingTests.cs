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
    using Opc.Ua.PubSub.DataSets;
    using Opc.Ua.PubSub.Encoding;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Composes the registry, the routing provider and the notification sink
    /// exactly as production does, so a name or routing mismatch between them
    /// is caught without standing up an OPC UA server and a broker.
    /// </summary>
    public sealed class ManagedPubSubRoutingTests
    {
        [Fact]
        public async Task RoutesNotificationsForAnUnnamedDataSetAsync()
        {
            //
            // Published nodes configurations do not name their datasets, so the
            // registry falls back to the writer group and writer identifiers.
            // The sink must enqueue under the same name or every notification
            // is silently dropped by the buffer.
            //
            await AssertRoutesAsync(dataSetName: null);
        }

        [Fact]
        public async Task RoutesNotificationsForANamedDataSetAsync()
        {
            await AssertRoutesAsync(dataSetName: "named");
        }

        [Fact]
        public async Task StampsTheFieldEncodingTheContentMaskAsksForAsync()
        {
            //
            // The native encoders take the wire shape from the field, not from
            // the writer. A source that leaves it unset emits a bare variant and
            // silently drops the status code and source timestamp the writer's
            // content mask asked for.
            //
            foreach (var (mask, expected) in new (DataSetFieldContentFlags?, PubSubFieldEncoding)[]
            {
                (null, PubSubFieldEncoding.DataValue),
                (DataSetFieldContentFlags.StatusCode, PubSubFieldEncoding.DataValue),
                (DataSetFieldContentFlags.SourceTimestamp, PubSubFieldEncoding.DataValue),
                (DataSetFieldContentFlags.RawData, PubSubFieldEncoding.RawData),
                (DataSetFieldContentFlags.RawData | DataSetFieldContentFlags.StatusCode,
                    PubSubFieldEncoding.RawData),
                ((DataSetFieldContentFlags)0, PubSubFieldEncoding.Variant)
            })
            {
                var writerGroup = CreateWriterGroup("named");
                writerGroup.DataSetWriters![0].DataSetFieldContentMask = mask;
                var buffer = new ManagedPubSubNotificationBuffer(16);
                await using var provider = new ManagedPubSubNotificationDataSourceProvider(buffer);
                await using var registry = new ManagedPubSubDataSetSourceRegistry([provider]);

                await using (var transaction = await registry.PrepareAsync([writerGroup]))
                {
                    transaction.Install();
                    await transaction.CommitAsync();
                }

                Assert.True(registry.TryGetSource("named", out var published));
                var source = Assert.IsType<ManagedPubSubDataSetSource>(published);

                await using var sink = new PubSubNotificationSink(buffer,
                    NullLogger<PubSubNotificationSink>.Instance);
                sink.OnMessage(CreateNotification(writerGroup));

                var field = await ReadFieldAsync(source, source.BuildMetaData());
                Assert.Equal(expected, field.Encoding);
            }
        }

        [Fact]
        public async Task CarriesTheSourceTimestampAndStatusOntoTheFieldAsync()
        {
            //
            // The stack writes a DataValue flattened as
            // {UaType, Value, StatusCode, SourceTimestamp} and omits the members
            // that are still at their default. A field that loses the source
            // timestamp is therefore indistinguishable on the wire from a bare
            // variant, which is exactly the difference the parity gate reports
            // against the custom encoder.
            //
            var sourceTimestamp = new DateTimeOffset(2026, 3, 4, 5, 6, 7,
                TimeSpan.Zero);
            var writerGroup = CreateWriterGroup("named");
            var buffer = new ManagedPubSubNotificationBuffer(16);
            await using var provider = new ManagedPubSubNotificationDataSourceProvider(buffer);
            await using var registry = new ManagedPubSubDataSetSourceRegistry([provider]);

            await using (var transaction = await registry.PrepareAsync([writerGroup]))
            {
                transaction.Install();
                await transaction.CommitAsync();
            }

            Assert.True(registry.TryGetSource("named", out var published));
            var source = Assert.IsType<ManagedPubSubDataSetSource>(published);

            await using var sink = new PubSubNotificationSink(buffer,
                NullLogger<PubSubNotificationSink>.Instance);
            sink.OnMessage(CreateNotification(writerGroup, sourceTimestamp));

            var delta = await ReadFieldAsync(source, source.BuildMetaData());
            Assert.Equal(PubSubFieldEncoding.DataValue, delta.Encoding);
            Assert.Equal(DateTimeUtc.From(sourceTimestamp), delta.SourceTimestamp);
            Assert.Equal(StatusCodes.Good, delta.StatusCode);

            //
            // A key frame is built from the retained current values rather than
            // from the pending notification, so it has to preserve the same
            // members independently.
            //
            source.RequestKeyFrame();
            var keyFrame = Assert.Single(
                (await source.SampleAsync(source.BuildMetaData())).Fields.AsEnumerable());
            Assert.Equal(PubSubFieldEncoding.DataValue, keyFrame.Encoding);
            Assert.Equal(DateTimeUtc.From(sourceTimestamp), keyFrame.SourceTimestamp);
        }

        private static async Task AssertRoutesAsync(string? dataSetName)
        {
            var writerGroup = CreateWriterGroup(dataSetName);
            var buffer = new ManagedPubSubNotificationBuffer(16);
            await using var provider = new ManagedPubSubNotificationDataSourceProvider(buffer);
            await using var registry = new ManagedPubSubDataSetSourceRegistry([provider]);

            await using (var transaction = await registry.PrepareAsync([writerGroup]))
            {
                transaction.Install();
                await transaction.CommitAsync();
            }

            var expected = dataSetName ?? "group:writer";
            Assert.True(registry.TryGetSource(expected, out var published),
                $"The registry created no source for '{expected}'.");
            var source = Assert.IsType<ManagedPubSubDataSetSource>(published);

            await using var sink = new PubSubNotificationSink(buffer,
                NullLogger<PubSubNotificationSink>.Instance);
            sink.OnMessage(CreateNotification(writerGroup));

            var field = await ReadFieldAsync(source, source.BuildMetaData());

            Assert.Equal("Output", field.Name);
            Assert.Equal(42, Assert.IsType<int>(field.Value.Value));
        }

        private static async Task<DataSetField> ReadFieldAsync(
            ManagedPubSubDataSetSource source, DataSetMetaDataType metadata)
        {
            for (var attempt = 0; attempt < 200; attempt++)
            {
                var snapshot = await source.SampleAsync(metadata);
                if (snapshot.Fields.Count != 0)
                {
                    return snapshot.Fields[0];
                }
                await Task.Delay(10);
            }
            throw new Xunit.Sdk.XunitException(
                "The notification never reached the managed data source.");
        }

        private static WriterGroupModel CreateWriterGroup(string? dataSetName)
        {
            return new WriterGroupModel
            {
                Id = "group",
                DataSetWriters =
                [
                    new DataSetWriterModel
                    {
                        Id = "writer",
                        DataSet = new PublishedDataSetModel { Name = dataSetName }
                    }
                ]
            };
        }

        private static OpcUaSubscriptionNotification CreateNotification(
            WriterGroupModel writerGroup, DateTimeOffset? sourceTimestamp = null)
        {
            var writer = writerGroup.DataSetWriters![0];
            return new OpcUaSubscriptionNotification(DateTimeOffset.UnixEpoch,
                notifications:
                [
                    new MonitoredItemNotificationModel
                    {
                        DataSetFieldName = "Output",
                        Value = sourceTimestamp is { } timestamp
                            ? new DataValue(new Variant(42), StatusCodes.Good,
                                DateTimeUtc.From(timestamp))
                            : new DataValue(new Variant(42))
                    }
                ])
            {
                MessageType = MessageType.DeltaFrame,
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
                    WriterGroup = writerGroup,
                    Schema = null,
                    CloudEvent = null
                }
            };
        }
    }
}
