// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IIoT.OpcTwin.Services.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// method metadata query model for webservice api
    /// </summary>
    public class MethodMetadataResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public MethodMetadataResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public MethodMetadataResponseApiModel(MethodMetadataResultModel model) {
            Diagnostics = model.Diagnostics;

            if (model.InputArguments == null) {
                InputArguments = new List<MethodArgumentApiModel>();
            }
            else {
                InputArguments = model.InputArguments
                    .Select(a => new MethodArgumentApiModel(a))
                    .ToList();
            }
            if (model.OutputArguments == null) {
                OutputArguments = new List<MethodArgumentApiModel>();
            }
            else {
                OutputArguments = model.OutputArguments
                    .Select(a => new MethodArgumentApiModel(a))
                    .ToList();
            }
        }

        /// <summary>
        /// Input argument meta data
        /// </summary>
        [JsonProperty(PropertyName = "inputArgs")]
        public List<MethodArgumentApiModel> InputArguments { get; set; }

        /// <summary>
        /// output argument meta data
        /// </summary>
        [JsonProperty(PropertyName = "outputArgs")]
        public List<MethodArgumentApiModel> OutputArguments { get; set; }

        /// <summary>
        /// Optional error diagnostics
        /// </summary>
        [JsonProperty(PropertyName = "diagnostics",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Diagnostics { get; set; }
    }
}
