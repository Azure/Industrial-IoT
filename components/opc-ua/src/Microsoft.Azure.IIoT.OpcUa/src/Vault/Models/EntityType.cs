// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Entity type
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EntityType {

        /// <summary>
        /// Group
        /// </summary>
        Group,

        /// <summary>
        /// Application
        /// </summary>
        Application,

        /// <summary>
        /// Endpoint
        /// </summary>
        Endpoint,

        /// <summary>
        /// User
        /// </summary>
        User,

        /// <summary>
        /// Twin module
        /// </summary>
        Twin,

        /// <summary>
        /// Publisher module
        /// </summary>
        Publisher,
    }
}
