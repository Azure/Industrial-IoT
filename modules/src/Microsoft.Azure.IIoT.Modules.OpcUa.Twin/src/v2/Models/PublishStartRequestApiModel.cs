// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Publish request
    /// </summary>
    public class PublishStartRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishStartRequestApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishStartRequestApiModel(PublishStartRequestModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Item = model.Item == null ? null :
                new PublishedItemApiModel(model.Item);
            Header = model.Header == null ? null :
                new RequestHeaderApiModel(model.Header);
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishStartRequestModel ToServiceModel() {
            return new PublishStartRequestModel {
                Item = Item?.ToServiceModel(),
                Header = Header?.ToServiceModel()
            };
        }

        /// <summary>
        /// Item to publish
        /// </summary>
        [JsonProperty(PropertyName = "Item")]
        public PublishedItemApiModel Item { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "Header",
            NullValueHandling = NullValueHandling.Ignore)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
