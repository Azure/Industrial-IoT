// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models.LifeCycle {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Lifecycle entry
    /// </summary>
    public class LifeCycleEntry : BaseTimeStamped {

        /// <summary>
        /// Event class
        /// </summary>
        [JsonProperty(PropertyName = "eventClass",
            NullValueHandling = NullValueHandling.Ignore)]
        public string EventClass { get; set; }

        /// <summary>
        /// Creator
        /// </summary>
        [JsonProperty(PropertyName = "id_creatingInstance",
            NullValueHandling = NullValueHandling.Ignore)]
        public Reference CreatingInstance { get; set; }

        /// <summary>
        /// Writer
        /// </summary>
        [JsonProperty(PropertyName = "id_writingInstance",
            NullValueHandling = NullValueHandling.Ignore)]
        public Reference WritingInstance { get; set; }

        /// <summary>
        /// Subject
        /// </summary>
        [JsonProperty(PropertyName = "subject",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Subject { get; set; }

        /// <summary>
        /// Data
        /// </summary>
        [JsonProperty(PropertyName = "lifeCycleData",
            NullValueHandling = NullValueHandling.Ignore,
            ItemIsReference = false)]
        public List<ILifeCycleData> LifeCycleData { get; set; }
    }
}