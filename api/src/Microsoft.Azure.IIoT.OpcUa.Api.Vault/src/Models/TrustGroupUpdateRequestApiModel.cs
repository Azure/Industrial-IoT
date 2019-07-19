// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Trust group update model
    /// </summary>
    public sealed class TrustGroupUpdateRequestApiModel {

        /// <summary>
        /// The name of the trust group
        /// </summary>
        [JsonProperty(PropertyName = "name",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// The issued certificate lifetime.
        /// </summary>
        [JsonProperty(PropertyName = "issuedLifetime",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? IssuedLifetime { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        [JsonProperty(PropertyName = "issuedKeySize",
            NullValueHandling = NullValueHandling.Ignore)]
        public ushort? IssuedKeySize { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        [JsonProperty(PropertyName = "issuedSignatureAlgorithm",
            NullValueHandling = NullValueHandling.Ignore)]
        public SignatureAlgorithm? IssuedSignatureAlgorithm { get; set; }
    }
}
