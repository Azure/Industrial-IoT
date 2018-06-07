// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
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
        public ApplicationRegistrationResponseApiModel(ApplicationRegistrationResultModel model) {
            Id = model.Id;
        }

        /// <summary>
        /// New id application was registered under
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
