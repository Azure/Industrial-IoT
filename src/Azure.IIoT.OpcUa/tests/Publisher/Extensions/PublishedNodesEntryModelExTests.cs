// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Config.Models
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using Xunit;

    public sealed class PublishedNodesEntryModelExTests
    {
        [Fact]
        public void HasSameWriterGroupReturnsTrueForSameReference()
        {
            var entry = CreateEntry();

            Assert.Equal(true, entry.HasSameWriterGroup(entry));
        }

        [Fact]
        public void HasSameWriterGroupTreatsDefaultedStringsAsEmpty()
        {
            var first = new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://localhost:50000",
                PublisherId = null,
                WriterGroupType = null,
                WriterGroupRootNodeId = null,
                WriterGroupTransportConfiguration = null,
                WriterGroupQueueName = null
            };
            var second = first with
            {
                PublisherId = string.Empty,
                WriterGroupType = string.Empty,
                WriterGroupRootNodeId = string.Empty,
                WriterGroupTransportConfiguration = string.Empty,
                WriterGroupQueueName = string.Empty
            };

            Assert.Equal(true, first.HasSameWriterGroup(second));
            Assert.Equal(true, second.HasSameWriterGroup(first));
        }

        [Theory]
        [MemberData(nameof(GetDifferentWriterGroups))]
        public void HasSameWriterGroupReturnsFalseForSingleFieldDifference(string name,
            Action<PublishedNodesEntryModel> mutate)
        {
            var first = CreateEntry();
            var second = CreateEntry();
            mutate(second);

            Assert.False(first.HasSameWriterGroup(second));
            Assert.False(second.HasSameWriterGroup(first));
        }

        [Fact]
        public void HasSameDataSetReturnsTrueForEquivalentEntries()
        {
            var first = CreateEntry();
            var second = CreateEntry();

            Assert.Equal(true, first.HasSameDataSet(second));
            Assert.Equal(true, second.HasSameDataSet(first));
        }

        [Fact]
        public void HasSameDataSetIgnoresPublishingIntervalUsedForSplitting()
        {
            var first = CreateEntry();
            var second = CreateEntry() with
            {
                DataSetPublishingInterval = 2000,
                DataSetPublishingIntervalTimespan = TimeSpan.FromSeconds(2)
            };

            Assert.Equal(true, first.HasSameDataSet(second));
            Assert.Equal(true, second.HasSameDataSet(first));
        }

        [Theory]
        [MemberData(nameof(GetDifferentDataSets))]
        public void HasSameDataSetReturnsFalseForSingleFieldDifference(string name,
            Action<PublishedNodesEntryModel> mutate)
        {
            var first = CreateEntry();
            var second = CreateEntry();
            mutate(second);

            Assert.False(first.HasSameDataSet(second));
            Assert.False(second.HasSameDataSet(first));
        }

        [Fact]
        public void ToDataSetEntryReturnsNullForNullInput()
        {
            PublishedNodesEntryModel? entry = null;

            var result = entry.ToDataSetEntry();

            Assert.Null(result);
        }

        [Fact]
        public void ToDataSetEntryRemovesNodeAndPasswordFields()
        {
            var entry = CreateEntry() with
            {
                NodeId = new NodeIdModel { Identifier = "i=2258" },
                OpcNodes = [new OpcNodeModel { Id = "i=2258" }]
            };

            var result = entry.ToDataSetEntry();

            Assert.NotNull(result);
            Assert.Null(result.NodeId);
            Assert.Null(result.OpcNodes);
            Assert.Null(result.OpcAuthenticationPassword);
            Assert.Null(result.EncryptedAuthPassword);
            Assert.Equal(entry.EndpointUrl, result.EndpointUrl);
            Assert.Equal(entry.OpcAuthenticationUsername, result.OpcAuthenticationUsername);
        }

        [Fact]
        public void UniqueWriterGroupIdUsesNormalizedTimeSpanValues()
        {
            var first = CreateEntry() with
            {
                BatchTriggerInterval = 1234,
                BatchTriggerIntervalTimespan = TimeSpan.FromSeconds(10)
            };
            var second = CreateEntry() with
            {
                BatchTriggerInterval = 10000,
                BatchTriggerIntervalTimespan = null
            };
            var different = second with
            {
                BatchTriggerInterval = 10001
            };

            Assert.Equal(first.GetUniqueWriterGroupId(), second.GetUniqueWriterGroupId());
            Assert.NotEqual(second.GetUniqueWriterGroupId(), different.GetUniqueWriterGroupId());
        }

        [Fact]
        public void UniqueDataSetWriterIdUsesExplicitPublishingIntervalOverride()
        {
            var first = CreateEntry() with
            {
                DataSetPublishingInterval = 1000,
                DataSetPublishingIntervalTimespan = TimeSpan.FromSeconds(1)
            };
            var second = CreateEntry() with
            {
                DataSetPublishingInterval = 2000,
                DataSetPublishingIntervalTimespan = TimeSpan.FromSeconds(2)
            };

            Assert.Equal(
                first.GetUniqueDataSetWriterId(TimeSpan.FromSeconds(5)),
                second.GetUniqueDataSetWriterId(TimeSpan.FromSeconds(5)));
            Assert.NotEqual(first.GetUniqueDataSetWriterId(), second.GetUniqueDataSetWriterId());
        }

        [Fact]
        public void NormalizedIntervalsPreferTimeSpanOverMilliseconds()
        {
            var entry = CreateEntry() with
            {
                MetaDataUpdateTime = 1,
                MetaDataUpdateTimeTimespan = TimeSpan.FromSeconds(2),
                BatchTriggerInterval = 3,
                BatchTriggerIntervalTimespan = TimeSpan.FromSeconds(4),
                DataSetSamplingInterval = 5,
                DataSetSamplingIntervalTimespan = TimeSpan.FromSeconds(6),
                DefaultHeartbeatInterval = 7,
                DefaultHeartbeatIntervalTimespan = TimeSpan.FromSeconds(8),
                DataSetPublishingInterval = 9,
                DataSetPublishingIntervalTimespan = TimeSpan.FromSeconds(10)
            };

            Assert.Equal(TimeSpan.FromSeconds(2), entry.GetNormalizedMetaDataUpdateTime());
            Assert.Equal(TimeSpan.FromSeconds(4), entry.GetNormalizedBatchTriggerInterval());
            Assert.Equal(TimeSpan.FromSeconds(6), entry.GetNormalizedDataSetSamplingInterval());
            Assert.Equal(TimeSpan.FromSeconds(8), entry.GetNormalizedDefaultHeartbeatInterval());
            Assert.Equal(TimeSpan.FromSeconds(10), entry.GetNormalizedDataSetPublishingInterval());
        }

        [Fact]
        public void ToConnectionModelMapsEndpointOptionsAndUserNameCredential()
        {
            var entry = CreateEntry() with
            {
                UseSecurity = true,
                EndpointSecurityMode = null,
                OpcAuthenticationMode = OpcAuthenticationMode.UsernamePassword,
                OpcAuthenticationUsername = "user",
                OpcAuthenticationPassword = "password",
                UseReverseConnect = true,
                DisableSubscriptionTransfer = true,
                DumpConnectionDiagnostics = true
            };

            var connection = entry.ToConnectionModel();

            Assert.Equal(entry.EndpointUrl, connection.Endpoint.Url);
            Assert.Equal(SecurityMode.NotNone, connection.Endpoint.SecurityMode);
            Assert.Equal(entry.EndpointSecurityPolicy, connection.Endpoint.SecurityPolicy);
            Assert.Equal(ConnectionOptions.UseReverseConnect,
                connection.Options & ConnectionOptions.UseReverseConnect);
            Assert.Equal(ConnectionOptions.NoSubscriptionTransfer,
                connection.Options & ConnectionOptions.NoSubscriptionTransfer);
            Assert.Equal(ConnectionOptions.DumpDiagnostics,
                connection.Options & ConnectionOptions.DumpDiagnostics);
            Assert.NotNull(connection.User);
            Assert.Equal(CredentialType.UserName, connection.User!.Type);
            Assert.NotNull(connection.User.Value);
            Assert.Equal("user", connection.User.Value!.User);
            Assert.Equal("password", connection.User.Value.Password);
        }

        [Fact]
        public void ToConnectionModelOmitsAnonymousUser()
        {
            var connection = CreateEntry() with
            {
                OpcAuthenticationMode = OpcAuthenticationMode.Anonymous
            };

            var result = connection.ToConnectionModel();

            Assert.Null(result.User);
            Assert.Equal(ConnectionOptions.None, result.Options);
            Assert.Equal(SecurityMode.Sign, result.Endpoint.SecurityMode);
        }

        [Fact]
        public void ToConnectionModelThrowsWhenEncryptedCredentialCannotBeDecrypted()
        {
            var entry = CreateEntry() with
            {
                OpcAuthenticationMode = OpcAuthenticationMode.UsernamePassword,
                OpcAuthenticationUsername = null,
                EncryptedAuthUsername = "encrypted"
            };

            Assert.Throws<NotSupportedException>(() => entry.ToConnectionModel());
        }

        [Fact]
        public void ToPublishedNodesEntryReturnsNullForMissingEndpoint()
        {
            ConnectionModel? connection = null;

            var result = connection.ToPublishedNodesEntry();

            Assert.Null(result);
        }

        [Theory]
        [InlineData(SecurityMode.None, false, null)]
        [InlineData(SecurityMode.NotNone, true, null)]
        [InlineData(SecurityMode.Sign, null, SecurityMode.Sign)]
        public void ToPublishedNodesEntryMapsSecurityModeAndCertificateCredentials(
            SecurityMode securityMode,
            bool? expectedUseSecurity, SecurityMode? expectedSecurityMode)
        {
            var connection = new ConnectionModel
            {
                Endpoint = new EndpointModel
                {
                    Url = "opc.tcp://localhost:50000",
                    SecurityMode = securityMode,
                    SecurityPolicy = "policy"
                },
                Group = "group",
                Options = ConnectionOptions.UseReverseConnect |
                    ConnectionOptions.NoSubscriptionTransfer,
                User = new CredentialModel
                {
                    Type = CredentialType.X509Certificate,
                    Value = new UserIdentityModel
                    {
                        User = "cert",
                        Password = "secret"
                    }
                }
            };

            var entry = connection.ToPublishedNodesEntry();

            Assert.NotNull(entry);
            Assert.Equal("opc.tcp://localhost:50000", entry.EndpointUrl);
            Assert.Equal(expectedUseSecurity, entry.UseSecurity);
            Assert.Equal(expectedSecurityMode, entry.EndpointSecurityMode);
            Assert.Equal("policy", entry.EndpointSecurityPolicy);
            Assert.Equal(OpcAuthenticationMode.Certificate, entry.OpcAuthenticationMode);
            Assert.Equal("cert", entry.OpcAuthenticationUsername);
            Assert.Equal("secret", entry.OpcAuthenticationPassword);
            Assert.Equal("group", entry.DataSetWriterGroup);
            Assert.Equal(true, entry.UseReverseConnect);
            Assert.Equal(true, entry.DisableSubscriptionTransfer);
            Assert.Equal(MessageEncoding.Json, entry.MessageEncoding);
            Assert.Equal(MessagingMode.FullNetworkMessages, entry.MessagingMode);
            Assert.Empty(entry.OpcNodes!);
        }

        public static TheoryData<string, Action<PublishedNodesEntryModel>>
            GetDifferentWriterGroups()
        {
            return new TheoryData<string, Action<PublishedNodesEntryModel>>
            {
                { "publisher id", entry => entry.PublisherId = "other" },
                { "data set writer group", entry => entry.DataSetWriterGroup = "other" },
                { "writer group type", entry => entry.WriterGroupType = "other" },
                { "writer group root", entry => entry.WriterGroupRootNodeId = "other" },
                { "transport", entry => entry.WriterGroupTransport = WriterGroupTransport.Mqtt },
                { "transport configuration", entry => entry.WriterGroupTransportConfiguration = "other" },
                { "writer group qos", entry => entry.WriterGroupQualityOfService = QoS.ExactlyOnce },
                { "writer group queue", entry => entry.WriterGroupQueueName = "other" },
                { "message encoding", entry => entry.MessageEncoding = MessageEncoding.JsonGzip },
                { "messaging mode", entry => entry.MessagingMode = MessagingMode.DataSets },
                { "batch size", entry => entry.BatchSize = 101 },
                { "batch interval", entry => entry.BatchTriggerIntervalTimespan = TimeSpan.FromSeconds(11) },
                { "partitions", entry => entry.WriterGroupPartitions = 4 },
                { "message retention", entry => entry.WriterGroupMessageRetention = false },
                { "message ttl", entry => entry.WriterGroupMessageTtlTimepan = TimeSpan.FromMinutes(3) }
            };
        }

        public static TheoryData<string, Action<PublishedNodesEntryModel>>
            GetDifferentDataSets()
        {
            return new TheoryData<string, Action<PublishedNodesEntryModel>>
            {
                { "writer group", entry => entry.DataSetWriterGroup = "other" },
                { "endpoint", entry => entry.EndpointUrl = "opc.tcp://other:50000" },
                { "reverse connect", entry => entry.UseReverseConnect = true },
                { "disable transfer normalized", entry => entry.DisableSubscriptionTransfer = true },
                { "use security", entry => entry.UseSecurity = false },
                { "security mode", entry => entry.EndpointSecurityMode = SecurityMode.SignAndEncrypt },
                { "security policy", entry => entry.EndpointSecurityPolicy = "other" },
                { "authentication mode", entry => entry.OpcAuthenticationMode = OpcAuthenticationMode.Certificate },
                { "user name", entry => entry.OpcAuthenticationUsername = "other" },
                { "password", entry => entry.OpcAuthenticationPassword = "other" },
                { "encrypted user", entry => entry.EncryptedAuthUsername = "other" },
                { "encrypted password", entry => entry.EncryptedAuthPassword = "other" },
                { "max keep alive", entry => entry.MaxKeepAliveCount = 43 },
                { "writer id", entry => entry.DataSetWriterId = "other" },
                { "data set name", entry => entry.DataSetName = "other" },
                { "data set type", entry => entry.DataSetType = "other" },
                { "data set root", entry => entry.DataSetRootNodeId = "other" },
                { "source uri", entry => entry.DataSetSourceUri = "other" },
                { "subject", entry => entry.DataSetSubject = "other" },
                { "class id", entry => entry.DataSetClassId = Guid.NewGuid() },
                { "key frame", entry => entry.DataSetKeyFrameCount = 5 },
                { "disable transfer exact", entry => entry.DisableSubscriptionTransfer = null },
                { "keep alive messages", entry => entry.SendKeepAliveDataSetMessages = false },
                { "keep alive key frames", entry => entry.SendKeepAliveAsKeyFrameMessages = false },
                { "priority", entry => entry.Priority = 6 },
                { "metadata update time", entry => entry.MetaDataUpdateTimeTimespan = TimeSpan.FromSeconds(31) },
                { "sampling interval", entry => entry.DataSetSamplingIntervalTimespan = TimeSpan.FromSeconds(32) },
                { "heartbeat interval", entry => entry.DefaultHeartbeatIntervalTimespan = TimeSpan.FromSeconds(33) },
                { "heartbeat behavior", entry => entry.DefaultHeartbeatBehavior = HeartbeatBehavior.PeriodicLKG },
                { "qos", entry => entry.QualityOfService = QoS.AtMostOnce },
                { "queue", entry => entry.QueueName = "other" },
                { "metadata queue", entry => entry.MetaDataQueueName = "other" },
                { "routing", entry => entry.DataSetRouting = DataSetRoutingMode.UseBrowseNamesWithNamespaceIndex },
                { "republish", entry => entry.RepublishAfterTransfer = false },
                { "watchdog time", entry => entry.OpcNodeWatchdogTimespan = TimeSpan.FromMinutes(5) },
                { "watchdog behavior", entry => entry.DataSetWriterWatchdogBehavior = SubscriptionWatchdogBehavior.FailFast },
                { "watchdog condition", entry => entry.OpcNodeWatchdogCondition = MonitoredItemWatchdogCondition.WhenAllAreLate },
                { "fetch display names", entry => entry.DataSetFetchDisplayNames = false },
                { "message retention", entry => entry.MessageRetention = false },
                { "message ttl", entry => entry.MessageTtlTimespan = TimeSpan.FromMinutes(6) }
            };
        }

        private static PublishedNodesEntryModel CreateEntry()
        {
            return new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://localhost:50000",
                PublisherId = "publisher",
                DataSetWriterGroup = "writerGroup",
                WriterGroupRootNodeId = "root",
                WriterGroupType = "type",
                WriterGroupTransport = WriterGroupTransport.IoTHub,
                WriterGroupTransportConfiguration = "transport",
                WriterGroupQualityOfService = QoS.AtLeastOnce,
                WriterGroupQueueName = "writerQueue",
                MessageEncoding = MessageEncoding.Json,
                MessagingMode = MessagingMode.FullNetworkMessages,
                BatchSize = 100,
                BatchTriggerIntervalTimespan = TimeSpan.FromSeconds(10),
                WriterGroupPartitions = 3,
                WriterGroupMessageTtlTimepan = TimeSpan.FromMinutes(2),
                WriterGroupMessageRetention = true,
                DataSetWriterId = "writer",
                UseReverseConnect = false,
                DisableSubscriptionTransfer = false,
                UseSecurity = true,
                EndpointSecurityMode = SecurityMode.Sign,
                EndpointSecurityPolicy = "policy",
                OpcAuthenticationMode = OpcAuthenticationMode.UsernamePassword,
                OpcAuthenticationUsername = "user",
                OpcAuthenticationPassword = "password",
                EncryptedAuthUsername = "encryptedUser",
                EncryptedAuthPassword = "encryptedPassword",
                DataSetName = "dataSet",
                DataSetClassId = Guid.Parse("42D0E4D1-691F-465E-B90A-EF527400D60C"),
                DataSetKeyFrameCount = 4,
                DataSetType = "dataSetType",
                DataSetRootNodeId = "dataSetRoot",
                SendKeepAliveDataSetMessages = true,
                SendKeepAliveAsKeyFrameMessages = true,
                Priority = 5,
                MaxKeepAliveCount = 42,
                MetaDataUpdateTimeTimespan = TimeSpan.FromSeconds(30),
                DataSetSamplingIntervalTimespan = TimeSpan.FromSeconds(20),
                DefaultHeartbeatIntervalTimespan = TimeSpan.FromSeconds(40),
                DefaultHeartbeatBehavior = HeartbeatBehavior.WatchdogLKG,
                QualityOfService = QoS.ExactlyOnce,
                QueueName = "queue",
                MetaDataQueueName = "metadataQueue",
                DataSetRouting = DataSetRoutingMode.UseBrowseNames,
                RepublishAfterTransfer = true,
                OpcNodeWatchdogTimespan = TimeSpan.FromMinutes(1),
                DataSetWriterWatchdogBehavior = SubscriptionWatchdogBehavior.Reset,
                OpcNodeWatchdogCondition = MonitoredItemWatchdogCondition.WhenAnyIsLate,
                DataSetFetchDisplayNames = true,
                MessageTtlTimespan = TimeSpan.FromMinutes(4),
                MessageRetention = true,
                DataSetSourceUri = "source",
                DataSetSubject = "subject"
            };
        }
    }
}
