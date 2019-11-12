// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.v2.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// Job info model
    /// </summary>
    public class JobInfoListApiModel {

        /// <summary>
        /// Default
        /// </summary>
        public JobInfoListApiModel() {
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public JobInfoListApiModel(JobInfoListModel model) {
            Jobs = model.Jobs?
                .Select(d => new JobInfoApiModel(d)).ToList();
            ContinuationToken = model.ContinuationToken;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        internal JobInfoListModel ToServiceModel() {
            return new JobInfoListModel {
                Jobs = Jobs?.Select(d => d.ToServiceModel()).ToList(),
                ContinuationToken = ContinuationToken
            };
        }

        /// <summary>
        /// Continuation token
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Jobs
        /// </summary>
        [JsonProperty(PropertyName = "jobs")]
        public List<JobInfoApiModel> Jobs { get; set; }
    }
}