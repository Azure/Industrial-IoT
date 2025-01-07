// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;

    /// <summary>
    /// Subscription configuration
    /// </summary>
    public sealed class OpcUaSubscriptionOptions
    {
        /// <summary>
        /// The default behavior of heartbeat if not configured.
        /// </summary>
        public HeartbeatBehavior? DefaultHeartbeatBehavior { get; set; }

        /// <summary>
        /// The default interval for heartbeats if not configured.
        /// </summary>
        public TimeSpan? DefaultHeartbeatInterval { get; set; }

        /// <summary>
        /// The default flag whether to skip the first value if
        /// not set on node level.
        /// </summary>
        public bool? DefaultSkipFirst { get; set; }

        /// <summary>
        /// Republish messages after transfer
        /// </summary>
        public bool? DefaultRepublishAfterTransfer { get; set; }

        /// <summary>
        /// The default flag whether to descard new items in queue
        /// </summary>
        public bool? DefaultDiscardNew { get; set; }

        /// <summary>
        /// The default sampling interval.
        /// </summary>
        public TimeSpan? DefaultSamplingInterval { get; set; }

        /// <summary>
        /// The default publishing interval.
        /// </summary>
        public TimeSpan? DefaultPublishingInterval { get; set; }

        /// <summary>
        /// Allow max monitored item per subscription. If the server
        /// supports less, this value takes no effect.
        /// </summary>
        public uint? MaxMonitoredItemPerSubscription { get; set; }

        /// <summary>
        /// Default subscription keep alive counter
        /// </summary>
        public uint? DefaultKeepAliveCount { get; set; }

        /// <summary>
        /// Default subscription lifetime counter
        /// </summary>
        public uint? DefaultLifeTimeCount { get; set; }

        /// <summary>
        /// Enable publishing and monitored items when created
        /// rather than when publishing should start.
        /// </summary>
        public bool? EnableImmediatePublishing { get; set; }

        /// <summary>
        /// Flag wether to grab the display name of nodes form
        /// the OPC UA Server.
        /// </summary>
        public bool? ResolveDisplayName { get; set; }

        /// <summary>
        /// set the default queue size for monitored items. If not
        /// set the default queue size will be configured (1 for
        /// data monitored items, and 0 for event monitoring).
        /// </summary>
        public uint? DefaultQueueSize { get; set; }

        /// <summary>
        /// Automatically calculate queue sizes based on the
        /// publishing interval and sampling interval as
        /// max(1, roundup(subscription pi / si)).
        /// </summary>
        public bool? AutoSetQueueSizes { get; set; }

        /// <summary>
        /// Use deferred acnkoledgement (experimental)
        /// </summary>
        public bool? UseDeferredAcknoledgements { get; set; }

        /// <summary>
        /// Always use cyclic reads for sampling
        /// </summary>
        public bool? DefaultSamplingUsingCyclicRead { get; set; }

        /// <summary>
        /// Default cache age to use for cyclic reads.
        /// Default is 0 (uncached)
        /// </summary>
        public TimeSpan DefaultCyclicReadMaxAge { get; set; }

        /// <summary>
        /// The default rebrowse period for model change event generation.
        /// </summary>
        public TimeSpan? DefaultRebrowsePeriod { get; set; }

        /// <summary>
        /// set the default data change filter for monitored items. Default is
        /// status and value change triggering.
        /// </summary>
        public DataChangeTriggerType? DefaultDataChangeTrigger { get; set; }

        /// <summary>
        /// Retrieve paths from root folder to enable automatic
        /// unified namespace publishing
        /// </summary>
        public bool? FetchOpcBrowsePathFromRoot { get; set; }

        /// <summary>
        /// The default watchdog behaviour of the subscription.
        /// </summary>
        public SubscriptionWatchdogBehavior? DefaultWatchdogBehavior { get; set; }

        /// <summary>
        /// Default monitored item watchdog timeout duration.
        /// </summary>
        public TimeSpan? DefaultMonitoredItemWatchdogTimeout { get; set; }

        /// <summary>
        /// The condition when to run the watchdog action in case
        /// of late monitored items.
        /// </summary>
        public MonitoredItemWatchdogCondition? DefaultMonitoredItemWatchdogCondition { get; set; }

        /// <summary>
        /// How long to wait until retrying on errors related
        /// to creating and modifying the subscription.
        /// </summary>
        public TimeSpan? SubscriptionErrorRetryDelay { get; set; }

        /// <summary>
        /// The watchdog period to kick off regular management
        /// of the subscription and reapply any state on failed
        /// nodes.
        /// </summary>
        public TimeSpan? SubscriptionManagementIntervalDuration { get; set; }

        /// <summary>
        /// At what interval should bad monitored items be retried.
        /// These are items that have been rejected by the server
        /// during subscription update or never successfully
        /// published.
        /// </summary>
        public TimeSpan? BadMonitoredItemRetryDelayDuration { get; set; }

        /// <summary>
        /// At what interval should invalid monitored items be
        /// retried. These are items that are potentially
        /// misconfigured.
        /// </summary>
        public TimeSpan? InvalidMonitoredItemRetryDelayDuration { get; set; }
    }
}
