// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Role permission model
    /// </summary>
    public class RolePermissionApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public RolePermissionApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public RolePermissionApiModel(RolePermissionModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            RoleId = model.RoleId;
            Permissions = model.Permissions;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public RolePermissionModel ToServiceModel() {
            return new RolePermissionModel {
                RoleId = RoleId,
                Permissions = Permissions
            };
        }

        /// <summary>
        /// Identifier of the role object.
        /// </summary>
        [JsonProperty(PropertyName = "RoleId")]
        public string RoleId { get; set; }

        /// <summary>
        /// Permissions assigned for the role.
        /// </summary>
        [JsonProperty(PropertyName = "Permissions",
            NullValueHandling = NullValueHandling.Ignore)]
        public RolePermissions? Permissions { get; set; }
    }
}
