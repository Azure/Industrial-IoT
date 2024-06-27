// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;

    /// <summary>
    /// Represents a standard OPC UA Subscription
    /// </summary>
    public sealed record class SubscriptionConfigurationModel
    {
        /// <summary>
        /// Publishing interval
        /// </summary>
        public TimeSpan? PublishingInterval { get; set; }

        /// <summary>
        /// Life time
        /// </summary>
        public uint? LifetimeCount { get; set; }

        /// <summary>
        /// Max keep alive count
        /// </summary>
        public uint? KeepAliveCount { get; set; }

        /// <summary>
        /// Priority
        /// </summary>
        public byte? Priority { get; set; }

        /// <summary>
        /// Max notification per publish
        /// </summary>
        public uint? MaxNotificationsPerPublish { get; set; }

        /// <summary>
        /// Resolves the display names for the monitored items
        /// in the subscription.
        /// </summary>
        public bool? ResolveDisplayName { get; set; }

        /// <summary>
        /// Retrieve paths from root for all monitored items
        /// in the subscription.
        /// </summary>
        public bool ResolveBrowsePathFromRoot { get; set; }

        /// <summary>
        /// The metadata header information or null if disabled.
        /// </summary>
        public DataSetMetaDataModel? MetaData { get; set; }

        /// <summary>
        /// Use deferred acknoledgements
        /// </summary>
        public bool? UseDeferredAcknoledgements { get; set; }

        /// <summary>
        /// The number of items in a subscription for which
        /// loading of metadata should be done inline during
        /// subscription creation (otherwise will be completed
        /// asynchronously). If the number of items in the
        /// subscription is below this value it is guaranteed
        /// that the first notification contains metadata.
        /// Defaults to 30 items.
        /// </summary>
        public int? AsyncMetaDataLoadThreshold { get; set; }

        /// <summary>
        /// Will set the subscription to have publishing
        /// enabled and every monitored item created to be
        /// in desired monitoring mode.
        /// </summary>
        public bool EnableImmediatePublishing { get; set; }

        /// <summary>
        /// Use the sequential publishing feature in the stack.
        /// </summary>
        public bool EnableSequentialPublishing { get; set; }

        /// <summary>
        /// Republish after transfer
        /// </summary>
        public bool? RepublishAfterTransfer { get; set; }

        /// <summary>
        /// Subscription watchdog behavior
        /// </summary>
        public SubscriptionWatchdogBehavior? WatchdogBehavior { get; set; }

        /// <summary>
        /// Monitored item watchdog timeout
        /// </summary>
        public TimeSpan? MonitoredItemWatchdogTimeout { get; set; }

        /// <summary>
        /// Whether to run the watchdog action when any item
        /// is late or all items are late.
        /// </summary>
        public MonitoredItemWatchdogCondition? WatchdogCondition { get; set; }
    }
}
