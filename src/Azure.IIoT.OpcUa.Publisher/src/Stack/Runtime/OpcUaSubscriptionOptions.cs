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
        /// Default subscription keep alive counter
        /// </summary>
        public uint? DefaultKeepAliveCount { get; set; }

        /// <summary>
        /// Default subscription lifetime counter
        /// </summary>
        public uint? DefaultLifeTimeCount { get; set; }

        /// <summary>
        /// Whether to enable or disable data set metadata explicitly
        /// </summary>
        public bool? DisableDataSetMetaData { get; set; }

        /// <summary>
        /// Default metadata send interval.
        /// </summary>
        public TimeSpan? DefaultMetaDataUpdateTime { get; set; }

        /// <summary>
        /// The number of items in a subscription for which
        /// loading of metadata should be done inline.
        /// </summary>
        public int? AsyncMetaDataLoadThreshold { get; set; }

        /// <summary>
        /// Enable publishing and monitored items when created
        /// rather than when publishing should start.
        /// </summary>
        public bool? EnableImmediatePublishing { get; set; }

        /// <summary>
        /// Enable sequential publishing feature in the stack.
        /// </summary>
        public bool? EnableSequentialPublishing { get; set; }

        /// <summary>
        /// Whether to enable or disable keep alive messages
        /// </summary>
        public bool? EnableDataSetKeepAlives { get; set; }

        /// <summary>
        /// Default keyframe count
        /// </summary>
        public uint? DefaultKeyFrameCount { get; set; }

        /// <summary>
        /// Flag wether to grab the display name of nodes form
        /// the OPC UA Server.
        /// </summary>
        public bool? ResolveDisplayName { get; set; }

        /// <summary>
        /// Always default to use or not use reverse connect
        /// unless overridden by the configuration.
        /// </summary>
        public bool? DefaultUseReverseConnect { get; set; }

        /// <summary>
        /// Never load the complex type system from any session.
        /// This disables metadata loading capability but also
        /// the ability to encode complex types.
        /// </summary>
        public bool? DisableComplexTypeSystem { get; set; }

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
        /// The default rebrowse period for model change event generation.
        /// </summary>
        public TimeSpan? DefaultRebrowsePeriod { get; set; }

        /// <summary>
        /// set the default data change filter for monitored items. Default is
        /// status and value change triggering.
        /// </summary>
        public DataChangeTriggerType? DefaultDataChangeTrigger { get; set; }

        /// <summary>
        /// Disable creating a separate session per writer group. This
        /// will re-use sessions across writer groups. Default is to
        /// create a seperate session.
        /// </summary>
        public bool? DisableSessionPerWriterGroup { get; set; }

        /// <summary>
        /// Create a new session for every subscription that is created.
        /// </summary>
        public bool? EnableSessionPerDataSetWriterId { get; set; }

        /// <summary>
        /// Retrieve paths from root folder to enable automatic
        /// unified namespace publishing
        /// </summary>
        public bool? FetchOpcBrowsePathFromRoot { get; set; }

        /// <summary>
        /// Disable subscription transfer on reconnect.
        /// </summary>
        public bool? DisableSubscriptionTransfer { get; set; }

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
    }
}
