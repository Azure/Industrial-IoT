// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Configuration of monitored item jobs
    /// </summary>
    public class MonitoredItemJobApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public MonitoredItemJobApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public MonitoredItemJobApiModel(MonitoredItemJobModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Subscriptions = model.Subscriptions?
                .Select(d => new SubscriptionInfoApiModel(d))
                .ToList();
            EngineConfiguration = model.Engine == null ? null :
                new EngineConfigurationApiModel(model.Engine);
            Content = model.Content == null ? null :
                new MonitoredItemMessageContentApiModel(model.Content);
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public MonitoredItemJobModel ToServiceModel() {
            return new MonitoredItemJobModel {
                Content = Content?.ToServiceModel(),
                Subscriptions = Subscriptions?
                    .Select(d => d.ToServiceModel())
                    .ToList(),
                Engine = EngineConfiguration?.ToServiceModel()
            };
        }

        /// <summary>
        /// Subscriptions to set up
        /// </summary>
        [JsonProperty(PropertyName = "subscriptions")]
        public List<SubscriptionInfoApiModel> Subscriptions { get; set; }

        /// <summary>
        /// Defines the content and encoding of the published messages
        /// </summary>
        [JsonProperty(PropertyName = "content",
            NullValueHandling = NullValueHandling.Ignore)]
        public MonitoredItemMessageContentApiModel Content { get; set; }

        /// <summary>
        /// Engine configuration
        /// </summary>
        [JsonProperty(PropertyName = "engineConfiguration",
            NullValueHandling = NullValueHandling.Ignore)]
        public EngineConfigurationApiModel EngineConfiguration { get; set; }
    }
}