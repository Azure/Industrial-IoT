// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Collections.Generic;

    /// <summary>
    /// An activated monitored item subscription on an endpoint
    /// </summary>
    public class SubscriptionInfoModel {

        /// <summary>
        /// Connection configuration
        /// </summary>
        public ConnectionModel Connection { get; set; }

        /// <summary>
        /// Subscription configuration
        /// </summary>
        public SubscriptionModel Subscription { get; set; }

        /// <summary>
        /// Extra fields in each message
        /// </summary>
        public Dictionary<string, string> ExtraFields { get; set; }

        /// <summary>
        /// Messaging mode - defaults to monitoreditem
        /// </summary>
        public MessageModes? MessageMode { get; set; }
    }
}