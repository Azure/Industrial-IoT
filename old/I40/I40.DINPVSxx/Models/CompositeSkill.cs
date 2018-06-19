// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// A composite skill is a skill that is depending on the
    /// availability of other skills.
    /// </summary>
    public class CompositeSkill : Skill {

        /// <summary>
        /// Skills the skill is depending on.
        /// </summary>
        [JsonProperty(PropertyName = "skill",
            NullValueHandling = NullValueHandling.Ignore,
            ItemIsReference = false)]
        public List<Skill> Skills { get; set; }

        // TODO: UML defines base skill, which amounts to the same as Skill, should be fixed.
    }
}