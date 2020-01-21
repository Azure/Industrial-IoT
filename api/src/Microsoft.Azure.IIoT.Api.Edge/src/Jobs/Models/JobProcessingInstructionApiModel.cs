// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Processing instructions
    /// </summary>
    public class JobProcessingInstructionApiModel {

        /// <summary>
        /// Processing mode
        /// </summary>
        [JsonProperty(PropertyName = "processMode",
            NullValueHandling = NullValueHandling.Ignore)]
        public ProcessMode? ProcessMode { get; set; }

        /// <summary>
        /// Job to process
        /// </summary>
        [JsonProperty(PropertyName = "job",
            NullValueHandling = NullValueHandling.Ignore)]
        public JobInfoApiModel Job { get; set; }
    }
}