// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Supervisor registration list
    /// </summary>
    public class SupervisorListApiModel {

        /// <summary>
        /// Registrations
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public List<SupervisorApiModel> Items { get; set; }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationToken { get; set; }
    }
}
