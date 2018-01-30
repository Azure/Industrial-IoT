// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Method metadata request model for webservice api
    /// </summary>
    public class MethodCallResponseApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public MethodCallResponseApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public MethodCallResponseApiModel(MethodCallResultModel model) {
            Results = model.Results;
            Diagnostics = model.Diagnostics;
        }

        /// <summary>
        /// Output results
        /// </summary>
        [JsonProperty(PropertyName = "results")]
        public List<string> Results { get; set; }

        /// <summary>
        /// Optional error diagnostics
        /// </summary>
        [JsonProperty(PropertyName = "diagnostics",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Diagnostics { get; set; }
    }
}
