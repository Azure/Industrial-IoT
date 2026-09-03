// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="PubSubConfigurationTranslator"/>.
    /// These are pure logic tests — no OPC UA server, no network.
    /// </summary>
    public sealed class PubSubConfigurationTranslatorTests
    {
        // ── CreateEmpty ───────────────────────────────────────────────────────

        [Fact]
        public void CreateEmpty_ReturnsEnabledConfigWithNoConnectionsOrDataSets()
        {
            var config = PubSubConfigurationTranslator.CreateEmpty();

            Assert.NotNull(config);
            Assert.True(config.Enabled);
            Assert.Equal(0, config.Connections.Count);
            Assert.Equal(0, config.PublishedDataSets.Count);
        }

        // ── GetShadowEncoding ─────────────────────────────────────────────────

        [Theory]
        [InlineData(null, PubSubShadowEncoding.Json)]
        [InlineData(MessageEncoding.Json, PubSubShadowEncoding.Json)]
        [InlineData(MessageEncoding.JsonReversible, PubSubShadowEncoding.JsonReversible)]
        [InlineData(MessageEncoding.JsonGzip, PubSubShadowEncoding.JsonGzip)]
        [InlineData(MessageEncoding.JsonReversibleGzip, PubSubShadowEncoding.JsonReversibleGzip)]
        [InlineData(MessageEncoding.Uadp, PubSubShadowEncoding.Uadp)]
        public void GetShadowEncoding_ReturnsExpectedEncoding(
            MessageEncoding? input, PubSubShadowEncoding expected)
        {
            var result = PubSubConfigurationTranslator.GetShadowEncoding(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetShadowEncoding_UnsupportedEncoding_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                PubSubConfigurationTranslator.GetShadowEncoding((MessageEncoding)999));
        }

        // ── GetTransportProfile ───────────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData(MessageEncoding.Json)]
        [InlineData(MessageEncoding.JsonReversible)]
        [InlineData(MessageEncoding.JsonGzip)]
        [InlineData(MessageEncoding.JsonReversibleGzip)]
        public void GetTransportProfile_JsonEncodings_ReturnMqttProfile(
            MessageEncoding? encoding)
        {
            var result = PubSubConfigurationTranslator.GetTransportProfile(encoding);
            Assert.Equal(Profiles.PubSubMqttJsonTransport, result);
        }

        [Fact]
        public void GetTransportProfile_UadpEncoding_ReturnsUdpProfile()
        {
            var result = PubSubConfigurationTranslator.GetTransportProfile(
                MessageEncoding.Uadp);
            Assert.Equal(Profiles.PubSubUdpUadpTransport, result);
        }

        [Fact]
        public void GetTransportProfile_UnsupportedEncoding_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                PubSubConfigurationTranslator.GetTransportProfile((MessageEncoding)999));
        }

        // ── ImmediatePublishingInterval ───────────────────────────────────────

        [Fact]
        public void ImmediatePublishingInterval_IsPositiveAndShort()
        {
            var interval = PubSubConfigurationTranslator.ImmediatePublishingInterval;
            Assert.Equal(TimeSpan.FromMilliseconds(10), interval);
        }

        // ── Translate — argument validation ───────────────────────────────────

        [Fact]
        public void Translate_NullWriterGroups_ThrowsArgumentNullException()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            Assert.Throws<ArgumentNullException>(() =>
                sut.Translate(null!, tx));
        }

        [Fact]
        public void Translate_NullIdentities_ThrowsArgumentNullException()
        {
            var sut = new PubSubConfigurationTranslator();

            Assert.Throws<ArgumentNullException>(() =>
                sut.Translate([], null!));
        }

        [Fact]
        public void Translate_NullWriterGroupInCollection_ThrowsArgumentNullException()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            Assert.Throws<ArgumentNullException>(() =>
                sut.Translate([null!], tx));
        }

        [Fact]
        public void Translate_EmptyWriterGroups_ReturnsEmptyButEnabledConfig()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            var config = sut.Translate([], tx);

            Assert.NotNull(config);
            Assert.True(config.Enabled);
            Assert.Equal(0, config.Connections.Count);
        }

        // ── Translate — duplicate group ───────────────────────────────────────

        [Fact]
        public void Translate_DuplicateWriterGroupId_ThrowsArgumentException()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            var group = Group("group-1");
            Assert.Throws<ArgumentException>(() =>
                sut.Translate([group, group], tx));
        }

        // ── Translate — missing writer id ─────────────────────────────────────

        [Fact]
        public void Translate_WriterWithEmptyId_ThrowsArgumentException()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            var writer = new DataSetWriterModel { Id = "" };
            var group = new WriterGroupModel
            {
                Id = "group-1",
                DataSetWriters = [writer]
            };

            Assert.Throws<ArgumentException>(() =>
                sut.Translate([group], tx));
        }

        [Fact]
        public void Translate_DuplicateWriterIdInGroup_ThrowsArgumentException()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            var writer = new DataSetWriterModel { Id = "writer-1" };
            var group = new WriterGroupModel
            {
                Id = "group-1",
                DataSetWriters = [writer, writer]
            };

            Assert.Throws<ArgumentException>(() =>
                sut.Translate([group], tx));
        }

        // ── Translate — successful translation ────────────────────────────────

        [Fact]
        public void Translate_SingleWriterGroup_ProducesOneConnection()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            var config = sut.Translate([Group("group-1")], tx);

            Assert.Equal(1, config.Connections.Count);
        }

        [Fact]
        public void Translate_ConnectionNameContainsShadowPrefix()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            var config = sut.Translate([Group("group-1")], tx);

            Assert.StartsWith("shadow-", config.Connections[0].Name);
        }

        [Fact]
        public void Translate_JsonEncoding_UsesMqttTransportProfile()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            var group = new WriterGroupModel
            {
                Id = "group-json",
                MessageType = MessageEncoding.Json
            };
            var config = sut.Translate([group], tx);

            Assert.Equal(Profiles.PubSubMqttJsonTransport,
                config.Connections[0].TransportProfileUri);
        }

        [Fact]
        public void Translate_UadpEncoding_UsesUdpTransportProfile()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            var group = new WriterGroupModel
            {
                Id = "group-uadp",
                MessageType = MessageEncoding.Uadp
            };
            var config = sut.Translate([group], tx);

            Assert.Equal(Profiles.PubSubUdpUadpTransport,
                config.Connections[0].TransportProfileUri);
        }

        [Fact]
        public void Translate_WriterGroupWithNoPublishingInterval_UsesImmediateInterval()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            var group = new WriterGroupModel
            {
                Id = "group-1",
                PublishingInterval = null
            };
            var config = sut.Translate([group], tx);

            var writerGroup = config.Connections[0].WriterGroups[0];
            Assert.Equal(
                PubSubConfigurationTranslator.ImmediatePublishingInterval.TotalMilliseconds,
                writerGroup.PublishingInterval);
        }

        [Fact]
        public void Translate_WriterGroupWithFasterPublishingInterval_UsesConfiguredInterval()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();
            var configured = TimeSpan.FromMilliseconds(5);

            var group = new WriterGroupModel
            {
                Id = "group-1",
                PublishingInterval = configured
            };
            var config = sut.Translate([group], tx);

            var writerGroup = config.Connections[0].WriterGroups[0];
            Assert.Equal(configured.TotalMilliseconds, writerGroup.PublishingInterval);
        }

        [Fact]
        public void Translate_WriterGroupWithSlowerPublishingInterval_UsesImmediateInterval()
        {
            // Any configured interval >= ImmediatePublishingInterval is replaced
            // by ImmediatePublishingInterval so the runtime samples promptly.
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();
            var configured = TimeSpan.FromSeconds(10); // slower than immediate

            var group = new WriterGroupModel
            {
                Id = "group-1",
                PublishingInterval = configured
            };
            var config = sut.Translate([group], tx);

            var writerGroup = config.Connections[0].WriterGroups[0];
            Assert.Equal(
                PubSubConfigurationTranslator.ImmediatePublishingInterval.TotalMilliseconds,
                writerGroup.PublishingInterval);
        }

        [Fact]
        public void Translate_DataSetWriterProducesOneEntryInDataSets()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            var group = new WriterGroupModel
            {
                Id = "group-1",
                DataSetWriters =
                [
                    new DataSetWriterModel { Id = "writer-1" }
                ]
            };
            var config = sut.Translate([group], tx);

            Assert.Equal(1, config.PublishedDataSets.Count);
        }

        [Fact]
        public void Translate_SharedDataSetNameAcrossGroups_SharedDataSet()
        {
            // Two writers in different groups that name the same dataset
            // share a single PublishedDataSetDataType entry.
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            var dataSet = new PublishedDataSetModel { Name = "shared-dataset" };
            var group1 = new WriterGroupModel
            {
                Id = "group-1",
                DataSetWriters =
                [
                    new DataSetWriterModel
                    {
                        Id = "writer-1",
                        DataSet = dataSet
                    }
                ]
            };
            var group2 = new WriterGroupModel
            {
                Id = "group-2",
                DataSetWriters =
                [
                    new DataSetWriterModel
                    {
                        Id = "writer-2",
                        DataSet = dataSet
                    }
                ]
            };

            var config = sut.Translate([group1, group2], tx);

            // Both writers use the same dataset name, so only one entry
            Assert.Equal(1, config.PublishedDataSets.Count);
            Assert.Equal(2, config.Connections.Count);
        }

        // ── Activate flag ─────────────────────────────────────────────────────

        [Fact]
        public void Translate_ActivateFalse_ConnectionAndWriterGroupAreDisabled()
        {
            var sut = new PubSubConfigurationTranslator { Activate = false };
            var tx = CreateIdTransaction();

            var group = new WriterGroupModel
            {
                Id = "group-1",
                DataSetWriters =
                [
                    new DataSetWriterModel { Id = "writer-1" }
                ]
            };
            var config = sut.Translate([group], tx);

            Assert.False(config.Connections[0].Enabled);
            Assert.False(config.Connections[0].WriterGroups[0].Enabled);
            Assert.False(config.Connections[0].WriterGroups[0].DataSetWriters[0].Enabled);
        }

        [Fact]
        public void Translate_ActivateTrue_ConnectionAndWriterGroupAreEnabled()
        {
            var sut = new PubSubConfigurationTranslator { Activate = true };
            var tx = CreateIdTransaction();

            var group = new WriterGroupModel
            {
                Id = "group-1",
                DataSetWriters =
                [
                    new DataSetWriterModel { Id = "writer-1" }
                ]
            };
            var config = sut.Translate([group], tx);

            Assert.True(config.Connections[0].Enabled);
            Assert.True(config.Connections[0].WriterGroups[0].Enabled);
            Assert.True(config.Connections[0].WriterGroups[0].DataSetWriters[0].Enabled);
        }

        // ── PublisherId from options ──────────────────────────────────────────

        [Fact]
        public void Translate_PublisherIdFromOptions_IsUsedWhenGroupHasNoPublisherId()
        {
            var options = Options.Create(new PublisherOptions
            {
                PublisherId = "my-publisher"
            });
            var sut = new PubSubConfigurationTranslator(options);
            var tx = CreateIdTransaction();

            var config = sut.Translate([Group("group-1")], tx);

            var publisherId = config.Connections[0].PublisherId.Value as string;
            Assert.Equal("my-publisher", publisherId);
        }

        [Fact]
        public void Translate_GroupPublisherIdOverridesOptions()
        {
            var options = Options.Create(new PublisherOptions
            {
                PublisherId = "global-publisher"
            });
            var sut = new PubSubConfigurationTranslator(options);
            var tx = CreateIdTransaction();

            var group = new WriterGroupModel
            {
                Id = "group-1",
                PublisherId = "group-publisher"
            };
            var config = sut.Translate([group], tx);

            var publisherId = config.Connections[0].PublisherId.Value as string;
            Assert.Equal("group-publisher", publisherId);
        }

        // ── MaxNetworkMessageSize ─────────────────────────────────────────────

        [Fact]
        public void Translate_NoMaxMessageSize_DefaultsTo1500()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            var config = sut.Translate([Group("group-1")], tx);

            Assert.Equal(1500u, config.Connections[0].WriterGroups[0].MaxNetworkMessageSize);
        }

        [Fact]
        public void Translate_GroupMaxMessageSize_OverridesDefault()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            var group = new WriterGroupModel
            {
                Id = "group-1",
                MaxNetworkMessageSize = 4096
            };
            var config = sut.Translate([group], tx);

            Assert.Equal(4096u, config.Connections[0].WriterGroups[0].MaxNetworkMessageSize);
        }

        [Fact]
        public void Translate_GlobalMaxMessageSizeWithNoGroupOverride_IsUsed()
        {
            var options = Options.Create(new PublisherOptions
            {
                MaxNetworkMessageSize = 8192
            });
            var sut = new PubSubConfigurationTranslator(options);
            var tx = CreateIdTransaction();

            var config = sut.Translate([Group("group-1")], tx);

            Assert.Equal(8192u, config.Connections[0].WriterGroups[0].MaxNetworkMessageSize);
        }

        // ── TranslateWithEncodingRegistry ────────────────────────────────────

        [Fact]
        public void TranslateWithEncodingRegistry_SingleGroup_PopulatesEncodings()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            var result = sut.TranslateWithEncodingRegistry([Group("group-1")], tx);

            Assert.NotNull(result.Configuration);
            Assert.NotNull(result.Encodings);
        }

        // ── Multiple groups ───────────────────────────────────────────────────

        [Fact]
        public void Translate_MultipleGroups_ProducesMultipleConnections()
        {
            var sut = new PubSubConfigurationTranslator();
            var tx = CreateIdTransaction();

            var config = sut.Translate(
                [Group("group-1"), Group("group-2"), Group("group-3")],
                tx);

            Assert.Equal(3, config.Connections.Count);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static WriterGroupModel Group(string id) =>
            new WriterGroupModel { Id = id };

        private static IPubSubIdentityTransaction CreateIdTransaction()
        {
            return new SequentialIdTransaction();
        }

        /// <summary>
        /// Simple concrete implementation of <see cref="IPubSubIdentityTransaction"/>
        /// that hands out sequential ushort IDs.
        /// </summary>
        private sealed class SequentialIdTransaction : IPubSubIdentityTransaction
        {
            private ushort _next = 1;

            public ushort GetOrAllocate(string scope, string id) => _next++;

            public ValueTask CommitAsync(CancellationToken cancellationToken = default)
                => ValueTask.CompletedTask;

            public ValueTask DisposeAsync()
                => ValueTask.CompletedTask;
        }
    }
}
