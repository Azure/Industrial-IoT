// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Represents a standard OPC UA Subscription
    /// </summary>
    public class SubscriptionModel {

        /// <summary>
        /// Id of the subscription
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Monitored items in the subscription
        /// </summary>
        public List<MonitoredItemModel> MonitoredItems { get; set; }

        /// <summary>
        /// Publishing interval
        /// </summary>
        public int? PublishingInterval { get; set; }

        /// <summary>
        /// Life time
        /// </summary>
        public uint? LifeTimeCount { get; set; }

        /// <summary>
        /// Max keep alive count
        /// </summary>
        public uint? MaxKeepAliveCount { get; set; }

        /// <summary>
        /// Max notifications per publish
        /// </summary>
        public uint? MaxNotificationsPerPublish { get; set; }

        /// <summary>
        /// Priority
        /// </summary>
        public byte? Priority { get; set; }

        /// <summary>
        /// Publishing disabled
        /// </summary>
        public bool? PublishingDisabled { get; set; }
    }
}