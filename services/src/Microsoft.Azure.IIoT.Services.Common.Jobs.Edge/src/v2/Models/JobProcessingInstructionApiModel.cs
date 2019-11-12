// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Processing instructions
    /// </summary>
    public class JobProcessingInstructionApiModel {

        /// <summary>
        /// Default
        /// </summary>
        public JobProcessingInstructionApiModel() {
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public JobProcessingInstructionApiModel(JobProcessingInstructionModel model) {
            ProcessMode = model?.ProcessMode;
            Job = model?.Job == null ? null : new JobInfoApiModel(model.Job);
        }

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