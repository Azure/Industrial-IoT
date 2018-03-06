// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Result of a twin registration
    /// </summary>
    public class TwinRegistrationResponseApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public TwinRegistrationResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public TwinRegistrationResponseApiModel(TwinRegistrationResultModel model) {
            Id = model.Id;
        }

        /// <summary>
        /// New id twin was registered under
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
