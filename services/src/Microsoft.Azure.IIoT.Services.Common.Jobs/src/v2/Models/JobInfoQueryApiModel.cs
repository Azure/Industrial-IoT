// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.v2.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Job info query model
    /// </summary>
    public class JobInfoQueryApiModel {


        /// <summary>
        /// Default
        /// </summary>
        public JobInfoQueryApiModel() {
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public JobInfoQueryApiModel(JobInfoQueryModel model) {
            Name = model.Name;
            JobConfigurationType = model.JobConfigurationType;
            Status = model.Status;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        internal JobInfoQueryModel ToServiceModel() {
            return new JobInfoQueryModel {
                Name = Name,
                JobConfigurationType = JobConfigurationType,
                Status = Status
            };
        }

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