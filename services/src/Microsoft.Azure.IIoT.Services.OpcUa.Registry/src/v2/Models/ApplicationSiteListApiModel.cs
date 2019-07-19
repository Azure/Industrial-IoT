// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// List of application sites
    /// </summary>
    public class ApplicationSiteListApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationSiteListApiModel() {
            Sites = new List<string>();
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ApplicationSiteListApiModel(ApplicationSiteListModel model) {
            ContinuationToken = model.ContinuationToken;
            Sites = model.Sites;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public ApplicationSiteListModel ToServiceModel() {
            return new ApplicationSiteListModel {
                ContinuationToken = ContinuationToken,
                Sites = Sites
            };
        }

        /// <summary>
        /// Distinct list of sites applications were registered in.
        /// </summary>
        [JsonProperty(PropertyName = "sites")]
        public List<string> Sites { get; set; }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationToken { get; set; }
    }
}
