// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Opc.Ua;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Subscription context
/// </summary>
public interface ISubscriptionServiceSet : IMonitoredItemServiceSet
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
}
