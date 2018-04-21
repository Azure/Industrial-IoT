// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.External.Models {
    using Newtonsoft.Json;

    public class DeviceAuthenticationModel {

        /// <summary>
        /// Primary sas key
        /// </summary>
        [JsonProperty(PropertyName = "PrimaryKey", 
            NullValueHandling = NullValueHandling.Ignore)]
        public string PrimaryKey { get; set; }

        /// <summary>
        /// Secondary sas key
        /// </summary>
        [JsonProperty(PropertyName = "SecondaryKey", 
            NullValueHandling = NullValueHandling.Ignore)]
        public string SecondaryKey { get; set; }
    }
}
