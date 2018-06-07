// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Module.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Method metadata request model for edge service api
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
        public List<string> Results { get; set; }

        /// <summary>
        /// Optional error diagnostics
        /// </summary>
        public JToken Diagnostics { get; set; }
    }
}
