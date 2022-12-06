// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Data set source akin to a monitored item subscription.
    /// </summary>
    public class PublishedDataSetSourceModel {

        /// <summary>
        /// Either published data variables
        /// </summary>
        public PublishedDataItemsModel PublishedVariables { get; set; }

        /// <summary>
        /// Or published events data
        /// </summary>
        public PublishedEventItemsModel PublishedEvents { get; set; }

        /// <summary>
        /// Connection information (publisher extension)
        /// </summary>
        public ConnectionModel Connection { get; set; }

        /// <summary>
        /// Subscription settings (publisher extension)
        /// </summary>
        public PublishedDataSetSettingsModel SubscriptionSettings { get; set; }
    }
}