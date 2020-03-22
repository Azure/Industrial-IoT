// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;

    /// <summary>
    /// Data set source akin to a monitored item subscription.
    /// </summary>
    [DataContract]
    public class PublishedDataSetSourceApiModel {

        /// <summary>
        /// Either published data variables
        /// </summary>
        [DataMember(Name = "publishedVariables",
            EmitDefaultValue = false)]
        public PublishedDataItemsApiModel PublishedVariables { get; set; }

        /// <summary>
        /// Or published events data
        /// </summary>
        [DataMember(Name = "publishedEvents",
            EmitDefaultValue = false)]
        public PublishedDataSetEventsApiModel PublishedEvents { get; set; }

        /// <summary>
        /// Connection information (publisher extension)
        /// </summary>
        [DataMember(Name = "connection")]
        public ConnectionApiModel Connection { get; set; }

        /// <summary>
        /// Subscription settings (publisher extension)
        /// </summary>
        [DataMember(Name = "subscriptionSettings",
            EmitDefaultValue = false)]
        public PublishedDataSetSettingsApiModel SubscriptionSettings { get; set; }
    }
}