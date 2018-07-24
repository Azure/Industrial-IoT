// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Deployment.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Environment variable as part of module model.
    /// </summary>
    public class EnvironmentVariableModel {

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
