// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Threading;
    using PublisherClientOptions =
        Azure.IIoT.OpcUa.Publisher.Stack.OpcUaClientOptions;

    /// <summary>
    /// Translates Publisher client options to the public managed-session API.
    /// </summary>
    internal static class ManagedSessionOptionsAdapter
    {
        internal static ISubscriptionEngineFactory CreateSubscriptionEngineFactory(
            TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);
            return new DefaultSubscriptionEngineFactory(timeProvider);
        }

        internal static ReconnectPolicy CreateReconnectPolicy(PublisherClientOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            var initialDelay = options.MinReconnectDelayDuration ?? TimeSpan.Zero;
            var maximumDelay = options.MaxReconnectDelayDuration ??
                ReconnectPolicy.DefaultMaxDelay;
            if (initialDelay <= TimeSpan.Zero)
            {
                initialDelay = ReconnectPolicy.DefaultInitialDelay;
            }
            if (maximumDelay <= TimeSpan.Zero)
            {
                maximumDelay = ReconnectPolicy.DefaultMaxDelay;
            }
            if (maximumDelay < initialDelay)
            {
                initialDelay = maximumDelay;
            }
            return new ReconnectPolicy(new ReconnectPolicyOptions
            {
                InitialDelay = initialDelay,
                MaxDelay = maximumDelay,
                MaxTotalReconnectTime = Timeout.InfiniteTimeSpan
            });
        }

        internal static bool TransferSubscriptionsOnRecreate(ConnectionModel connection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            return !connection.Options.HasFlag(ConnectionOptions.NoSubscriptionTransfer);
        }

        internal static (int Minimum, int Maximum) GetPublishWorkerCounts(
            PublisherClientOptions options, int subscriptionCount)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentOutOfRangeException.ThrowIfNegative(subscriptionCount);

            var minimum = options.MinPublishRequests.GetValueOrDefault();
            if (minimum <= 0)
            {
                minimum = 2;
            }
            var maximum = options.MaxPublishRequests ?? 10;
            if (maximum <= 0 || maximum > ushort.MaxValue)
            {
                maximum = ushort.MaxValue;
            }
            if (options.PublishRequestsPerSubscriptionPercent is int percentage)
            {
                var perSubscription = percentage == 100 || percentage <= 0 ?
                    subscriptionCount :
                    (int)Math.Ceiling(subscriptionCount * (percentage / 100.0));
                minimum = Math.Max(minimum, perSubscription);
            }
            minimum = Math.Min(minimum, maximum);
            return (minimum, maximum);
        }

        internal static OperationLimits? CreateOperationLimitOverrides(
            PublisherClientOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            var maxNodesPerRead = options.MaxNodesPerReadOverride.GetValueOrDefault();
            var maxNodesPerBrowse = options.MaxNodesPerBrowseOverride.GetValueOrDefault();
            if (maxNodesPerRead <= 0 && maxNodesPerBrowse <= 0)
            {
                return null;
            }
            return new OperationLimits
            {
                MaxNodesPerRead = maxNodesPerRead > 0 ? (uint)maxNodesPerRead : 0,
                MaxNodesPerBrowse = maxNodesPerBrowse > 0 ? (uint)maxNodesPerBrowse : 0
            };
        }

        internal static ManagedSessionPoolOptions CreatePoolOptions(
            PublisherClientOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return new ManagedSessionPoolOptions
            {
                LingerTimeout = options.LingerTimeoutDuration ??
                    TimeSpan.FromSeconds(10),
                ServiceCallTimeout = options.DefaultServiceCallTimeoutDuration ??
                    TimeSpan.FromSeconds(30),
                NodeCacheTimeout = options.NodeCacheTimeout ??
                    TimeSpan.FromMinutes(1),
                NodeCacheCapacity = options.NodeCacheCapacity ?? 4096
            };
        }

        internal static int GetEndpointOperationTimeout(ManagedSessionClientContext context,
            TimeSpan connectTimeout)
        {
            ArgumentNullException.ThrowIfNull(context);
            var configured = context.Options.Value.Quotas.OperationTimeout;
            var timeout = configured > 0 ? configured : connectTimeout.TotalMilliseconds;
            return (int)Math.Clamp(timeout, 1, int.MaxValue);
        }
    }
}
