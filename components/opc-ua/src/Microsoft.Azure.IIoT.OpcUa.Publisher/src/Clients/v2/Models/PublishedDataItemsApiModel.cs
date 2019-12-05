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
    /// Published items
    /// </summary>
    public class PublishedDataItemsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishedDataItemsApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishedDataItemsApiModel(PublishedDataItemsModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            PublishedData = model.PublishedData?
                .Select(d => new PublishedDataSetVariableApiModel(d))
                .ToList();
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishedDataItemsModel ToServiceModel() {
            return new PublishedDataItemsModel {
                PublishedData = PublishedData?
                    .Select(d => d.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Published data variables
        /// </summary>
        [JsonProperty(PropertyName = "publishedData")]
        public List<PublishedDataSetVariableApiModel> PublishedData { get; set; }
    }
}