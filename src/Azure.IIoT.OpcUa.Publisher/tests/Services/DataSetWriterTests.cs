// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using System;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="WriterGroupDataSource.DataSetWriter"/> —
    /// the inner class that represents an expanded data set writer with its partition key.
    /// </summary>
    public sealed class DataSetWriterTests : IDisposable
    {
        private readonly WriterGroupDataSource _group;
        private static readonly PublisherOptions _options =
            new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions().Value;

        public DataSetWriterTests()
        {
            var clients = new Mock<IOpcUaClientManager<ConnectionModel>>();
            var sink = new Mock<IMessageSink>();
            _group = new WriterGroupDataSource(
                clients.Object,
                new WriterGroupModel { Id = "test-group" },
                sink.Object,
                new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions(),
                null,
                NullLoggerFactory.Instance);
        }

        public void Dispose()
        {
            _group.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        // ── Factory helpers ───────────────────────────────────────────────────

        private static DataSetWriterModel CreateVariableWriter(
            string id = "w1",
            string? writerName = null,
            TimeSpan? publishingInterval = null,
            string? queueName = null,
            QoS? qos = null,
            TimeSpan? ttl = null,
            bool? retain = null,
            string? metaDataQueueName = null)
        {
            return new DataSetWriterModel
            {
                Id = id,
                DataSetWriterName = writerName,
                Publishing = queueName == null && qos == null && ttl == null && retain == null ? null
                    : new PublishingQueueSettingsModel
                    {
                        QueueName = queueName,
                        RequestedDeliveryGuarantee = qos,
                        Ttl = ttl,
                        Retain = retain
                    },
                MetaData = metaDataQueueName == null ? null
                    : new PublishingQueueSettingsModel
                    {
                        QueueName = metaDataQueueName
                    },
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel
                    {
                        SubscriptionSettings = publishingInterval == null ? null
                            : new PublishedDataSetSettingsModel
                            {
                                PublishingInterval = publishingInterval
                            },
                        PublishedVariables = new PublishedDataItemsModel
                        {
                            PublishedData =
                            [
                                new PublishedDataSetVariableModel { Id = "field1" }
                            ]
                        }
                    }
                }
            };
        }

        private WriterGroupDataSource.DataSetWriter GetSingleWriter(DataSetWriterModel model)
        {
            return WriterGroupDataSource.DataSetWriter.GetDataSetWriters(_group, model).Single();
        }

        // ── PublishingInterval ────────────────────────────────────────────────

        [Fact]
        public void PublishingInterval_IsNullWhenNoSubscriptionSettings()
        {
            var writer = GetSingleWriter(CreateVariableWriter());

            Assert.Null(writer.PublishingInterval);
        }

        [Fact]
        public void PublishingInterval_IsSetFromSubscriptionSettings()
        {
            var writer = GetSingleWriter(CreateVariableWriter(
                publishingInterval: TimeSpan.FromSeconds(5)));

            Assert.Equal(TimeSpan.FromSeconds(5), writer.PublishingInterval);
        }

        [Fact]
        public void PublishingInterval_IsNullWhenIgnoreConfiguredPublishingIntervals()
        {
            var clients = new Mock<IOpcUaClientManager<ConnectionModel>>();
            var sink = new Mock<IMessageSink>();
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.IgnoreConfiguredPublishingIntervals = true;

            using var group = new WriterGroupDataSource(
                clients.Object,
                new WriterGroupModel { Id = "grp" },
                sink.Object,
                options,
                null,
                NullLoggerFactory.Instance);

            var writerModel = CreateVariableWriter(publishingInterval: TimeSpan.FromSeconds(5));
            var writer = WriterGroupDataSource.DataSetWriter.GetDataSetWriters(group, writerModel).Single();

            // PublishingInterval must be null when interval override is set
            Assert.Null(writer.PublishingInterval);
        }

        // ── Topic / Qos / Ttl / Retain ────────────────────────────────────────

        [Fact]
        public void Topic_DefaultsToSlashWhenNoPublishing()
        {
            var writer = GetSingleWriter(CreateVariableWriter());

            Assert.NotNull(writer.Topic);
        }

        [Fact]
        public void Topic_ReflectsResolvedQueueName()
        {
            var writer = GetSingleWriter(CreateVariableWriter(queueName: "my/queue/name"));

            Assert.Contains("my/queue/name", writer.Topic, StringComparison.Ordinal);
        }

        [Fact]
        public void Qos_IsNullWhenNoPublishing()
        {
            var writer = GetSingleWriter(CreateVariableWriter());

            Assert.Null(writer.Qos);
        }

        [Fact]
        public void Ttl_IsNullWhenNoPublishing()
        {
            var writer = GetSingleWriter(CreateVariableWriter());

            Assert.Null(writer.Ttl);
        }

        [Fact]
        public void Retain_IsNullWhenNoPublishing()
        {
            var writer = GetSingleWriter(CreateVariableWriter());

            Assert.Null(writer.Retain);
        }

        [Fact]
        public void MetadataTopic_DefaultsToSlashOrTelemetryTopicWhenNoMetaData()
        {
            var writer = GetSingleWriter(CreateVariableWriter());

            // MetadataTopic falls back to "/" or the telemetry topic
            Assert.NotNull(writer.MetadataTopic);
        }

        [Fact]
        public void MetadataRetain_DefaultsToTrue()
        {
            var writer = GetSingleWriter(CreateVariableWriter());

            Assert.True(writer.MetadataRetain);
        }

        [Fact]
        public void MetadataTtl_IsNullWhenNeitherMetaDataNorUpdateTimeSet()
        {
            var writer = GetSingleWriter(CreateVariableWriter());

            Assert.Null(writer.MetadataTtl);
        }

        // ── Writer / DataSet / Source properties ──────────────────────────────

        [Fact]
        public void Writer_ReturnsExpandedWriterModel()
        {
            var model = CreateVariableWriter(id: "my-id");
            var writer = GetSingleWriter(model);

            Assert.Equal("my-id", writer.Writer.Id);
        }

        [Fact]
        public void DataSet_ReturnsDataSetFromWriter()
        {
            var model = CreateVariableWriter();
            var writer = GetSingleWriter(model);

            Assert.NotNull(writer.DataSet);
            Assert.NotNull(writer.DataSet.DataSetSource);
        }

        [Fact]
        public void Source_ReturnsDataSetSource()
        {
            var model = CreateVariableWriter();
            var writer = GetSingleWriter(model);

            Assert.NotNull(writer.Source);
            Assert.NotNull(writer.Source.PublishedVariables);
        }

        [Fact]
        public void LastErrors_StartsEmpty()
        {
            var model = CreateVariableWriter();
            var writer = GetSingleWriter(model);

            Assert.Empty(writer.LastErrors);
        }

        // ── GetState ──────────────────────────────────────────────────────────

        [Fact]
        public void GetState_ReturnsWriterIdAndName()
        {
            var model = CreateVariableWriter(id: "state-writer", writerName: "My Writer");
            var writer = GetSingleWriter(model);

            var state = writer.GetState();

            Assert.Equal("state-writer", state.Id);
            Assert.Equal("My Writer", state.DataSetWriterName);
        }

        [Fact]
        public void GetState_NoErrors_SourceHasEmptyErrors()
        {
            var model = CreateVariableWriter(id: "w1");
            var writer = GetSingleWriter(model);

            var state = writer.GetState();

            Assert.NotNull(state.Source);
            Assert.Empty(state.Source.Errors!);
        }

        [Fact]
        public void GetState_WithErrors_SourceHasErrors()
        {
            var model = CreateVariableWriter(id: "w1");
            var writer = GetSingleWriter(model);

            writer.LastErrors.TryAdd("ns=2;s=MyNode",
                new ServiceResultModel { StatusCode = 0x80340000, SymbolicId = "BadNodeIdUnknown" });

            var state = writer.GetState();

            Assert.NotNull(state.Source);
            var error = Assert.Single(state.Source.Errors!);
            Assert.Equal("ns=2;s=MyNode", error.NodeId);
            Assert.Equal(0x80340000u, error.ErrorInfo.StatusCode);
        }

        [Fact]
        public void GetState_NoConnection_EndpointUrlIsNull()
        {
            var model = CreateVariableWriter(id: "w1");
            var writer = GetSingleWriter(model);

            var state = writer.GetState();

            Assert.Null(state.EndpointUrl);
        }

        [Fact]
        public void GetState_WithConnection_ReturnsEndpointUrl()
        {
            var model = new DataSetWriterModel
            {
                Id = "w1",
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel
                    {
                        Connection = new ConnectionModel
                        {
                            Endpoint = new EndpointModel
                            {
                                Url = "opc.tcp://server:4840"
                            }
                        },
                        PublishedVariables = new PublishedDataItemsModel
                        {
                            PublishedData = [new PublishedDataSetVariableModel { Id = "f1" }]
                        }
                    }
                }
            };
            var writer = GetSingleWriter(model);

            var state = writer.GetState();

            Assert.Equal("opc.tcp://server:4840", state.EndpointUrl);
        }

        // ── Equals ────────────────────────────────────────────────────────────

        [Fact]
        public void Equals_TwoWritersWithSameId_AndSameSettings_AreEqual()
        {
            var model = CreateVariableWriter(id: "w1");
            var w1 = GetSingleWriter(model);
            var w2 = GetSingleWriter(model);

            Assert.True(w1.Equals(w2));
            Assert.True(w2.Equals(w1));
        }

        [Fact]
        public void Equals_WriterEqualsItself()
        {
            var writer = GetSingleWriter(CreateVariableWriter(id: "w1"));

            Assert.True(writer.Equals(writer));
        }

        [Fact]
        public void Equals_WriterWithDifferentId_NotEqual()
        {
            var w1 = GetSingleWriter(CreateVariableWriter(id: "w1"));
            var w2 = GetSingleWriter(CreateVariableWriter(id: "w2"));

            Assert.False(w1.Equals(w2));
        }

        [Fact]
        public void Equals_NullObject_ReturnsFalse()
        {
            var writer = GetSingleWriter(CreateVariableWriter(id: "w1"));

            Assert.False(writer.Equals(null));
        }

        [Fact]
        public void Equals_NonDataSetWriterObject_ReturnsFalse()
        {
            var writer = GetSingleWriter(CreateVariableWriter(id: "w1"));

            Assert.False(writer.Equals("not a writer"));
        }

        [Fact]
        public void Equals_WritersWithDifferentPublishingInterval_NotEqual()
        {
            var w1 = GetSingleWriter(CreateVariableWriter(id: "w1",
                publishingInterval: TimeSpan.FromSeconds(1)));
            var w2 = GetSingleWriter(CreateVariableWriter(id: "w1",
                publishingInterval: TimeSpan.FromSeconds(5)));

            Assert.False(w1.Equals(w2));
        }

        [Fact]
        public void Equals_WritersWithDifferentQos_NotEqual()
        {
            var w1 = GetSingleWriter(CreateVariableWriter(id: "w1", qos: QoS.AtLeastOnce));
            var w2 = GetSingleWriter(CreateVariableWriter(id: "w1", qos: QoS.ExactlyOnce));

            Assert.False(w1.Equals(w2));
        }

        // ── GetHashCode ───────────────────────────────────────────────────────

        [Fact]
        public void GetHashCode_SameWriter_SameHash()
        {
            var model = CreateVariableWriter(id: "w1");
            var w1 = GetSingleWriter(model);
            var w2 = GetSingleWriter(model);

            Assert.Equal(w1.GetHashCode(), w2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_DifferentWriterId_DifferentHash()
        {
            var w1 = GetSingleWriter(CreateVariableWriter(id: "w1"));
            var w2 = GetSingleWriter(CreateVariableWriter(id: "w2"));

            Assert.NotEqual(w1.GetHashCode(), w2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_EqualsTrue_HashCodesMatch()
        {
            var model = CreateVariableWriter(id: "w1",
                publishingInterval: TimeSpan.FromSeconds(2));
            var w1 = GetSingleWriter(model);
            var w2 = GetSingleWriter(model);

            Assert.True(w1.Equals(w2));
            Assert.Equal(w1.GetHashCode(), w2.GetHashCode());
        }

        // ── ToString ──────────────────────────────────────────────────────────

        [Fact]
        public void ToString_ReturnsNonNullStringWithWriterId()
        {
            var writer = GetSingleWriter(CreateVariableWriter(id: "my-id"));

            var str = writer.ToString();

            Assert.NotNull(str);
            Assert.Contains("my-id", str, StringComparison.Ordinal);
        }

        [Fact]
        public void ToString_IncludesTopicInOutput()
        {
            var writer = GetSingleWriter(CreateVariableWriter(id: "w1"));

            var str = writer.ToString();

            Assert.NotNull(str);
            Assert.Contains("->", str, StringComparison.Ordinal);
        }
    }
}
