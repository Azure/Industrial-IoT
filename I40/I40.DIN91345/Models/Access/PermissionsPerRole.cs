// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models.Access {
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Role permissions
    /// </summary>
    public class PermissionsPerRole {

        /// <summary>
        /// Role to which permission shall be assigned.
        /// The permissions hold for all elements as
        /// specified in the access permission table.
        /// </summary>
        [JsonProperty(PropertyName = "role")]
        [Required]
        public Role Role { get; set; }

        /// <summary>
        /// [0..*] Permissions assigned to the role.
        /// The permissions hold for all elements as
        /// specified in the access permission table.
        /// </summary>
        [JsonProperty(PropertyName = "permission",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<Permission> Permissions { get; set; }
    }
}