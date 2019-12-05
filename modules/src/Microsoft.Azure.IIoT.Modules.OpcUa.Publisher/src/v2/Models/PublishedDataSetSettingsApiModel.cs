// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Published dataset settings - corresponds to SubscriptionModel
    /// </summary>
    public class PublishedDataSetSettingsApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishedDataSetSettingsApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishedDataSetSettingsApiModel(PublishedDataSetSettingsModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            LifeTimeCount = model.LifeTimeCount;
            MaxKeepAliveCount = model.MaxKeepAliveCount;
            MaxNotificationsPerPublish = model.MaxNotificationsPerPublish;
            Priority = model.Priority;
            PublishingInterval = model.PublishingInterval;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishedDataSetSettingsModel ToServiceModel() {
            return new PublishedDataSetSettingsModel {
                LifeTimeCount = LifeTimeCount,
                MaxKeepAliveCount = MaxKeepAliveCount,
                MaxNotificationsPerPublish = MaxNotificationsPerPublish,
                Priority = Priority,
                PublishingInterval = PublishingInterval
            };
        }

        /// <summary>
        /// Publishing interval
        /// </summary>
        [JsonProperty(PropertyName = "publishingInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? PublishingInterval { get; set; }

        /// <summary>
        /// Life time
        /// </summary>
        [JsonProperty(PropertyName = "lifeTimeCount",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? LifeTimeCount { get; set; }

        /// <summary>
        /// Max keep alive count
        /// </summary>
        [JsonProperty(PropertyName = "maxKeepAliveCount",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? MaxKeepAliveCount { get; set; }

        /// <summary>
        /// Max notifications per publish
        /// </summary>
        [JsonProperty(PropertyName = "maxNotificationsPerPublish",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? MaxNotificationsPerPublish { get; set; }

        /// <summary>
        /// Priority
        /// </summary>
        [JsonProperty(PropertyName = "priority",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte? Priority { get; set; }
    }
}