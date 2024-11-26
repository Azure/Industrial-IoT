// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;

    /// <summary>
    /// Subscription options
    /// </summary>
    public record class SubscriptionOptions
    {
        /// <summary>
        /// Set keep alive count
        /// </summary>
        public uint KeepAliveCount { get; init; }

        /// <summary>
        /// The life time of the subscription in counts of
        /// publish interval.
        /// LifetimeCount shall be at least 3*KeepAliveCount.
        /// </summary>
        public uint LifetimeCount { get; init; }

        /// <summary>
        /// Set desired priority of the subscription
        /// </summary>
        public byte Priority { get; init; }

        /// <summary>
        /// Set desired publishing interval
        /// </summary>
        public TimeSpan PublishingInterval { get; init; }

        /// <summary>
        /// Set desired publishing enabled
        /// </summary>
        public bool PublishingEnabled { get; init; }

        /// <summary>
        /// Set max notifications per publish
        /// </summary>
        public uint MaxNotificationsPerPublish { get; init; }

        /// <summary>
        /// Set min lifetime interval
        /// </summary>
        public TimeSpan MinLifetimeInterval { get; init; }
    }
}
