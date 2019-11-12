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
    /// Worker info list model
    /// </summary>
    public class WorkerInfoListApiModel {

        /// <summary>
        /// Default
        /// </summary>
        public WorkerInfoListApiModel() {
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public WorkerInfoListApiModel(WorkerInfoListModel model) {
            Workers = model.Workers?
                .Select(d => new WorkerInfoApiModel(d)).ToList();
            ContinuationToken = model.ContinuationToken;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public WorkerInfoListModel ToServiceModel() {
            return new WorkerInfoListModel {
                Workers = Workers?.Select(a => a.ToServiceModel()).ToList(),
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
        /// Agents
        /// </summary>
        [JsonProperty(PropertyName = "workers")]
        public List<WorkerInfoApiModel> Workers { get; set; }
    }
}