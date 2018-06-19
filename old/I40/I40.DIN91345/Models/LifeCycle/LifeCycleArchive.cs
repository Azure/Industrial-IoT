// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models.LifeCycle {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Archive of entries
    /// </summary>
    public class LifeCycleArchive : History {

        /// <summary>
        /// Lifecycle entries
        /// </summary>
        [JsonProperty(PropertyName = "lifeCycleEntries",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<LifeCycleEntry> LifeCycleEntries { get; set; }
    }
}