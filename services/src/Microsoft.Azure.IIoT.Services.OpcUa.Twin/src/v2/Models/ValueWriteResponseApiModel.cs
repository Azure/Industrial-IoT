// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Value write response model
    /// </summary>
    public class ValueWriteResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ValueWriteResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ValueWriteResponseApiModel(ValueWriteResultModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            ErrorInfo = model.ErrorInfo == null ? null :
                new ServiceResultApiModel(model.ErrorInfo);
        }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [JsonProperty(PropertyName = "errorInfo",
            NullValueHandling = NullValueHandling.Ignore)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
