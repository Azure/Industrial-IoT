// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Session diagnostics
    /// </summary>
    [DataContract]
    public record class SessionDiagnosticsModel
    {
        /// <summary>
        /// Session id
        /// </summary>
        [DataMember(Name = "sessionId", Order = 0,
            EmitDefaultValue = false)]
        public string? SessionId { get; init; }

        /// <summary>
        /// Session name
        /// </summary>
        [DataMember(Name = "sessionName", Order = 1,
            EmitDefaultValue = false)]
        public string? SessionName { get; init; }

        /// <summary>
        /// Server uri
        /// </summary>
        [DataMember(Name = "serverUri", Order = 2,
            EmitDefaultValue = false)]
        public string? ServerUri { get; init; }

        /// <summary>
        /// Actual session timeout
        /// </summary>
        [DataMember(Name = "actualSessionTimeout", Order = 3,
            EmitDefaultValue = false)]
        public double ActualSessionTimeout { get; init; }

        /// <summary>
        /// Max response message size
        /// </summary>
        [DataMember(Name = "maxResponseMessageSize", Order = 8,
            EmitDefaultValue = false)]
        public uint MaxResponseMessageSize { get; init; }

        /// <summary>
        /// Connection established
        /// </summary>
        [DataMember(Name = "connectTime", Order = 9,
            EmitDefaultValue = false)]
        public DateTime ConnectTime { get; init; }

        /// <summary>
        /// Last contact
        /// </summary>
        [DataMember(Name = "lastContactTime", Order = 10,
            EmitDefaultValue = false)]
        public DateTime LastContactTime { get; init; }

        /// <summary>
        /// Current subscriptions count
        /// </summary>
        [DataMember(Name = "currentSubscriptionsCount", Order = 11,
            EmitDefaultValue = false)]
        public uint CurrentSubscriptionsCount { get; init; }

        /// <summary>
        /// Current monitored items count
        /// </summary>
        [DataMember(Name = "currentMonitoredItemsCount", Order = 12,
            EmitDefaultValue = false)]
        public uint CurrentMonitoredItemsCount { get; init; }

        /// <summary>
        /// Current publish requests in queue
        /// </summary>
        [DataMember(Name = "currentPublishRequestsInQueue", Order = 13,
            EmitDefaultValue = false)]
        public uint CurrentPublishRequestsInQueue { get; init; }

        /// <summary>
        /// Total request count
        /// </summary>
        [DataMember(Name = "totalRequestCount", Order = 14,
            EmitDefaultValue = false)]
        public ServiceCounterModel? TotalRequestCount { get; init; }

        /// <summary>
        /// Unauthorized request count
        /// </summary>
        [DataMember(Name = "unauthorizedRequestCount", Order = 15,
            EmitDefaultValue = false)]
        public uint UnauthorizedRequestCount { get; init; }

        /// <summary>
        /// Read count
        /// </summary>
        [DataMember(Name = "readCount", Order = 16,
            EmitDefaultValue = false)]
        public ServiceCounterModel? ReadCount { get; init; }

        /// <summary>
        /// History read counts
        /// </summary>
        [DataMember(Name = "historyReadCount", Order = 17,
            EmitDefaultValue = false)]
        public ServiceCounterModel? HistoryReadCount { get; init; }

        /// <summary>
        /// Write counts
        /// </summary>
        [DataMember(Name = "writeCount", Order = 18,
            EmitDefaultValue = false)]
        public ServiceCounterModel? WriteCount { get; init; }

        /// <summary>
        /// History update count
        /// </summary>
        [DataMember(Name = "historyUpdateCount", Order = 19,
            EmitDefaultValue = false)]
        public ServiceCounterModel? HistoryUpdateCount { get; init; }

        /// <summary>
        /// Call count
        /// </summary>
        [DataMember(Name = "callCount", Order = 20,
            EmitDefaultValue = false)]
        public ServiceCounterModel? CallCount { get; init; }

        /// <summary>
        /// Create monitored item count
        /// </summary>
        [DataMember(Name = "createMonitoredItemsCount", Order = 21,
            EmitDefaultValue = false)]
        public ServiceCounterModel? CreateMonitoredItemsCount { get; init; }

        /// <summary>
        /// Modify monitored item counts
        /// </summary>
        [DataMember(Name = "modifyMonitoredItemsCount", Order = 22,
            EmitDefaultValue = false)]
        public ServiceCounterModel? ModifyMonitoredItemsCount { get; init; }

        /// <summary>
        /// Set monitoring mode counts
        /// </summary>
        [DataMember(Name = "setMonitoringModeCount", Order = 23,
            EmitDefaultValue = false)]
        public ServiceCounterModel? SetMonitoringModeCount { get; init; }

        /// <summary>
        /// Set triggering counts
        /// </summary>
        [DataMember(Name = "setTriggeringCount", Order = 24,
            EmitDefaultValue = false)]
        public ServiceCounterModel? SetTriggeringCount { get; init; }

        /// <summary>
        /// Delete monitored items counts
        /// </summary>
        [DataMember(Name = "deleteMonitoredItemsCount", Order = 25,
            EmitDefaultValue = false)]
        public ServiceCounterModel? DeleteMonitoredItemsCount { get; init; }

        /// <summary>
        /// Create Subscription count
        /// </summary>
        [DataMember(Name = "createSubscriptionCount", Order = 26,
            EmitDefaultValue = false)]
        public ServiceCounterModel? CreateSubscriptionCount { get; init; }

        /// <summary>
        /// Modify subscription count
        /// </summary>
        [DataMember(Name = "modifySubscriptionCount", Order = 27,
            EmitDefaultValue = false)]
        public ServiceCounterModel? ModifySubscriptionCount { get; init; }

        /// <summary>
        /// Set publishing mode count
        /// </summary>
        [DataMember(Name = "setPublishingModeCount", Order = 28,
            EmitDefaultValue = false)]
        public ServiceCounterModel? SetPublishingModeCount { get; init; }

        /// <summary>
        /// Publish counts
        /// </summary>
        [DataMember(Name = "publishCount", Order = 29,
            EmitDefaultValue = false)]
        public ServiceCounterModel? PublishCount { get; init; }

        /// <summary>
        /// Republish count
        /// </summary>
        [DataMember(Name = "republishCount", Order = 30,
            EmitDefaultValue = false)]
        public ServiceCounterModel? RepublishCount { get; init; }

        /// <summary>
        /// Transfer subscriptions count
        /// </summary>
        [DataMember(Name = "transferSubscriptionsCount", Order = 31,
            EmitDefaultValue = false)]
        public ServiceCounterModel? TransferSubscriptionsCount { get; init; }

        /// <summary>
        /// Delete subscriptions count
        /// </summary>
        [DataMember(Name = "deleteSubscriptionsCount", Order = 32,
            EmitDefaultValue = false)]
        public ServiceCounterModel? DeleteSubscriptionsCount { get; init; }

        /// <summary>
        /// Add nodes count
        /// </summary>
        [DataMember(Name = "addNodesCount", Order = 33,
            EmitDefaultValue = false)]
        public ServiceCounterModel? AddNodesCount { get; init; }

        /// <summary>
        /// Add References count
        /// </summary>
        [DataMember(Name = "addReferencesCount", Order = 34,
            EmitDefaultValue = false)]
        public ServiceCounterModel? AddReferencesCount { get; init; }

        /// <summary>
        /// Delete nodes count
        /// </summary>
        [DataMember(Name = "deleteNodesCount", Order = 35,
            EmitDefaultValue = false)]
        public ServiceCounterModel? DeleteNodesCount { get; init; }

        /// <summary>
        /// Delete References count
        /// </summary>
        [DataMember(Name = "deleteReferencesCount", Order = 36,
            EmitDefaultValue = false)]
        public ServiceCounterModel? DeleteReferencesCount { get; init; }

        /// <summary>
        /// Browse count
        /// </summary>
        [DataMember(Name = "browseCount", Order = 37,
            EmitDefaultValue = false)]
        public ServiceCounterModel? BrowseCount { get; init; }

        /// <summary>
        /// Browse next count
        /// </summary>
        [DataMember(Name = "browseNextCount", Order = 38,
            EmitDefaultValue = false)]
        public ServiceCounterModel? BrowseNextCount { get; init; }

        /// <summary>
        /// Translate browse paths to node ids count
        /// </summary>
        [DataMember(Name = "translateBrowsePathsToNodeIdsCount", Order = 39,
            EmitDefaultValue = false)]
        public ServiceCounterModel? TranslateBrowsePathsToNodeIdsCount { get; init; }

        /// <summary>
        /// Query first count
        /// </summary>
        [DataMember(Name = "queryFirstCount", Order = 40,
            EmitDefaultValue = false)]
        public ServiceCounterModel? QueryFirstCount { get; init; }

        /// <summary>
        /// Query next count
        /// </summary>
        [DataMember(Name = "queryNextCount", Order = 41,
            EmitDefaultValue = false)]
        public ServiceCounterModel? QueryNextCount { get; init; }

        /// <summary>
        /// Register nodes count
        /// </summary>
        [DataMember(Name = "registerNodesCount", Order = 42,
            EmitDefaultValue = false)]
        public ServiceCounterModel? RegisterNodesCount { get; init; }

        /// <summary>
        /// Unregister nodes count
        /// </summary>
        [DataMember(Name = "unregisterNodesCount", Order = 43,
            EmitDefaultValue = false)]
        public ServiceCounterModel? UnregisterNodesCount { get; init; }
    }
}
