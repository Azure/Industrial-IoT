// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Shared.External.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Result of an endpoint registration
    /// </summary>
    public class ServerRegistrationResponseApiModel {
        /// <summary>
        /// New id endpoint was registered under
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
