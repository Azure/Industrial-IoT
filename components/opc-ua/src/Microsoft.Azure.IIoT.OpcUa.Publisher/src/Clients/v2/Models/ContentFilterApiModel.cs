// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Content filter
    /// </summary>
    public class ContentFilterApiModel {

        /// <inheritdoc/>
        public ContentFilterApiModel() {
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public ContentFilterApiModel(ContentFilterModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Elements = model.Elements?
                .Select(f => new ContentFilterElementApiModel(f))
                .ToList();
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ContentFilterModel ToServiceModel() {
            return new ContentFilterModel {
                Elements = Elements?
                    .Select(e => e.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// The flat list of elements in the filter AST
        /// </summary>
        [JsonProperty(PropertyName = "elements")]
        public List<ContentFilterElementApiModel> Elements { get; set; }
    }
}