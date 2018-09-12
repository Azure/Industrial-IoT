// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Environment variable as part of module model.
    /// </summary>
    public class EnvironmentVariableModel {

        /// <summary>
        /// Value of variable
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
