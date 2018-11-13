// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Service result
    /// </summary>
    public class ServiceResultApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ServiceResultApiModel() { }

        /// <summary>
        /// Create node api model from service model
        /// </summary>
        /// <param name="model"></param>
        public ServiceResultApiModel(ServiceResultModel model) {
            Diagnostics = model.Diagnostics;
            ErrorMessage = model.ErrorMessage;
            StatusCode = model.StatusCode;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public ServiceResultModel ToServiceModel() {
            return new ServiceResultModel {
                Diagnostics = Diagnostics,
                StatusCode = StatusCode,
                ErrorMessage = ErrorMessage
            };
        }

        /// <summary>
        /// Error code - if null operation succeeded.
        /// </summary>
        public uint? StatusCode { get; set; }

        /// <summary>
        /// Error message in case of error or null.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Additional diagnostics information
        /// </summary>
        public JToken Diagnostics{ get; set; }
    }
}
