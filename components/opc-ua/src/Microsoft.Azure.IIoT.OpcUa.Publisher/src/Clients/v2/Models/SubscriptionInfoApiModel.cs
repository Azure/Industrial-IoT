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
    /// Subscription model
    /// </summary>
    public class SubscriptionInfoApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public SubscriptionInfoApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public SubscriptionInfoApiModel(SubscriptionInfoModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            MessageMode = model.MessageMode;
            ExtraFields = model.ExtraFields?
                .ToDictionary(k => k.Key, v => v.Value);
            Subscription = model.Subscription == null ? null :
                new SubscriptionApiModel(model.Subscription);
            Connection = model.Connection == null ? null :
                new ConnectionApiModel(model.Connection);
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public SubscriptionInfoModel ToServiceModel() {
            return new SubscriptionInfoModel {
                MessageMode = MessageMode,
                ExtraFields = ExtraFields?
                    .ToDictionary(k => k.Key, v => v.Value),
                Subscription = Subscription?.ToServiceModel(),
                Connection = Connection?.ToServiceModel()
            };
        }

        /// <summary>
        /// Connection information
        /// </summary>
        [JsonProperty(PropertyName = "connection")]
        public ConnectionApiModel Connection { get; set; }

        /// <summary>
        /// Subscription
        /// </summary>
        [JsonProperty(PropertyName = "subscription")]
        public SubscriptionApiModel Subscription { get; set; }

        /// <summary>
        /// Extra fields in each message
        /// </summary>
        [JsonProperty(PropertyName = "extraFields",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> ExtraFields { get; set; }

        /// <summary>
        /// Messaging mode - defaults to monitoreditem
        /// </summary>
        [JsonProperty(PropertyName = "messageMode",
            NullValueHandling = NullValueHandling.Ignore)]
        public MessageModes? MessageMode { get; set; }
    }
}