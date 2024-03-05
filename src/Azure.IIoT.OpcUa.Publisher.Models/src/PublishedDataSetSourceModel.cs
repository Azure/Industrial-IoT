// ------------------------------------------------------------
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
        /// Connection information (publisher extension)
        /// </summary>
        [DataMember(Name = "connection", Order = 5)]
        public ConnectionModel? Connection { get; init; }

        /// <summary>
        /// Subscription settings (publisher extension)
        /// </summary>
        [DataMember(Name = "subscriptionSettings", Order = 6,
            EmitDefaultValue = false)]
        public PublishedDataSetSettingsModel? SubscriptionSettings { get; init; }

        /// <summary>
        /// Either published data variables
        /// </summary>
        [DataMember(Name = "publishedVariables", Order = 7,
            EmitDefaultValue = false)]
        public PublishedDataItemsModel? PublishedVariables { get; set; }

        /// <summary>
        /// Or published events data
        /// </summary>
        [DataMember(Name = "publishedEvents", Order = 8,
            EmitDefaultValue = false)]
        public PublishedEventItemsModel? PublishedEvents { get; set; }

        /// <summary>
        /// Or published objects (publisher extension)
        /// </summary>
        [DataMember(Name = "publishedObjects", Order = 9,
            EmitDefaultValue = false)]
        public PublishedObjectItemsModel? PublishedObjects { get; set; }
    }
}
