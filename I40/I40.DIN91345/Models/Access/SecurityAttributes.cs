// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models.Access {
    using I40.DIN91345.Models.Users;
    using I40.Common.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Security Attributes
    /// </summary>
    public class SecurityAttributes {

        /// <summary>
        /// Access Roles and Rights defined for the AAS and
        /// elements within the AAS.
        /// </summary>
        [JsonProperty(PropertyName = "permissionManager",
            NullValueHandling = NullValueHandling.Ignore)]
        public AccessPermissions PermissionManager { get; set; }

        /// <summary>
        /// AliasManagement for the AAS.
        /// </summary>
        [JsonProperty(PropertyName = "alias",
            NullValueHandling = NullValueHandling.Ignore)]
        public AliasManagement AliasManagement { get; set; }

        /// <summary>
        /// Authentification Handling for the AAS e.g. via
        /// certifiactes
        /// </summary>
        [JsonProperty(PropertyName = "authentificator",
            NullValueHandling = NullValueHandling.Ignore)]
        public Authentifications CredentialManagement { get; set; }

        /// <summary>
        /// Logging changes on propoerties
        /// </summary>
        [JsonProperty(PropertyName = "history",
            NullValueHandling = NullValueHandling.Ignore)]
        public History History { get; set; }

        /// <summary>
        /// User management. Mapping of users to roles.
        /// </summary>
        [JsonProperty(PropertyName = "userManager",
            NullValueHandling = NullValueHandling.Ignore)]
        public Users UserManagement { get; set; }
    }
}