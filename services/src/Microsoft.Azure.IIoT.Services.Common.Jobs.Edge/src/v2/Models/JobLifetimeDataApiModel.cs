// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Lifetime data
    /// </summary>
    public class JobLifetimeDataApiModel {

        /// <summary>
        /// Default
        /// </summary>
        public JobLifetimeDataApiModel() {
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public JobLifetimeDataApiModel(JobLifetimeDataModel model) {
            Status = model.Status;
            Updated = model.Updated;
            Created = model.Created;
            ProcessingStatus = model.ProcessingStatus?
                .ToDictionary(k => k.Key, v => new ProcessingStatusApiModel(v.Value));
        }

        /// <summary>
        /// Status
        /// </summary>

        public JobStatus Status { get; set; }

        /// <summary>
        /// Processing status
        /// </summary>
        public Dictionary<string, ProcessingStatusApiModel> ProcessingStatus { get; set; }

        /// <summary>
        /// Updated at
        /// </summary>
        public DateTime Updated { get; set; }

        /// <summary>
        /// Created at
        /// </summary>
        public DateTime Created { get; set; }
    }
}