// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Entity role type
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EntityRoleType {

        /// <summary>
        /// Client role
        /// </summary>
        Client,

        /// <summary>
        /// Server role
        /// </summary>
        Server
    }
}
