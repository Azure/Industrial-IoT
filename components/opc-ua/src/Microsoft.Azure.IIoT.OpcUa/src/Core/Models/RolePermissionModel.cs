// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {

    /// <summary>
    /// Role permission model
    /// </summary>
    public class RolePermissionModel {

        /// <summary>
        /// Identifier of the role object.
        /// </summary>
        public string RoleId { get; set; }

        /// <summary>
        /// Permissions assigned for the role.
        /// </summary>
        public RolePermissions? Permissions { get; set; }
    }
}
