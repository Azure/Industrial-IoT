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
    /// Method call response model
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
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Results = model.Results?
                .Select(a => a == null ? null : new MethodCallArgumentApiModel(a))
                .ToList();
            ErrorInfo = model.ErrorInfo == null ? null :
                new ServiceResultApiModel(model.ErrorInfo);
        }

        /// <summary>
        /// Output results
        /// </summary>
        [JsonProperty(PropertyName = "results")]
        public List<MethodCallArgumentApiModel> Results { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [JsonProperty(PropertyName = "errorInfo",
            NullValueHandling = NullValueHandling.Ignore)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
