// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Result of an endpoint registration
    /// </summary>
    public class ServerRegistrationResponseApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerRegistrationResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ServerRegistrationResponseApiModel(ServerRegistrationResultModel model) {
            Id = model.Id;
        }

        /// <summary>
        /// New id endpoint was registered under
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
