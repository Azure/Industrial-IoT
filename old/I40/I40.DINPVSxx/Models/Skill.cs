// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// A skill describes the potential to achieve an effect
    /// within a domain.
    /// </summary>
    public abstract class Skill : Property {

        /// <summary>
        /// [0..*] input parameters of the skill.
        /// </summary>
        [JsonProperty(PropertyName = "in",
            NullValueHandling = NullValueHandling.Ignore,
            ItemIsReference = false)]
        public List<SkillParameter> In { get; set; }
        // TODO: Marked as ref

        /// <summary>
        /// [0..*] output parameters of the skill.
        /// </summary>
        [JsonProperty(PropertyName = "out",
            NullValueHandling = NullValueHandling.Ignore,
            ItemIsReference = false)]
        public List<SkillParameter> Out { get; set; }
        // TODO: Marked as ref
    }
}