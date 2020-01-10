// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Event filter
    /// </summary>
    public class EventFilterApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public EventFilterApiModel() {
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public EventFilterApiModel(EventFilterModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            SelectClauses = model.SelectClauses?
                .Select(f => new SimpleAttributeOperandApiModel(f))
                .ToList();
            WhereClause = model.WhereClause == null ? null :
                new ContentFilterApiModel(model.WhereClause);
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public EventFilterModel ToServiceModel() {
            return new EventFilterModel {
                SelectClauses = SelectClauses?
                    .Select(e => e.ToServiceModel())
                    .ToList(),
                WhereClause = WhereClause?.ToServiceModel()
            };
        }

        /// <summary>
        /// Select statements
        /// </summary>
        [JsonProperty(PropertyName = "selectClauses")]
        public List<SimpleAttributeOperandApiModel> SelectClauses { get; set; }

        /// <summary>
        /// Where clause
        /// </summary>
        [JsonProperty(PropertyName = "whereClause")]
        public ContentFilterApiModel WhereClause { get; set; }
    }
}