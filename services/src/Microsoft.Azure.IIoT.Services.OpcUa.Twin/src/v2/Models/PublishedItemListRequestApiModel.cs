// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Request list of published items
    /// </summary>
    public class PublishedItemListRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishedItemListRequestApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishedItemListRequestApiModel(PublishedItemListRequestModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            ContinuationToken = model.ContinuationToken;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishedItemListRequestModel ToServiceModel() {
            return new PublishedItemListRequestModel {
                ContinuationToken = ContinuationToken
            };
        }

        /// <summary>
        /// Continuation token or null to start
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationToken { get; set; }
    }
}
