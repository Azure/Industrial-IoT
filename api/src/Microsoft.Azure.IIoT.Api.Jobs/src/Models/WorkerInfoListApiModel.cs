// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;

    /// <summary>
    /// Worker info list model
    /// </summary>
    public class WorkerInfoListApiModel {

        /// <summary>
        /// Continuation token
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Workers
        /// </summary>
        [JsonProperty(PropertyName = "workers")]
        public List<WorkerInfoApiModel> Workers { get; set; }
    }
}