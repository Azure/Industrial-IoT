// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Shared.External.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Method metadata request model for webservice api
    /// </summary>
    public class MethodMetadataRequestApiModel {
        /// <summary>
        /// Count of input arguments
        /// </summary>
        [JsonProperty(PropertyName = "methodId")]
        public string MethodId { get; set; }
    }
}
