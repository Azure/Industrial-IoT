// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Opc Subscription model
    /// </summary>
    public class SubscriptionApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public SubscriptionApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public SubscriptionApiModel(SubscriptionModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            LifeTimeCount = model.LifeTimeCount;
            MonitoredItems = model.MonitoredItems?
                .Select(d => new MonitoredItemApiModel(d))
                .ToList();
            MaxKeepAliveCount = model.MaxKeepAliveCount;
            MaxNotificationsPerPublish = model.MaxNotificationsPerPublish;
            Id = model.Id;
            Priority = model.Priority;
            PublishingInterval = model.PublishingInterval;
            PublishingDisabled = model.PublishingDisabled;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public SubscriptionModel ToServiceModel() {
            return new SubscriptionModel {
                LifeTimeCount = LifeTimeCount,
                MaxKeepAliveCount = MaxKeepAliveCount,
                MaxNotificationsPerPublish = MaxNotificationsPerPublish,
                MonitoredItems = MonitoredItems?
                    .Select(i => i.ToServiceModel())
                    .ToList(),
                Id = Id,
                Priority = Priority,
                PublishingDisabled = PublishingDisabled,
                PublishingInterval = PublishingInterval,
            };
        }


        /// <summary>
        /// Monitored items
        /// </summary>
        [JsonProperty(PropertyName = "monitoredItems")]
        public List<MonitoredItemApiModel> MonitoredItems { get; set; }

        /// <summary>
        /// Id of the subscription
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Publishing interval
        /// </summary>
        [JsonProperty(PropertyName = "publishingInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? PublishingInterval { get; set; }

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

        /// <summary>
        /// Publishing disabled
        /// </summary>
        [JsonProperty(PropertyName = "publishingDisabled",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? PublishingDisabled { get; set; }
    }
}