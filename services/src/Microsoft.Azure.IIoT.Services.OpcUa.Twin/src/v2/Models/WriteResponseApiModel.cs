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
    /// Result of attribute write
    /// </summary>
    public class WriteResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public WriteResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public WriteResponseApiModel(WriteResultModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Results = model.Results?
                .Select(a => a == null ? null : new AttributeWriteResponseApiModel(a))
                .ToList();
        }

        /// <summary>
        /// All results of attribute writes
        /// </summary>
        [JsonProperty(PropertyName = "results")]
        public List<AttributeWriteResponseApiModel> Results { set; get; }
    }
}
