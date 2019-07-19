// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// List of published nodes
    /// </summary>
    public class PublishedItemListResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishedItemListResponseApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishedItemListResponseApiModel(PublishedItemListResultModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            ContinuationToken = model.ContinuationToken;
            Items = model.Items?
                .Select(n => n == null ? null : new PublishedItemApiModel(n))
                .ToList();
        }

        /// <summary>
        /// Monitored items
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public List<PublishedItemApiModel> Items { get; set; }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationToken { get; set; }
    }
}
