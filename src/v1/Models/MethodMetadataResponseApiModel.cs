// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// method metadata query model for twin module
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
        public List<MethodArgumentApiModel> InputArguments { get; set; }

        /// <summary>
        /// output argument meta data
        /// </summary>
        public List<MethodArgumentApiModel> OutputArguments { get; set; }

        /// <summary>
        /// Optional error diagnostics
        /// </summary>
        public JToken Diagnostics { get; set; }
    }
}
