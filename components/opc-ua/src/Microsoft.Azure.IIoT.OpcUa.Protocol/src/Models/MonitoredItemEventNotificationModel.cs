// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Monitored item event notification
    /// </summary>
    public class MonitoredItemEventNotificationModel : MonitoredItemNotificationModel {
        /// <summary>
        /// The value of the node.
        /// </summary>
        public List<EventValueModel> EventValues { get; set; }

        /// <summary>
        /// Ctor of the object.
        /// </summary>
        public MonitoredItemEventNotificationModel() {
            EventValues = new List<EventValueModel>();
        }
    }
}
