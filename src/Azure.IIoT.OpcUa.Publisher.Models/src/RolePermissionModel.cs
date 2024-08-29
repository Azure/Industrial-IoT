// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Role permission model
    /// </summary>
    [DataContract]
    public sealed record class RolePermissionModel
    {
        /// <summary>
        /// Identifier of the role object.
        /// </summary>
        [DataMember(Name = "roleId", Order = 0)]
        [Required]
        public required string RoleId { get; set; }

        /// <summary>
        /// Permissions assigned for the role.
        /// </summary>
        [DataMember(Name = "permissions", Order = 1,
            EmitDefaultValue = false)]
        public RolePermissions? Permissions { get; set; }
    }
}
