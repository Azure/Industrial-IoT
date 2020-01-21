// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// An expression element in the filter ast
    /// </summary>
    public class ContentFilterElementApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ContentFilterElementApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public ContentFilterElementApiModel(ContentFilterElementModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            FilterOperands = model.FilterOperands?
                .Select(f => new FilterOperandApiModel(f))
                .ToList();
            FilterOperator = model.FilterOperator;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ContentFilterElementModel ToServiceModel() {
            return new ContentFilterElementModel {
                FilterOperands = FilterOperands?
                    .Select(f => f.ToServiceModel())
                    .ToList(),
                FilterOperator = FilterOperator
            };
        }

        /// <summary>
        /// The operator to use on the operands
        /// </summary>
        [JsonProperty(PropertyName = "filterOperator",
            NullValueHandling = NullValueHandling.Ignore)]
        public FilterOperatorType FilterOperator { get; set; }

        /// <summary>
        /// The operands in the element for the operator
        /// </summary>
        [JsonProperty(PropertyName = "filterOperands",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<FilterOperandApiModel> FilterOperands { get; set; }
    }
}