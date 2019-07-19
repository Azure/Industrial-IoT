// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Trust group registration request model
    /// </summary>
    public sealed class TrustGroupRegistrationRequestApiModel {

        /// <summary>
        /// The new name of the trust group
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// The identifer of the parent trust group.
        /// </summary>
        [JsonProperty(PropertyName = "parentId")]
        public string ParentId { get; set; }

        /// <summary>
        /// The subject name of the group as distinguished name.
        /// </summary>
        [JsonProperty(PropertyName = "subjectName")]
        public string SubjectName { get; set; }

        /// <summary>
        /// The lifetime of certificates issued in the group.
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
