// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Protocol {
    using Azure.IIoT.OpcUa.Shared.Models;
    using System;

    /// <summary>
    /// Subscription configuration
    /// </summary>
    public interface ISubscriptionConfig {

        /// <summary>
        /// The default interval for heartbeats if not configured.
        /// </summary>
        TimeSpan? DefaultHeartbeatInterval { get; }

        /// <summary>
        /// The default flag whether to skip the first value if
        /// not set on node level.
        /// </summary>
        bool DefaultSkipFirst { get; }

        /// <summary>
        /// The default flag whether to descard new items in queue
        /// </summary>
        bool DefaultDiscardNew { get; }

        /// <summary>
        /// The default sampling interval.
        /// </summary>
        TimeSpan? DefaultSamplingInterval { get; }

        /// <summary>
        /// The default publishing interval.
        /// </summary>
        TimeSpan? DefaultPublishingInterval { get; }

        /// <summary>
        /// Default subscription keep alive counter
        /// </summary>
        uint? DefaultKeepAliveCount { get; }

        /// <summary>
        /// Default subscription lifetime counter
        /// </summary>
        uint? DefaultLifeTimeCount { get; }

        /// <summary>
        /// Whether to enable or disable data set metadata explicitly
        /// </summary>
        bool? DisableDataSetMetaData { get; }

        /// <summary>
        /// Default metadata send interval.
        /// </summary>
        TimeSpan? DefaultMetaDataUpdateTime { get; }

        /// <summary>
        /// Whether to enable or disable key frames explicitly
        /// </summary>
        bool? DisableKeyFrames { get; }

        /// <summary>
        /// Default keyframe count
        /// </summary>
        uint? DefaultKeyFrameCount { get; }

        /// <summary>
        /// Flag wether to grab the display name of nodes form
        /// the OPC UA Server.
        /// </summary>
        bool ResolveDisplayName { get; }

        /// <summary>
        /// set the default queue size for monitored items. If not
        /// set the default queue size will be configured (1 for
        /// data monitored items, and 0 for event monitoring).
        /// </summary>
        uint? DefaultQueueSize { get; }

        /// <summary>
        /// set the default data change filter for monitored items. Default is
        /// status and value change triggering.
        /// </summary>
        DataChangeTriggerType? DefaultDataChangeTrigger { get; }
    }
}
