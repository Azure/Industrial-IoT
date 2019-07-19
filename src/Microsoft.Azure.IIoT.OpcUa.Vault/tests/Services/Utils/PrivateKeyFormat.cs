// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Private key format
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PrivateKeyFormat {

        /// <summary>
        /// Pem format
        /// </summary>
        PEM,

        /// <summary>
        /// Pfx
        /// </summary>
        PFX,
    }
}
