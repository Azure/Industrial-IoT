// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Result of an application registration
    /// </summary>
    public class ApplicationRegistrationResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationRegistrationResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ApplicationRegistrationResponseApiModel(
            ApplicationRegistrationResultModel model) {
            Id = model.Id;
        }

        /// <summary>
        /// New id application was registered under
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
