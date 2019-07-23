// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// The certificate request type.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CertificateRequestType {

        /// <summary>
        /// Signing request
        /// </summary>
        SigningRequest,

        /// <summary>
        /// Key pair request
        /// </summary>
        KeyPairRequest,
    }
}

