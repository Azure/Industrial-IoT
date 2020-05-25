// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription abstraction
    /// </summary>
    public interface ISubscription : IDisposable {

        /// <summary>
        /// Subscription change events
        /// </summary>
        event EventHandler<SubscriptionNotificationModel> OnSubscriptionChange;

        /// <summary>
        /// Item change events
        /// </summary>
        event EventHandler<SubscriptionNotificationModel> OnMonitoredItemChange;

        /// <summary>
        /// Identifier of the subscription
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Connection
        /// </summary>
        ConnectionModel Connection { get; }

        /// <summary>
        /// Number of retries on the session
        /// </summary>
        long NumberOfConnectionRetries { get; }

        /// <summary>
        /// Create snapshot
        /// </summary>
        /// <returns></returns>
        Task<SubscriptionNotificationModel> GetSnapshotAsync();

        /// <summary>
        /// Apply desired state
        /// </summary>
        /// <param name="monitoredItems"></param>
        /// <param name="configuration"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        Task ApplyAsync(IEnumerable<MonitoredItemModel> monitoredItems,
            SubscriptionConfigurationModel configuration, bool enable);

        /// <summary>
        /// Close and delete subscription
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
    }
}