// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models.Access {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Access permission table
    /// </summary>
    public class AccessPermissionTable : BaseIdentifiable {

        /// <summary>
        /// [0..*] Set of role-permission pairs that define the
        /// permissions per role for the elements defined via
        /// ref_permissionsFor
        /// </summary>
        [JsonProperty(PropertyName = "permissionsPerRole",
            NullValueHandling = NullValueHandling.Ignore)]
        public PermissionsPerRole PermissionsPerRole { get; set; }

        /// <summary>
        /// [0..*] References to the referenceable elements that
        /// have the permissions defined in this table.
        /// </summary>
        [JsonProperty(PropertyName = "ref_permissionsFor",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<Reference> PermissionsFor { get; set; }
    }
}