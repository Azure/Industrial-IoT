// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Gateway registration list
    /// </summary>
    public class GatewayListApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public GatewayListApiModel() {
            Items = new List<GatewayApiModel>();
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public GatewayListApiModel(GatewayListModel model) {
            ContinuationToken = model.ContinuationToken;
            if (model.Items != null) {
                Items = model.Items
                    .Select(s => new GatewayApiModel(s))
                    .ToList();
            }
            else {
                Items = new List<GatewayApiModel>();
            }
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public GatewayListModel ToServiceModel() {
            return new GatewayListModel {
                ContinuationToken = ContinuationToken,
                Items = Items.Select(s => s.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Registrations
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public List<GatewayApiModel> Items { get; set; }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationToken { get; set; }
    }
}
