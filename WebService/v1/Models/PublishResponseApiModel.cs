// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// publish response model for webservice api
    /// </summary>
    public class PublishResponseApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishResponseApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishResponseApiModel(PublishResultModel model) {
            Diagnostics = model.Diagnostics;
        }

        /// <summary>
        /// Optional error diagnostics
        /// </summary>
        [JsonProperty(PropertyName = "diagnostics",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Diagnostics { get; set; }
    }
}
