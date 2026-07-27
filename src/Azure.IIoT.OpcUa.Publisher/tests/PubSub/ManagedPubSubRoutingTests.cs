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
            WriterGroupModel writerGroup)
        {
            var writer = writerGroup.DataSetWriters![0];
            return new OpcUaSubscriptionNotification(DateTimeOffset.UnixEpoch,
                notifications:
                [
                    new MonitoredItemNotificationModel
                    {
                        DataSetFieldName = "Output",
                        Value = new DataValue(new Variant(42))
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
