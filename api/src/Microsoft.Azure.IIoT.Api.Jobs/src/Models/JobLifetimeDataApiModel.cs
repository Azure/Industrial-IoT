// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Lifetime data
    /// </summary>
    public class JobLifetimeDataApiModel {

        /// <summary>
        /// Status
        /// </summary>

        [JsonProperty(PropertyName = "status",
            NullValueHandling = NullValueHandling.Ignore)]
        public JobStatus Status { get; set; }

        /// <summary>
        /// Processing status
        /// </summary>
        [JsonProperty(PropertyName = "processingStatus",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, ProcessingStatusApiModel> ProcessingStatus { get; set; }

        /// <summary>
        /// Updated at
        /// </summary>
        [JsonProperty(PropertyName = "updated")]
        public DateTime Updated { get; set; }

        /// <summary>
        /// Created at
        /// </summary>
        [JsonProperty(PropertyName = "created")]
        public DateTime Created { get; set; }
    }
}