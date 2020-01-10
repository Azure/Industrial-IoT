// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Role permission model
    /// </summary>
    public class RolePermissionApiModel {

        /// <summary>
        /// Identifier of the role object.
        /// </summary>
        [JsonProperty(PropertyName = "roleId")]
        public string RoleId { get; set; }

        /// <summary>
        /// Permissions assigned for the role.
        /// </summary>
        [JsonProperty(PropertyName = "permissions",
            NullValueHandling = NullValueHandling.Ignore)]
        public RolePermissions? Permissions { get; set; }
    }
}
