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
    /// Endpoint registration list
    /// </summary>
    public class EndpointInfoListApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public EndpointInfoListApiModel() {
            Items = new List<EndpointInfoApiModel>();
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public EndpointInfoListApiModel(EndpointInfoListModel model) {
            ContinuationToken = model.ContinuationToken;
            if (model.Items != null) {
                Items = model.Items
                    .Select(s => new EndpointInfoApiModel(s))
                    .ToList();
            }
            else {
                Items = new List<EndpointInfoApiModel>();
            }
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public EndpointInfoListModel ToServiceModel() {
            return new EndpointInfoListModel {
                ContinuationToken = ContinuationToken,
                Items = Items.Select(s => s.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Endpoint registrations
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public List<EndpointInfoApiModel> Items { get; set; }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationToken { get; set; }
    }
}
