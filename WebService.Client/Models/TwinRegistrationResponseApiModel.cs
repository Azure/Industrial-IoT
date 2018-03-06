// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Result of an twin registration
    /// </summary>
    public class TwinRegistrationResponseApiModel {

        /// <summary>
        /// New id twin was registered under
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
