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
    /// List of registered applications
    /// </summary>
    public class ApplicationInfoListApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationInfoListApiModel() {
            Items = new List<ApplicationInfoApiModel>();
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ApplicationInfoListApiModel(ApplicationInfoListModel model) {
            ContinuationToken = model.ContinuationToken;
            if (model.Items != null) {
                Items = model.Items
                    .Select(s => new ApplicationInfoApiModel(s))
                    .ToList();
            }
            else {
                Items = new List<ApplicationInfoApiModel>();
            }
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public ApplicationInfoListModel ToServiceModel() {
            return new ApplicationInfoListModel {
                ContinuationToken = ContinuationToken,
                Items = Items.Select(s => s.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Application infos
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public List<ApplicationInfoApiModel> Items { get; set; }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationToken { get; set; }
    }
}
