// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Trust group root registration model
    /// </summary>
    public sealed class TrustGroupRootCreateRequestApiModel {

        /// <summary>
        /// The new name of the trust group root
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// The trust group type.
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public TrustGroupType Type { get; set; }

        /// <summary>
        /// The subject name of the group as distinguished name.
        /// </summary>
        [JsonProperty(PropertyName = "subjectName")]
        public string SubjectName { get; set; }

        /// <summary>
        /// The lifetime of the trust group root certificate.
        /// </summary>
        [JsonProperty(PropertyName = "lifetime")]
        public TimeSpan Lifetime { get; set; }

        /// <summary>
        /// The certificate key size in bits.
        /// </summary>
        [JsonProperty(PropertyName = "keySize")]
        public ushort? KeySize { get; set; }

        /// <summary>
        /// The certificate signature algorithm.
        /// </summary>
        [JsonProperty(PropertyName = "signatureAlgorithm")]
        public SignatureAlgorithm? SignatureAlgorithm { get; set; }

        /// <summary>
        /// The issued certificate lifetime.
        /// </summary>
        [JsonProperty(PropertyName = "issuedLifetime")]
        public TimeSpan? IssuedLifetime { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        [JsonProperty(PropertyName = "issuedKeySize")]
        public ushort? IssuedKeySize { get; set; }

        /// <summary>
        /// The issued certificate signature algorithm.
        /// </summary>
        [JsonProperty(PropertyName = "issuedSignatureAlgorithm")]
        public SignatureAlgorithm? IssuedSignatureAlgorithm { get; set; }
    }
}
