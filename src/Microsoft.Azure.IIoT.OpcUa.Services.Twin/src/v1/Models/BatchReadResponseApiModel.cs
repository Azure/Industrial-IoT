// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Result of attribute reads
    /// </summary>
    public class BatchReadResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public BatchReadResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public BatchReadResponseApiModel(BatchReadResultModel model) {
            Results = model.Results?
                .Select(a => new AttributeReadResponseApiModel(a)).ToList();
        }

        /// <summary>
        /// All results of attribute reads
        /// </summary>
        [JsonProperty(PropertyName = "results")]
        public List<AttributeReadResponseApiModel> Results { set; get; }
    }
}
