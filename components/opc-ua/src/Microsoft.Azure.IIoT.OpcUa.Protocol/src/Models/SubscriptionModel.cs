// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Collections.Generic;

    /// <summary>
    /// An activated monitored item subscription on an endpoint
    /// </summary>
    public class SubscriptionModel {

        /// <summary>
        /// Id of the subscription
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Connection configuration
        /// </summary>
        public ConnectionModel Connection { get; set; }

        /// <summary>
        /// Subscription configuration
        /// </summary>
        public SubscriptionConfigurationModel Configuration { get; set; }

        /// <summary>
        /// Monitored items in the subscription
        /// </summary>
        public List<BaseMonitoredItemModel> MonitoredItems { get; set; }

        /// <summary>
        /// Extra fields in each message
        /// </summary>
        public Dictionary<string, string> ExtensionFields { get; set; }
    }
}