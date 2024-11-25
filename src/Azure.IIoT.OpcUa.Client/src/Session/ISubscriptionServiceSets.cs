// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Opc.Ua;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription context
    /// </summary>
    public interface ISubscriptionServiceSets
    {
        /// <summary>
        /// Create subscription
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="requestedPublishingInterval"></param>
        /// <param name="requestedLifetimeCount"></param>
        /// <param name="requestedMaxKeepAliveCount"></param>
        /// <param name="maxNotificationsPerPublish"></param>
        /// <param name="publishingEnabled"></param>
        /// <param name="priority"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<CreateSubscriptionResponse> CreateSubscriptionAsync(RequestHeader? requestHeader,
            double requestedPublishingInterval, uint requestedLifetimeCount,
            uint requestedMaxKeepAliveCount, uint maxNotificationsPerPublish, bool publishingEnabled,
            byte priority, CancellationToken ct = default);

        /// <summary>
        /// Modify subscription
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="requestedPublishingInterval"></param>
        /// <param name="requestedLifetimeCount"></param>
        /// <param name="requestedMaxKeepAliveCount"></param>
        /// <param name="maxNotificationsPerPublish"></param>
        /// <param name="priority"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ModifySubscriptionResponse> ModifySubscriptionAsync(RequestHeader? requestHeader,
            uint subscriptionId, double requestedPublishingInterval, uint requestedLifetimeCount,
            uint requestedMaxKeepAliveCount, uint maxNotificationsPerPublish,
            byte priority, CancellationToken ct = default);

        /// <summary>
        /// Set Publishing mode
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="publishingEnabled"></param>
        /// <param name="subscriptionIds"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<SetPublishingModeResponse> SetPublishingModeAsync(RequestHeader? requestHeader,
            bool publishingEnabled, UInt32Collection subscriptionIds, CancellationToken ct = default);

        /// <summary>
        /// Republish service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="retransmitSequenceNumber"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<RepublishResponse> RepublishAsync(RequestHeader? requestHeader,
            uint subscriptionId, uint retransmitSequenceNumber, CancellationToken ct = default);

        /// <summary>
        /// Set monitoring
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="monitoringMode"></param>
        /// <param name="monitoredItemIds"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<SetMonitoringModeResponse> SetMonitoringModeAsync(RequestHeader? requestHeader,
            uint subscriptionId, MonitoringMode monitoringMode, UInt32Collection monitoredItemIds,
            CancellationToken ct = default);

        /// <summary>
        /// Create monitored items services
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="itemsToCreate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<CreateMonitoredItemsResponse> CreateMonitoredItemsAsync(RequestHeader? requestHeader,
            uint subscriptionId, TimestampsToReturn timestampsToReturn,
            MonitoredItemCreateRequestCollection itemsToCreate, CancellationToken ct = default);

        /// <summary>
        /// Modify monitored item service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="itemsToModify"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ModifyMonitoredItemsResponse> ModifyMonitoredItemsAsync(RequestHeader? requestHeader,
            uint subscriptionId, TimestampsToReturn timestampsToReturn,
            MonitoredItemModifyRequestCollection itemsToModify, CancellationToken ct = default);

        /// <summary>
        /// Delete monitored items service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="monitoredItemIds"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DeleteMonitoredItemsResponse> DeleteMonitoredItemsAsync(RequestHeader? requestHeader,
            uint subscriptionId, UInt32Collection monitoredItemIds, CancellationToken ct = default);

        /// <summary>
        /// Delete subscription service
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionIds"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DeleteSubscriptionsResponse> DeleteSubscriptionsAsync(RequestHeader? requestHeader,
            UInt32Collection subscriptionIds, CancellationToken ct = default);
    }
}
