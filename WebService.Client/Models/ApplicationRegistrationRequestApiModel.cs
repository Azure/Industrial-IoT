// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Application registration request
    /// </summary>
    public class ApplicationRegistrationRequestApiModel {

        /// <summary>
        /// Discovery url to use for registration
        /// </summary>
        [JsonProperty(PropertyName = "discoveryUrl")]
        public string DiscoveryUrl { get; set; }
    }
}
