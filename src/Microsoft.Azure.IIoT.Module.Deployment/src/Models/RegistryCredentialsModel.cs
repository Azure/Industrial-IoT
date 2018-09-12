// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Registry credentials
    /// </summary>
    public class RegistryCredentialsModel {

        /// <summary>
        /// Registry address
        /// </summary>
        [JsonProperty(PropertyName = "address",
            Required = Required.Always)]
        public string Address { get; set; }

        /// <summary>
        /// User name
        /// </summary>
        [JsonProperty(PropertyName = "username",
            NullValueHandling = NullValueHandling.Ignore)]
        public string UserName { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        [JsonProperty(PropertyName = "password",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Password { get; set; }
    }
}
