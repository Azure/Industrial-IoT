// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using System;

    /// <summary>
    /// Represents a standard OPC UA Subscription
    /// </summary>
    public class SubscriptionConfigurationModel {

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
        /// Max notifications per publish
        /// </summary>
        public uint? MaxNotificationsPerPublish { get; set; }

        /// <summary>
        /// Priority
        /// </summary>
        public byte? Priority { get; set; }

        /// <summary>
        /// Resolves the display names for the monitored items
        /// </summary>
        public bool? ResolveDisplayName { get; set; }
    }
}