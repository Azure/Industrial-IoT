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
    public sealed record class SubscriptionModel
    {
        /// <summary>
        /// Publishing interval
        /// </summary>
        public TimeSpan? PublishingInterval { get; init; }

        /// <summary>
        /// Life time
        /// </summary>
        public uint? LifetimeCount { get; init; }

        /// <summary>
        /// Max keep alive count
        /// </summary>
        public uint? KeepAliveCount { get; init; }

        /// <summary>
        /// Priority
        /// </summary>
        public byte? Priority { get; init; }

        /// <summary>
        /// Max notification per publish
        /// </summary>
        public uint? MaxNotificationsPerPublish { get; init; }

        /// <summary>
        /// Retrieve paths from root for all monitored items
        /// in the subscription.
        /// </summary>
        public bool ResolveBrowsePathFromRoot { get; init; }

        /// <summary>
        /// Use deferred acknoledgements
        /// </summary>
        public bool? UseDeferredAcknoledgements { get; init; }

        /// <summary>
        /// Will set the subscription to have publishing
        /// enabled and every monitored item created to be
        /// in desired monitoring mode.
        /// </summary>
        public bool EnableImmediatePublishing { get; init; }

        /// <summary>
        /// Use the sequential publishing feature in the stack.
        /// </summary>
        public bool EnableSequentialPublishing { get; init; }

        /// <summary>
        /// Republish after transfer
        /// </summary>
        public bool? RepublishAfterTransfer { get; init; }

        /// <summary>
        /// Subscription watchdog behavior
        /// </summary>
        public SubscriptionWatchdogBehavior? WatchdogBehavior { get; init; }

        /// <summary>
        /// Monitored item watchdog timeout
        /// </summary>
        public TimeSpan? MonitoredItemWatchdogTimeout { get; init; }

        /// <summary>
        /// Whether to run the watchdog action when any item
        /// is late or all items are late.
        /// </summary>
        public MonitoredItemWatchdogCondition? WatchdogCondition { get; init; }

        /// <summary>
        /// The metadata header information or null if disabled.
        /// 
        /// TODO: Call into the subscriptions to collect metadata
        /// </summary>
        public DataSetMetaDataModel? MetaData { get; set; }

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
    }
}
