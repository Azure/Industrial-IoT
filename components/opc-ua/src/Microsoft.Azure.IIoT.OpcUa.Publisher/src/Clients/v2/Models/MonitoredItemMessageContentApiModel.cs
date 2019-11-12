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
    /// Monitored item content
    /// </summary>
    public class MonitoredItemMessageContentApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public MonitoredItemMessageContentApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public MonitoredItemMessageContentApiModel(MonitoredItemMessageContentModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Encoding = model.Encoding;
            Fields = model.Fields;
            Properties = model.Properties?
                .ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public MonitoredItemMessageContentModel ToServiceModel() {
            return new MonitoredItemMessageContentModel {
                Encoding = Encoding,
                Fields = Fields,
                Properties = Properties?
                    .ToDictionary(k => k.Key, v => v.Value)
            };
        }

        /// <summary>
        /// Fields that should be encoded in the message
        /// </summary>
        [JsonProperty(PropertyName = "fields",
            NullValueHandling = NullValueHandling.Ignore)]
        public MonitoredItemMessageContentMask? Fields { get; set; }

        /// <summary>
        /// Content encoding for published messages
        /// </summary>
        [JsonProperty(PropertyName = "cncoding",
            NullValueHandling = NullValueHandling.Ignore)]
        public MonitoredItemMessageEncoding? Encoding { get; set; }

        /// <summary>
        /// Properties to include in the message
        /// </summary>
        [JsonProperty(PropertyName = "properties",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Properties { get; set; }
    }

}