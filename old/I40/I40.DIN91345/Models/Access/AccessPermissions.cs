// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models.Access {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Access permissions
    /// </summary>
    public class AccessPermissions {

        /// <summary>
        /// Access permission tables of the AAS describing the
        /// rights assigned to roles to access elements of the AAS.
        /// </summary>
        [JsonProperty(PropertyName = "accessPermissionTable",
            NullValueHandling = NullValueHandling.Ignore)]
        public AccessPermissionTable AccessPermissionTable { get; set; }

        /// <summary>
        /// Reference to a submodel defining the roles that are
        /// configured for the AAS. They are selectable by the
        /// access permission tables to assign permissions to the
        /// roles.
        /// Default: reference to the submodel referenced via
        /// ref_defaultRoles.  Constraint AAS-020: If there is no
        /// submodel with selectable roles then the default roles
        /// are taken as selectable roles.
        /// Resolves to <see cref="Submodel"/>
        /// </summary>
        [JsonProperty(PropertyName = "ref_selectableRoles",
            NullValueHandling = NullValueHandling.Ignore)]
        public Reference SelectableRoles { get; set; }

        /// <summary>
        /// Reference to a submodel defining the default roles
        /// for the AAS.
        /// Resolves to <see cref="Submodel"/>
        /// </summary>
        [JsonProperty(PropertyName = "ref_defaultRoles")]
        [Required]
        public Reference DefaultRoles { get; set; }

        /// <summary>
        /// Reference to a submodel defining which permissions
        /// can be assigned to the roles or users.
        /// Default: reference to the submodel referenced via
        /// ref_defaultPermissions
        /// Constraint AAS-020: If there is no submodel with
        /// selectable permissions then the default permissions
        /// are taken as selectable permissions.
        /// Resolves to <see cref="Submodel"/>
        /// </summary>
        [JsonProperty(PropertyName = "ref_selectablePermissions",
            NullValueHandling = NullValueHandling.Ignore)]
        public Reference SelectablePermissions { get; set; }

        /// <summary>
        /// Default permissions
        /// Resolve to <see cref="Submodel"/>
        /// </summary>
        [JsonProperty(PropertyName = "ref_defaultPermissions")]
        [Required]
        public Reference DefaultPermissions { get; set; }
    }
}