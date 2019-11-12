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
    /// Publisher registration list
    /// </summary>
    public class PublisherListApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublisherListApiModel() {
            Items = new List<PublisherApiModel>();
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public PublisherListApiModel(PublisherListModel model) {
            ContinuationToken = model.ContinuationToken;
            if (model.Items != null) {
                Items = model.Items
                    .Select(s => new PublisherApiModel(s))
                    .ToList();
            }
            else {
                Items = new List<PublisherApiModel>();
            }
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public PublisherListModel ToServiceModel() {
            return new PublisherListModel {
                ContinuationToken = ContinuationToken,
                Items = Items.Select(s => s.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Registrations
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public List<PublisherApiModel> Items { get; set; }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationToken { get; set; }
    }
}
