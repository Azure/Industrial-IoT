﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Data set source akin to a monitored item subscription.
    /// </summary>
    [DataContract]
    public sealed record class PublishedDataSetSourceModel
    {
        /// <summary>
        /// Either published data variables
        /// </summary>
        [DataMember(Name = "publishedVariables", Order = 0,
            EmitDefaultValue = false)]
        public PublishedDataItemsModel? PublishedVariables { get; set; }

        /// <summary>
        /// Or published events data
        /// </summary>
        [DataMember(Name = "publishedEvents", Order = 1,
            EmitDefaultValue = false)]
        public PublishedEventItemsModel? PublishedEvents { get; set; }

        /// <summary>
        /// Connection information (publisher extension)
        /// </summary>
        [DataMember(Name = "connection", Order = 2)]
        public ConnectionModel? Connection { get; set; }

        /// <summary>
        /// Subscription settings (publisher extension)
        /// </summary>
        [DataMember(Name = "subscriptionSettings", Order = 3,
            EmitDefaultValue = false)]
        public PublishedDataSetSettingsModel? SubscriptionSettings { get; set; }
    }
}
