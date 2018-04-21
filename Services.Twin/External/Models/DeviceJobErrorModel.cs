// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.External.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Potential error during job execution on device
    /// </summary>
    public class DeviceJobErrorModel
    {
        /// <summary>
        /// Error code
        /// </summary>
        [JsonProperty("Code")]
        public string Code { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; set; }
    }
}
