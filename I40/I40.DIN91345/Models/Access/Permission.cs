// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models.Access {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Description of a single permission.
    /// </summary>
    public class Permission {

        /// <summary>
        /// Kind of permission
        /// </summary>
        [JsonProperty(PropertyName = "kindOfPermission")]
        [Required]
        public PermissionKind Kind { get; set; }

        /// <summary>
        /// Reference to a property that defines the semantics
        /// of the permission.
        /// Resolves to <see cref="DINPVSxx.Models.Property"/>
        /// Constraint AAS-021: The property is a static data
        /// property.
        /// Constraint AAS-022: The permission property shall be
        /// part of the submodel that is referenced within
        /// <see cref="AccessPermissions.SelectablePermissions"/>.
        /// </summary>
        [JsonProperty(PropertyName = "ref_permission")]
        [Required]
        public Reference Semantics { get; set; }
    }
}