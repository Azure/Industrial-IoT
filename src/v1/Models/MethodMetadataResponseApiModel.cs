// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// method metadata query model
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
            ObjectId = model.ObjectId;
            if (model.InputArguments == null) {
                InputArguments = new List<MethodMetadataArgumentApiModel>();
            }
            else {
                InputArguments = model.InputArguments
                    .Select(a => new MethodMetadataArgumentApiModel(a))
                    .ToList();
            }
            if (model.OutputArguments == null) {
                OutputArguments = new List<MethodMetadataArgumentApiModel>();
            }
            else {
                OutputArguments = model.OutputArguments
                    .Select(a => new MethodMetadataArgumentApiModel(a))
                    .ToList();
            }
        }

        /// <summary>
        /// Id of object that the method is a component of
        /// </summary>
        [JsonProperty(PropertyName = "objectId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ObjectId { get; set; }

        /// <summary>
        /// Input argument meta data
        /// </summary>
        [JsonProperty(PropertyName = "inputArguments",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<MethodMetadataArgumentApiModel> InputArguments { get; set; }

        /// <summary>
        /// output argument meta data
        /// </summary>
        [JsonProperty(PropertyName = "outputArguments",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<MethodMetadataArgumentApiModel> OutputArguments { get; set; }

        /// <summary>
        /// Optional error diagnostics
        /// </summary>
        [JsonProperty(PropertyName = "diagnostics",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Diagnostics { get; set; }
    }
}
