// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Job info query model
    /// </summary>
    public class JobInfoQueryApiModel {

        /// <summary>
        /// Name
        /// </summary>
        [JsonProperty(PropertyName = "name",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// Configuration type
        /// </summary>
        [JsonProperty(PropertyName = "jJobConfigurationType",
           NullValueHandling = NullValueHandling.Ignore)]
        public string JobConfigurationType { get; set; }

        /// <summary>
        /// Job status
        /// </summary>
        [JsonProperty(PropertyName = "status",
            NullValueHandling = NullValueHandling.Ignore)]
        public JobStatus? Status { get; set; }
    }
}