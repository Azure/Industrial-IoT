// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License. See LICENSE in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Threading;
    using Xunit;
    using PublisherClientOptions = Azure.IIoT.OpcUa.Publisher.Stack.OpcUaClientOptions;

    /// <summary>
    /// Tests for <see cref="ManagedSessionOptionsAdapter"/> pure-logic helpers.
    /// </summary>
    public sealed class ManagedSessionOptionsAdapterTests
    {
        // ── CreateReconnectPolicy ─────────────────────────────────────────────

        [Fact]
        public void CreateReconnectPolicy_NullDelays_UsesDefaultInitialAndMaxDelay()
        {
            var options = new PublisherClientOptions
            {
                MinReconnectDelayDuration = null,
                MaxReconnectDelayDuration = null
            };

            var policy = ManagedSessionOptionsAdapter.CreateReconnectPolicy(options);

            Assert.Equal(ReconnectPolicy.DefaultInitialDelay, policy.InitialDelay);
            Assert.Equal(ReconnectPolicy.DefaultMaxDelay, policy.MaxDelay);
            Assert.Equal(Timeout.InfiniteTimeSpan, policy.MaxTotalReconnectTime);
        }

        [Fact]
        public void CreateReconnectPolicy_ZeroDelays_UsesDefaultInitialAndMaxDelay()
        {
            var options = new PublisherClientOptions
            {
                MinReconnectDelayDuration = TimeSpan.Zero,
                MaxReconnectDelayDuration = TimeSpan.Zero
            };

            var policy = ManagedSessionOptionsAdapter.CreateReconnectPolicy(options);

            Assert.Equal(ReconnectPolicy.DefaultInitialDelay, policy.InitialDelay);
            Assert.Equal(ReconnectPolicy.DefaultMaxDelay, policy.MaxDelay);
        }

        [Fact]
        public void CreateReconnectPolicy_NegativeDelays_UsesDefaultInitialAndMaxDelay()
        {
            var options = new PublisherClientOptions
            {
                MinReconnectDelayDuration = TimeSpan.FromSeconds(-1),
                MaxReconnectDelayDuration = TimeSpan.FromSeconds(-5)
            };

            var policy = ManagedSessionOptionsAdapter.CreateReconnectPolicy(options);

            Assert.Equal(ReconnectPolicy.DefaultInitialDelay, policy.InitialDelay);
            Assert.Equal(ReconnectPolicy.DefaultMaxDelay, policy.MaxDelay);
        }

        [Fact]
        public void CreateReconnectPolicy_ExplicitDelays_UsesThose()
        {
            var initial = TimeSpan.FromSeconds(3);
            var max = TimeSpan.FromSeconds(60);
            var options = new PublisherClientOptions
            {
                MinReconnectDelayDuration = initial,
                MaxReconnectDelayDuration = max
            };

            var policy = ManagedSessionOptionsAdapter.CreateReconnectPolicy(options);

            Assert.Equal(initial, policy.InitialDelay);
            Assert.Equal(max, policy.MaxDelay);
        }

        [Fact]
        public void CreateReconnectPolicy_MaxLessThanInitial_ClampsInitialToMax()
        {
            var initial = TimeSpan.FromSeconds(20);
            var max = TimeSpan.FromSeconds(5);
            var options = new PublisherClientOptions
            {
                MinReconnectDelayDuration = initial,
                MaxReconnectDelayDuration = max
            };

            var policy = ManagedSessionOptionsAdapter.CreateReconnectPolicy(options);

            // When max < initial, initial is set to max
            Assert.Equal(max, policy.InitialDelay);
            Assert.Equal(max, policy.MaxDelay);
        }

        [Fact]
        public void CreateReconnectPolicy_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ManagedSessionOptionsAdapter.CreateReconnectPolicy(null!));
        }

        // ── TransferSubscriptionsOnRecreate ──────────────────────────────────

        [Fact]
        public void TransferSubscriptionsOnRecreate_NoFlag_ReturnsTrue()
        {
            var connection = new ConnectionModel
            {
                Endpoint = new EndpointModel { Url = "opc.tcp://host:4840" },
                Options = ConnectionOptions.None
            };

            var result = ManagedSessionOptionsAdapter
                .TransferSubscriptionsOnRecreate(connection);

            Assert.True(result);
        }

        [Fact]
        public void TransferSubscriptionsOnRecreate_NoTransferFlag_ReturnsFalse()
        {
            var connection = new ConnectionModel
            {
                Endpoint = new EndpointModel { Url = "opc.tcp://host:4840" },
                Options = ConnectionOptions.NoSubscriptionTransfer
            };

            var result = ManagedSessionOptionsAdapter
                .TransferSubscriptionsOnRecreate(connection);

            Assert.False(result);
        }

        [Fact]
        public void TransferSubscriptionsOnRecreate_OtherFlagSet_ReturnsTrue()
        {
            var connection = new ConnectionModel
            {
                Endpoint = new EndpointModel { Url = "opc.tcp://host:4840" },
                Options = ConnectionOptions.UseReverseConnect
            };

            var result = ManagedSessionOptionsAdapter
                .TransferSubscriptionsOnRecreate(connection);

            Assert.True(result);
        }

        [Fact]
        public void TransferSubscriptionsOnRecreate_NullConnection_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ManagedSessionOptionsAdapter.TransferSubscriptionsOnRecreate(null!));
        }

        // ── GetPublishWorkerCounts ────────────────────────────────────────────

        [Fact]
        public void GetPublishWorkerCounts_DefaultOptions_ReturnsDefaultMinAndMax()
        {
            var options = new PublisherClientOptions();

            var (min, max) = ManagedSessionOptionsAdapter
                .GetPublishWorkerCounts(options, 0);

            // Default min is 2, default max is 10
            Assert.Equal(2, min);
            Assert.Equal(10, max);
        }

        [Fact]
        public void GetPublishWorkerCounts_ExplicitMinAndMax_ReturnsThem()
        {
            var options = new PublisherClientOptions
            {
                MinPublishRequests = 5,
                MaxPublishRequests = 20
            };

            var (min, max) = ManagedSessionOptionsAdapter
                .GetPublishWorkerCounts(options, 0);

            Assert.Equal(5, min);
            Assert.Equal(20, max);
        }

        [Fact]
        public void GetPublishWorkerCounts_ZeroMin_DefaultsToTwo()
        {
            var options = new PublisherClientOptions
            {
                MinPublishRequests = 0,
                MaxPublishRequests = 15
            };

            var (min, _) = ManagedSessionOptionsAdapter
                .GetPublishWorkerCounts(options, 0);

            Assert.Equal(2, min);
        }

        [Fact]
        public void GetPublishWorkerCounts_ZeroMax_ClampsToUshortMax()
        {
            var options = new PublisherClientOptions
            {
                MaxPublishRequests = 0
            };

            var (_, max) = ManagedSessionOptionsAdapter
                .GetPublishWorkerCounts(options, 0);

            Assert.Equal(ushort.MaxValue, max);
        }

        [Fact]
        public void GetPublishWorkerCounts_NegativeMax_ClampsToUshortMax()
        {
            var options = new PublisherClientOptions
            {
                MaxPublishRequests = -1
            };

            var (_, max) = ManagedSessionOptionsAdapter
                .GetPublishWorkerCounts(options, 0);

            Assert.Equal(ushort.MaxValue, max);
        }

        [Fact]
        public void GetPublishWorkerCounts_100PercentPerSubscription_RaisesMin()
        {
            var options = new PublisherClientOptions
            {
                PublishRequestsPerSubscriptionPercent = 100
            };

            var (min, _) = ManagedSessionOptionsAdapter
                .GetPublishWorkerCounts(options, 10);

            // 100% of 10 subscriptions = 10, which is > default min of 2
            Assert.Equal(10, min);
        }

        [Fact]
        public void GetPublishWorkerCounts_50PercentPerSubscription_RaisesMin()
        {
            var options = new PublisherClientOptions
            {
                PublishRequestsPerSubscriptionPercent = 50
            };

            var (min, _) = ManagedSessionOptionsAdapter
                .GetPublishWorkerCounts(options, 10);

            // 50% of 10 = ceil(5.0) = 5, which is > default min of 2
            Assert.Equal(5, min);
        }

        [Fact]
        public void GetPublishWorkerCounts_MinClampedToMax()
        {
            var options = new PublisherClientOptions
            {
                MinPublishRequests = 100,
                MaxPublishRequests = 5
            };

            var (min, max) = ManagedSessionOptionsAdapter
                .GetPublishWorkerCounts(options, 0);

            // min must not exceed max
            Assert.Equal(5, min);
            Assert.Equal(5, max);
        }

        [Fact]
        public void GetPublishWorkerCounts_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ManagedSessionOptionsAdapter.GetPublishWorkerCounts(null!, 0));
        }

        [Fact]
        public void GetPublishWorkerCounts_NegativeSubscriptionCount_ThrowsArgumentOutOfRangeException()
        {
            var options = new PublisherClientOptions();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ManagedSessionOptionsAdapter.GetPublishWorkerCounts(options, -1));
        }

        // ── CreateOperationLimitOverrides ─────────────────────────────────────

        [Fact]
        public void CreateOperationLimitOverrides_BothZero_ReturnsNull()
        {
            var options = new PublisherClientOptions
            {
                MaxNodesPerReadOverride = 0,
                MaxNodesPerBrowseOverride = 0
            };

            var result = ManagedSessionOptionsAdapter.CreateOperationLimitOverrides(options);

            Assert.Null(result);
        }

        [Fact]
        public void CreateOperationLimitOverrides_BothNull_ReturnsNull()
        {
            var options = new PublisherClientOptions();

            var result = ManagedSessionOptionsAdapter.CreateOperationLimitOverrides(options);

            Assert.Null(result);
        }

        [Fact]
        public void CreateOperationLimitOverrides_MaxNodesPerRead_ReturnsMappedLimits()
        {
            var options = new PublisherClientOptions { MaxNodesPerReadOverride = 500 };

            var result = ManagedSessionOptionsAdapter.CreateOperationLimitOverrides(options);

            Assert.NotNull(result);
            Assert.Equal(500u, result!.MaxNodesPerRead);
            Assert.Equal(0u, result.MaxNodesPerBrowse);
        }

        [Fact]
        public void CreateOperationLimitOverrides_MaxNodesPerBrowse_ReturnsMappedLimits()
        {
            var options = new PublisherClientOptions { MaxNodesPerBrowseOverride = 250 };

            var result = ManagedSessionOptionsAdapter.CreateOperationLimitOverrides(options);

            Assert.NotNull(result);
            Assert.Equal(0u, result!.MaxNodesPerRead);
            Assert.Equal(250u, result.MaxNodesPerBrowse);
        }

        [Fact]
        public void CreateOperationLimitOverrides_BothSet_ReturnsBoth()
        {
            var options = new PublisherClientOptions
            {
                MaxNodesPerReadOverride = 100,
                MaxNodesPerBrowseOverride = 200
            };

            var result = ManagedSessionOptionsAdapter.CreateOperationLimitOverrides(options);

            Assert.NotNull(result);
            Assert.Equal(100u, result!.MaxNodesPerRead);
            Assert.Equal(200u, result.MaxNodesPerBrowse);
        }

        [Fact]
        public void CreateOperationLimitOverrides_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ManagedSessionOptionsAdapter.CreateOperationLimitOverrides(null!));
        }

        // ── CreatePoolOptions ─────────────────────────────────────────────────

        [Fact]
        public void CreatePoolOptions_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ManagedSessionOptionsAdapter.CreatePoolOptions(null!));
        }

        [Fact]
        public void CreatePoolOptions_DefaultOptions_ReturnsSensibleDefaults()
        {
            var options = new PublisherClientOptions();

            var result = ManagedSessionOptionsAdapter.CreatePoolOptions(options);

            Assert.Equal(TimeSpan.FromSeconds(10), result.LingerTimeout);
            Assert.Equal(TimeSpan.FromSeconds(30), result.ServiceCallTimeout);
            Assert.Equal(TimeSpan.FromMinutes(1), result.NodeCacheTimeout);
            Assert.Equal(4096, result.NodeCacheCapacity);
        }

        [Fact]
        public void CreatePoolOptions_ExplicitOptions_MapsCorrectly()
        {
            var options = new PublisherClientOptions
            {
                LingerTimeoutDuration = TimeSpan.FromSeconds(5),
                DefaultServiceCallTimeoutDuration = TimeSpan.FromSeconds(15),
                NodeCacheTimeout = TimeSpan.FromMinutes(2),
                NodeCacheCapacity = 2048
            };

            var result = ManagedSessionOptionsAdapter.CreatePoolOptions(options);

            Assert.Equal(TimeSpan.FromSeconds(5), result.LingerTimeout);
            Assert.Equal(TimeSpan.FromSeconds(15), result.ServiceCallTimeout);
            Assert.Equal(TimeSpan.FromMinutes(2), result.NodeCacheTimeout);
            Assert.Equal(2048, result.NodeCacheCapacity);
        }
    }
}

