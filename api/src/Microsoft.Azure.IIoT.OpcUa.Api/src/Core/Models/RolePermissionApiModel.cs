// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Role permission model
    /// </summary>
    [DataContract]
    public class RolePermissionApiModel {

        /// <summary>
        /// Identifier of the role object.
        /// </summary>
        [DataMember(Name = "roleId", Order = 0)]
        [Required]
        public string RoleId { get; set; }

        /// <summary>
        /// Permissions assigned for the role.
        /// </summary>
        [DataMember(Name = "permissions", Order = 1,
            EmitDefaultValue = false)]
        public RolePermissions? Permissions { get; set; }
    }
}
