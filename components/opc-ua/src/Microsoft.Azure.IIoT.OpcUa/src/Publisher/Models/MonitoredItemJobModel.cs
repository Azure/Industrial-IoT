// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Monitored item subscription job
    /// </summary>
    public class MonitoredItemJobModel {

        /// <summary>
        /// Subscriptions that are part of the job
        /// </summary>
        public List<SubscriptionInfoModel> Subscriptions { get; set; }

        /// <summary>
        /// Defines the content and encoding of the published messages
        /// </summary>
        public MonitoredItemMessageContentModel Content { get; set; }

        /// <summary>
        /// Engine configuration
        /// </summary>
        public EngineConfigurationModel Engine { get; set; }
    }
}