// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.ComponentModel;

    /// <summary>
    /// Credential model
    /// </summary>
    public class CredentialApiModel {

        /// <summary>
        /// Type of credential
        /// </summary>
        [JsonProperty(PropertyName = "type",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(CredentialType.None)]
        public CredentialType? Type { get; set; }

        /// <summary>
        /// Value to pass to server
        /// </summary>
        [JsonProperty(PropertyName = "value",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public JToken Value { get; set; }
    }
}
