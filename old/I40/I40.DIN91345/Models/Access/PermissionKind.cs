// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models.Access {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Enumeration of the kind of permissions that is given to
    /// the assignment of a permission to a role.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PermissionKind {

        /// <summary>
        /// Allow the permission given to the role.
        /// </summary>
        Allow,

        /// <summary>
        /// Explicitly deny the permission given to the role.
        /// </summary>
        Deny,

        /// <summary>
        /// The permission is not applicable to the role.
        /// </summary>
        NotApplicable,

        /// <summary>
        /// It is undefined whether the permission is allowed,
        /// not applicable or denied to the role.
        /// </summary>
        Undefined
    }
}