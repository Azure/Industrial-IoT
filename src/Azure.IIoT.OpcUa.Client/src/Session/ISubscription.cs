// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription services
    /// </summary>
    public interface ISubscription
    {
        /// <summary>
        /// Set keep alive count
        /// </summary>
        uint KeepAliveCount { get; set; }

        /// <summary>
        /// The life time of the subscription in counts of
        /// publish interval.
        /// LifetimeCount shall be at least 3*KeepAliveCount.
        /// </summary>
        uint LifetimeCount { get; set; }

        /// <summary>
        /// Set desired priority of the subscription
        /// </summary>
        byte Priority { get; set; }

        /// <summary>
        /// Set desired publishing interval
        /// </summary>
        TimeSpan PublishingInterval { get; set; }

        /// <summary>
        /// Set desired publishing enabled
        /// </summary>
        bool PublishingEnabled { get; set; }

        /// <summary>
        /// Set max notifications per publish
        /// </summary>
        uint MaxNotificationsPerPublish { get; set; }

        /// <summary>
        /// Set min lifetime interval
        /// </summary>
        TimeSpan MinLifetimeInterval { get; set; }

        /// <summary>
        /// Monitored item count
        /// </summary>
        uint MonitoredItemCount { get; }

        /// <summary>
        /// Monitored items
        /// </summary>
        IEnumerable<MonitoredItem> MonitoredItems { get; }

        /// <summary>
        /// Created subscription
        /// </summary>
        bool Created { get; }

        /// <summary>
        /// The current publishing interval on the server
        /// </summary>
        TimeSpan CurrentPublishingInterval { get; }

        /// <summary>
        /// The current priority of the subscription
        /// </summary>
        byte CurrentPriority { get; }

        /// <summary>
        /// The current lifetime count on the server
        /// </summary>
        uint CurrentLifetimeCount { get; }

        /// <summary>
        /// The current keep alive count on the server
        /// </summary>
        uint CurrentKeepAliveCount { get; }

        /// <summary>
        /// The current publishing enabled state
        /// </summary>
        bool CurrentPublishingEnabled { get; }

        /// <summary>
        /// Current max notifications per publish
        /// </summary>
        uint CurrentMaxNotificationsPerPublish { get; }

        /// <summary>
        /// Create or update the subscription
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask ApplyChangesAsync(CancellationToken ct = default);

        /// <summary>
        /// Add a monitored item to the subscription
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        MonitoredItem AddMonitoredItem(IOptionsMonitor<MonitoredItemOptions> options);

        /// <summary>
        /// Apply monitored item changes
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask ApplyMonitoredItemChangesAsync(CancellationToken ct = default);

        /// <summary>
        /// Refresh all conditions
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask ConditionRefreshAsync(CancellationToken ct = default);

        /// <summary>
        /// Delete the subscription on the server
        /// </summary>
        /// <param name="silent"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask DeleteAsync(bool silent, CancellationToken ct = default);
    }
}
