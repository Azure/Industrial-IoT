// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using System;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="RequestHeaderModelEx.GetNamespaceFormat"/>,
    /// <see cref="PublishedDataSetSourceModelEx.ToSubscriptionModel"/>, and
    /// <see cref="SubscriptionModelEx.CreateSubscriptionId"/>.
    /// </summary>
    public sealed class ModelExtensionsTests
    {
        private static IOptions<PublisherOptions> CreateOptions() =>
            new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();

        // ── RequestHeaderModelEx.GetNamespaceFormat ────────────────────────────

        [Fact]
        public void GetNamespaceFormat_NullHeaderNullOptions_ReturnsUri()
        {
            var result = ((RequestHeaderModel?)null).GetNamespaceFormat();
            Assert.Equal(NamespaceFormat.Uri, result);
        }

        [Fact]
        public void GetNamespaceFormat_NullHeaderWithOptions_ReturnsOptionsDefault()
        {
            var options = CreateOptions();
            options.Value.DefaultNamespaceFormat = NamespaceFormat.Index;

            var result = ((RequestHeaderModel?)null).GetNamespaceFormat(options);

            Assert.Equal(NamespaceFormat.Index, result);
        }

        [Fact]
        public void GetNamespaceFormat_HeaderWithFormat_ReturnsHeaderFormat()
        {
            var header = new RequestHeaderModel
            {
                NamespaceFormat = NamespaceFormat.Expanded
            };

            var result = header.GetNamespaceFormat();

            Assert.Equal(NamespaceFormat.Expanded, result);
        }

        [Fact]
        public void GetNamespaceFormat_HeaderOverridesOptions()
        {
            var options = CreateOptions();
            options.Value.DefaultNamespaceFormat = NamespaceFormat.Index;
            var header = new RequestHeaderModel
            {
                NamespaceFormat = NamespaceFormat.Expanded
            };

            var result = header.GetNamespaceFormat(options);

            Assert.Equal(NamespaceFormat.Expanded, result);
        }

        [Fact]
        public void GetNamespaceFormat_NullHeaderNoOptionsDefault_ReturnsUri()
        {
            var options = CreateOptions();
            options.Value.DefaultNamespaceFormat = null;

            var result = ((RequestHeaderModel?)null).GetNamespaceFormat(options);

            Assert.Equal(NamespaceFormat.Uri, result);
        }

        // ── PublishedDataSetSourceModelEx.ToSubscriptionModel ─────────────────

        [Fact]
        public void ToSubscriptionModel_NullSettings_ReturnsDefaultSubscription()
        {
            var result = ((PublishedDataSetSettingsModel?)null)
                .ToSubscriptionModel(null, null);

            Assert.NotNull(result);
            Assert.Null(result.Priority);
            Assert.Null(result.PublishingInterval);
        }

        [Fact]
        public void ToSubscriptionModel_WithPriority_SetsPriority()
        {
            var settings = new PublishedDataSetSettingsModel { Priority = 5 };
            var result = settings.ToSubscriptionModel(null, null);
            Assert.Equal((byte)5, result.Priority);
        }

        [Fact]
        public void ToSubscriptionModel_WithPublishingInterval_SetsInterval()
        {
            var interval = TimeSpan.FromMilliseconds(500);
            var settings = new PublishedDataSetSettingsModel
            {
                PublishingInterval = interval
            };

            var result = settings.ToSubscriptionModel(null, null);
            Assert.Equal(interval, result.PublishingInterval);
        }

        [Fact]
        public void ToSubscriptionModel_IgnoreConfiguredPublishingIntervalsTrue_NullsInterval()
        {
            var settings = new PublishedDataSetSettingsModel
            {
                PublishingInterval = TimeSpan.FromMilliseconds(500)
            };

            var result = settings.ToSubscriptionModel(null, ignoreConfiguredPublishingIntervals: true);
            Assert.Null(result.PublishingInterval);
        }

        [Fact]
        public void ToSubscriptionModel_IgnoreConfiguredPublishingIntervalsFalse_KeepsInterval()
        {
            var interval = TimeSpan.FromMilliseconds(500);
            var settings = new PublishedDataSetSettingsModel
            {
                PublishingInterval = interval
            };

            var result = settings.ToSubscriptionModel(null, ignoreConfiguredPublishingIntervals: false);
            Assert.Equal(interval, result.PublishingInterval);
        }

        [Fact]
        public void ToSubscriptionModel_FetchBrowsePathFromRootOverride_SetsFlag()
        {
            var result = ((PublishedDataSetSettingsModel?)null)
                .ToSubscriptionModel(fetchBrowsePathFromRootOverride: true, null);

            Assert.True(result.ResolveBrowsePathFromRoot);
        }

        [Fact]
        public void ToSubscriptionModel_WithKeepAliveAndLifetime_SetsValues()
        {
            var settings = new PublishedDataSetSettingsModel
            {
                MaxKeepAliveCount = 10,
                LifeTimeCount = 30,
                MaxNotificationsPerPublish = 100
            };

            var result = settings.ToSubscriptionModel(null, null);

            Assert.Equal((uint)10, result.KeepAliveCount);
            Assert.Equal((uint)30, result.LifetimeCount);
            Assert.Equal((uint)100, result.MaxNotificationsPerPublish);
        }

        [Fact]
        public void ToSubscriptionModel_WithUseDeferredAck_SetsFlag()
        {
            var settings = new PublishedDataSetSettingsModel
            {
                UseDeferredAcknoledgements = true
            };

            var result = settings.ToSubscriptionModel(null, null);
            Assert.True(result.UseDeferredAcknoledgements);
        }

        [Fact]
        public void ToSubscriptionModel_WithEnableImmediatePublishing_SetsFlag()
        {
            var settings = new PublishedDataSetSettingsModel
            {
                EnableImmediatePublishing = true
            };

            var result = settings.ToSubscriptionModel(null, null);
            Assert.True(result.EnableImmediatePublishing);
        }

        [Fact]
        public void ToSubscriptionModel_WithEnableSequentialPublishing_SetsFlag()
        {
            var settings = new PublishedDataSetSettingsModel
            {
                EnableSequentialPublishing = true
            };

            var result = settings.ToSubscriptionModel(null, null);
            Assert.True(result.EnableSequentialPublishing);
        }

        [Fact]
        public void ToSubscriptionModel_WithRepublishAfterTransfer_SetsFlag()
        {
            var settings = new PublishedDataSetSettingsModel
            {
                RepublishAfterTransfer = true
            };

            var result = settings.ToSubscriptionModel(null, null);
            Assert.True(result.RepublishAfterTransfer);
        }

        [Fact]
        public void ToSubscriptionModel_WithWatchdogTimeout_SetsTimeout()
        {
            var timeout = TimeSpan.FromSeconds(30);
            var settings = new PublishedDataSetSettingsModel
            {
                MonitoredItemWatchdogTimeout = timeout
            };

            var result = settings.ToSubscriptionModel(null, null);
            Assert.Equal(timeout, result.MonitoredItemWatchdogTimeout);
        }

        // ── SubscriptionModelEx.CreateSubscriptionId ───────────────────────────

        [Fact]
        public void CreateSubscriptionId_EmptyModel_ReturnsNonEmptyString()
        {
            var model = new SubscriptionModel();
            var id = model.CreateSubscriptionId();
            Assert.NotEmpty(id);
        }

        [Fact]
        public void CreateSubscriptionId_SameModel_ReturnsSameId()
        {
            var model = new SubscriptionModel
            {
                Priority = 3,
                PublishingInterval = TimeSpan.FromMilliseconds(1000)
            };

            var id1 = model.CreateSubscriptionId();
            var id2 = model.CreateSubscriptionId();

            Assert.Equal(id1, id2);
        }

        [Fact]
        public void CreateSubscriptionId_DifferentPriority_ReturnsDifferentId()
        {
            var model1 = new SubscriptionModel { Priority = 1 };
            var model2 = new SubscriptionModel { Priority = 2 };

            Assert.NotEqual(model1.CreateSubscriptionId(), model2.CreateSubscriptionId());
        }

        [Fact]
        public void CreateSubscriptionId_DifferentInterval_ReturnsDifferentId()
        {
            var model1 = new SubscriptionModel
            {
                PublishingInterval = TimeSpan.FromMilliseconds(500)
            };
            var model2 = new SubscriptionModel
            {
                PublishingInterval = TimeSpan.FromMilliseconds(1000)
            };

            Assert.NotEqual(model1.CreateSubscriptionId(), model2.CreateSubscriptionId());
        }

        [Fact]
        public void CreateSubscriptionId_ContainsPriorityAndIntervalSuffix()
        {
            var model = new SubscriptionModel
            {
                Priority = 7,
                PublishingInterval = TimeSpan.FromMilliseconds(500)
            };

            var id = model.CreateSubscriptionId();

            // The suffix format is [P{priority}@{intervalMs}]
            Assert.Contains("[P7@500]", id, StringComparison.Ordinal);
        }

        [Fact]
        public void CreateSubscriptionId_NullPriority_UsesZero()
        {
            var model = new SubscriptionModel { Priority = null };
            var id = model.CreateSubscriptionId();
            Assert.Contains("[P0@", id, StringComparison.Ordinal);
        }
    }
}
