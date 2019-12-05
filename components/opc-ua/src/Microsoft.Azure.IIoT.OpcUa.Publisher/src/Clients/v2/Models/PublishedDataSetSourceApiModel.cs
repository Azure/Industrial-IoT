// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Data set source akin to a monitored item subscription.
    /// </summary>
    public class PublishedDataSetSourceApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishedDataSetSourceApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishedDataSetSourceApiModel(PublishedDataSetSourceModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Connection = model.Connection == null ? null :
                new ConnectionApiModel(model.Connection);
            PublishedVariables = model.PublishedVariables == null ? null :
                new PublishedDataItemsApiModel(model.PublishedVariables);
            PublishedEvents = model.PublishedEvents == null ? null :
                new PublishedDataSetEventsApiModel(model.PublishedEvents);
            SubscriptionSettings = model.SubscriptionSettings == null ? null :
                new PublishedDataSetSettingsApiModel(model.SubscriptionSettings);

        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishedDataSetSourceModel ToServiceModel() {
            return new PublishedDataSetSourceModel {
                Connection = Connection?.ToServiceModel(),
                PublishedEvents = PublishedEvents?.ToServiceModel(),
                PublishedVariables = PublishedVariables?.ToServiceModel(),
                SubscriptionSettings = SubscriptionSettings?.ToServiceModel()
            };
        }

        /// <summary>
        /// Either published data variables
        /// </summary>
        [JsonProperty(PropertyName = "publishedVariables",
            NullValueHandling = NullValueHandling.Ignore)]
        public PublishedDataItemsApiModel PublishedVariables { get; set; }

        /// <summary>
        /// Or published events data
        /// </summary>
        [JsonProperty(PropertyName = "publishedEvents",
            NullValueHandling = NullValueHandling.Ignore)]
        public PublishedDataSetEventsApiModel PublishedEvents { get; set; }

        /// <summary>
        /// Connection information (publisher extension)
        /// </summary>
        [JsonProperty(PropertyName = "connection")]
        public ConnectionApiModel Connection { get; set; }

        /// <summary>
        /// Subscription settings (publisher extension)
        /// </summary>
        [JsonProperty(PropertyName = "subscriptionSettings",
            NullValueHandling = NullValueHandling.Ignore)]
        public PublishedDataSetSettingsApiModel SubscriptionSettings { get; set; }
    }
}