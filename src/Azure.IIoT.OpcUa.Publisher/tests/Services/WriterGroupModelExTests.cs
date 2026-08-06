// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="WriterGroupModelEx.CopyAndResolve"/> and
    /// <see cref="WriterGroupModelEx.ResolveMessagingProfile"/>.
    /// </summary>
    public sealed class WriterGroupModelExTests
    {
        private static PublisherOptions CreateOptions() =>
            new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions().Value;

        private static DataSetWriterModel CreateVariableWriter(string id = "w1") =>
            new DataSetWriterModel
            {
                Id = id,
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel
                    {
                        PublishedVariables = new PublishedDataItemsModel
                        {
                            PublishedData =
                            [
                                new PublishedDataSetVariableModel
                                {
                                    Id = "field1",
                                    PublishedVariableNodeId = "ns=1;i=1000"
                                }
                            ]
                        }
                    }
                }
            };

        private static DataSetWriterModel CreateEmptyWriter(string id = "empty") =>
            new DataSetWriterModel { Id = id };

        private static WriterGroupModel CreateGroup(
            string id = "group",
            IList<DataSetWriterModel>? writers = null) =>
            new WriterGroupModel
            {
                Id = id,
                DataSetWriters = writers?.ToList()
            };

        // ── CopyAndResolve ─────────────────────────────────────────────────────

        [Fact]
        public void CopyAndResolve_NullModel_ThrowsArgumentNullException()
        {
            var options = CreateOptions();
            Assert.Throws<ArgumentNullException>(() =>
                WriterGroupModelEx.CopyAndResolve(null!, options));
        }

        [Fact]
        public void CopyAndResolve_NullOptions_ThrowsArgumentNullException()
        {
            var group = CreateGroup();
            Assert.Throws<ArgumentNullException>(() =>
                group.CopyAndResolve(null!));
        }

        [Fact]
        public void CopyAndResolve_NullDataSetWriters_ProducesEmptyArray()
        {
            var options = CreateOptions();
            var group = CreateGroup(writers: null);

            var result = group.CopyAndResolve(options);

            Assert.NotNull(result.DataSetWriters);
            Assert.Empty(result.DataSetWriters);
        }

        [Fact]
        public void CopyAndResolve_WritersWithNoData_FilteredOut()
        {
            var options = CreateOptions();
            var group = CreateGroup(writers: [CreateEmptyWriter("e1"), CreateEmptyWriter("e2")]);

            var result = group.CopyAndResolve(options);

            Assert.Empty(result.DataSetWriters!);
        }

        [Fact]
        public void CopyAndResolve_MixedWriters_OnlyDataWritersKept()
        {
            var options = CreateOptions();
            var group = CreateGroup(writers:
            [
                CreateVariableWriter("real"),
                CreateEmptyWriter("empty")
            ]);

            var result = group.CopyAndResolve(options);

            Assert.Single(result.DataSetWriters!);
            Assert.Equal("real", result.DataSetWriters![0].Id);
        }

        [Fact]
        public void CopyAndResolve_ReturnsDeepCopy_OriginalUnchanged()
        {
            var options = CreateOptions();
            var writer = CreateVariableWriter("w1");
            var group = CreateGroup(writers: [writer]);

            var result = group.CopyAndResolve(options);

            // The copy should be a different instance
            Assert.NotSame(group, result);
            Assert.NotSame(group.DataSetWriters![0], result.DataSetWriters![0]);
        }

        [Fact]
        public void CopyAndResolve_MaintainsGroupId()
        {
            var options = CreateOptions();
            var group = CreateGroup("my-group", [CreateVariableWriter()]);

            var result = group.CopyAndResolve(options);

            Assert.Equal("my-group", result.Id);
        }

        [Fact]
        public void CopyAndResolve_MessageTypeSetAfterResolve()
        {
            var options = CreateOptions();
            var group = CreateGroup(writers: [CreateVariableWriter()]);
            group.MessageType = null; // ensure null before resolve

            var result = group.CopyAndResolve(options);

            // MessageType should be resolved from the messaging profile
            Assert.NotNull(result.MessageType);
        }

        [Fact]
        public void CopyAndResolve_NetworkMessageContentMaskResolvedWhenNull()
        {
            var options = CreateOptions();
            var group = CreateGroup(writers: [CreateVariableWriter()]);

            var result = group.CopyAndResolve(options);

            Assert.NotNull(result.MessageSettings?.NetworkMessageContentMask);
        }

        // ── ResolveMessagingProfile ────────────────────────────────────────────

        [Fact]
        public void ResolveMessagingProfile_NullGroup_ThrowsArgumentNullException()
        {
            var options = CreateOptions();
            Assert.Throws<ArgumentNullException>(() =>
                WriterGroupModelEx.ResolveMessagingProfile(null!, options));
        }

        [Fact]
        public void ResolveMessagingProfile_NullOptions_ThrowsArgumentNullException()
        {
            var group = CreateGroup();
            Assert.Throws<ArgumentNullException>(() =>
                group.ResolveMessagingProfile(null!));
        }

        [Fact]
        public void ResolveMessagingProfile_SetsMessageTypeWhenNull()
        {
            var options = CreateOptions();
            var group = CreateGroup() with { MessageType = null };

            var result = group.ResolveMessagingProfile(options);

            Assert.NotNull(result.MessageType);
        }

        [Fact]
        public void ResolveMessagingProfile_PreservesExplicitMessageType()
        {
            var options = CreateOptions();
            var group = CreateGroup() with
            {
                MessageType = MessageEncoding.Uadp
            };

            var result = group.ResolveMessagingProfile(options);

            Assert.Equal(MessageEncoding.Uadp, result.MessageType);
        }

        [Fact]
        public void ResolveMessagingProfile_SetsNetworkMessageContentMaskWhenNull()
        {
            var options = CreateOptions();
            var group = new WriterGroupModel
            {
                Id = "g1",
                MessageSettings = new WriterGroupMessageSettingsModel
                {
                    NetworkMessageContentMask = null
                }
            };

            var result = group.ResolveMessagingProfile(options);

            Assert.NotNull(result.MessageSettings!.NetworkMessageContentMask);
        }

        [Fact]
        public void ResolveMessagingProfile_PreservesExplicitNetworkMessageContentMask()
        {
            var options = CreateOptions();
            const NetworkMessageContentFlags kCustomMask =
                NetworkMessageContentFlags.NetworkMessageHeader |
                NetworkMessageContentFlags.PublisherId;
            var group = new WriterGroupModel
            {
                Id = "g1",
                MessageSettings = new WriterGroupMessageSettingsModel
                {
                    NetworkMessageContentMask = kCustomMask
                }
            };

            var result = group.ResolveMessagingProfile(options);

            Assert.Equal(kCustomMask, result.MessageSettings!.NetworkMessageContentMask);
        }

        [Fact]
        public void ResolveMessagingProfile_SetsDataSetFieldContentMaskForWriter()
        {
            var options = CreateOptions();
            var writer = CreateVariableWriter() with { DataSetFieldContentMask = null };
            var group = CreateGroup(writers: [writer]);

            var result = group.ResolveMessagingProfile(options);

            Assert.NotNull(result.DataSetWriters![0].DataSetFieldContentMask);
        }

        [Fact]
        public void ResolveMessagingProfile_SetsDataSetMessageContentMaskForWriter()
        {
            var options = CreateOptions();
            var writer = CreateVariableWriter();
            writer.MessageSettings = null;
            var group = CreateGroup(writers: [writer]);

            var result = group.ResolveMessagingProfile(options);

            Assert.NotNull(result.DataSetWriters![0].MessageSettings?.DataSetMessageContentMask);
        }

        [Fact]
        public void ResolveMessagingProfile_WriteValueWhenSingleEntry_AddsSingleFieldFlag()
        {
            var options = CreateOptions();
            options.WriteValueWhenDataSetHasSingleEntry = true;
            var writer = CreateVariableWriter() with { DataSetFieldContentMask = null };
            var group = CreateGroup(writers: [writer]);

            var result = group.ResolveMessagingProfile(options);

            var mask = result.DataSetWriters![0].DataSetFieldContentMask;
            Assert.True(mask.HasValue &&
                mask.Value.HasFlag(DataSetFieldContentFlags.SingleFieldDegradeToValue));
        }

        [Fact]
        public void ResolveMessagingProfile_NoWriters_ReturnsGroupUnchanged()
        {
            var options = CreateOptions();
            var group = new WriterGroupModel { Id = "empty-group" };

            var result = group.ResolveMessagingProfile(options);

            Assert.Equal("empty-group", result.Id);
        }

        [Fact]
        public void ResolveMessagingProfile_ReturnsSameInstance()
        {
            var options = CreateOptions();
            var group = CreateGroup();

            var result = group.ResolveMessagingProfile(options);

            // ResolveMessagingProfile mutates and returns the same instance
            Assert.Same(group, result);
        }

        [Fact]
        public void CopyAndResolve_SecurityKeyServicesCloned()
        {
            var options = CreateOptions();
            var group = new WriterGroupModel
            {
                Id = "g1",
                SecurityKeyServices = [
                    new ConnectionModel { Endpoint = new EndpointModel { Url = "opc.tcp://ks:443" } }
                ],
                DataSetWriters = null
            };

            var result = group.CopyAndResolve(options);

            Assert.NotNull(result.SecurityKeyServices);
            Assert.Single(result.SecurityKeyServices);
            Assert.NotSame(group.SecurityKeyServices![0], result.SecurityKeyServices[0]);
        }

        [Fact]
        public void CopyAndResolve_LocaleIdsCloned()
        {
            var options = CreateOptions();
            var group = new WriterGroupModel
            {
                Id = "g1",
                LocaleIds = ["en-US", "de-DE"],
                DataSetWriters = null
            };

            var result = group.CopyAndResolve(options);

            Assert.NotNull(result.LocaleIds);
            Assert.Equal(2, result.LocaleIds.Count);
        }

        [Fact]
        public void ResolveMessagingProfile_HeaderLayoutUri_SetsWriterDataSetMaskFromProfile()
        {
            // FullNetworkMessages includes DataSetWriterId in DataSetMessageContentMask;
            // the default PubSub profile does not. Add a writer so we can observe the difference.
            var options = CreateOptions();
            var writerDefault = CreateVariableWriter("default-writer");
            var writerFull = CreateVariableWriter("full-writer");

            var defaultGroup = new WriterGroupModel
            {
                Id = "default",
                HeaderLayoutUri = null,
                DataSetWriters = [writerDefault]
            };
            var fullGroup = new WriterGroupModel
            {
                Id = "full",
                HeaderLayoutUri = nameof(MessagingMode.FullNetworkMessages),
                DataSetWriters = [writerFull]
            };

            defaultGroup.ResolveMessagingProfile(options);
            fullGroup.ResolveMessagingProfile(options);

            var defaultMask = writerDefault.MessageSettings!.DataSetMessageContentMask!.Value;
            var fullMask = writerFull.MessageSettings!.DataSetMessageContentMask!.Value;

            // FullNetworkMessages adds DataSetWriterId which PubSub omits
            Assert.True(fullMask.HasFlag(DataSetMessageContentFlags.DataSetWriterId),
                $"FullNetworkMessages should include DataSetWriterId but got: {fullMask}");
            Assert.False(defaultMask.HasFlag(DataSetMessageContentFlags.DataSetWriterId),
                $"Default PubSub should not include DataSetWriterId but got: {defaultMask}");
        }

        [Fact]
        public void ResolveMessagingProfile_HeaderLayoutUri_WithExplicitEncoding_UsesThatEncoding()
        {
            var options = CreateOptions();
            var group = new WriterGroupModel
            {
                Id = "g1",
                MessageType = MessageEncoding.Uadp,
                HeaderLayoutUri = nameof(MessagingMode.PubSub)
            };

            var result = group.ResolveMessagingProfile(options);

            // When HeaderLayoutUri is set, the explicit MessageType on the group is
            // passed as the encoding to MessagingProfile.Get; it should survive.
            Assert.Equal(MessageEncoding.Uadp, result.MessageType);
        }

        [Fact]
        public void ResolveMessagingProfile_OptionsMessagingProfile_UsedAsDefault()
        {
            // When options.MessagingProfile is explicitly set it should be used
            // instead of the hard-coded PubSub/Json fallback.
            var options = CreateOptions();
            options.MessagingProfile = MessagingProfile.Get(
                MessagingMode.FullNetworkMessages, MessageEncoding.Json);

            var group = new WriterGroupModel { Id = "g1" };
            var result = group.ResolveMessagingProfile(options);

            // The message type defaults from the supplied profile (not null)
            Assert.Equal(MessageEncoding.Json, result.MessageType);
            // The resolved mask should be the FullNetworkMessages one, not PubSub
            var expectedMask = MessagingProfile
                .Get(MessagingMode.FullNetworkMessages, MessageEncoding.Json)
                .NetworkMessageContentMask;
            Assert.Equal(expectedMask,
                result.MessageSettings?.NetworkMessageContentMask);
        }
    }
}
