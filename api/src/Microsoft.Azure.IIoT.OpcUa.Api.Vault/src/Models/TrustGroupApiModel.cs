// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;

    /// <summary>
    /// Signature algorithm
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SignatureAlgorithm {

        /// <summary>
        /// Rsa 256
        /// </summary>
        Rsa256,

        /// <summary>
        /// Rsa 384
        /// </summary>
        Rsa384,

        /// <summary>
        /// Rsa 512
        /// </summary>
        Rsa512,

        /// <summary>
        /// 256 with padding
        /// </summary>
        Rsa256Pss,

        /// <summary>
        /// 384 with padding
        /// </summary>
        Rsa384Pss,

        /// <summary>
        /// 512 with padding
        /// </summary>
        Rsa512Pss,
    }

    /// <summary>
    /// Trust group types
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TrustGroupType {

        /// <summary>
        /// Application certificate
        /// </summary>
        ApplicationInstanceCertificate,

        /// <summary>
        /// Https certificate type
        /// </summary>
        HttpsCertificate,

        /// <summary>
        /// User credential certificate type
        /// </summary>
        UserCredentialCertificate
    }

    /// <summary>
    /// Trust group model
    /// </summary>
    public sealed class TrustGroupApiModel {

        /// <summary>
        /// The name of the trust group.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// The identifer of the parent trust group.
        /// </summary>
        [JsonProperty(PropertyName = "parentId")]
        public string ParentId { get; set; }

        /// <summary>
        /// The trust group type
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public TrustGroupType Type { get; set; }

        /// <summary>
        /// The subject name of the group as distinguished name.
        /// </summary>
        [JsonProperty(PropertyName = "subjectName")]
        public string SubjectName { get; set; }

        /// <summary>
        /// The lifetime of the trust group certificate.
        /// </summary>
        [JsonProperty(PropertyName = "lifetime")]
        public TimeSpan Lifetime { get; set; }

        /// <summary>
        /// The trust group certificate key size in bits.
        /// </summary>
        [JsonProperty(PropertyName = "keySize")]
        public ushort KeySize { get; set; }

        /// <summary>
        /// The certificate signature algorithm.
        /// </summary>
        [JsonProperty(PropertyName = "signatureAlgorithm")]
        public SignatureAlgorithm SignatureAlgorithm { get; set; }

        /// <summary>
        /// The issued certificate lifetime in months.
        /// </summary>
        [JsonProperty(PropertyName = "issuedLifetime")]
        public TimeSpan IssuedLifetime { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        [JsonProperty(PropertyName = "issuedKeySize")]
        public ushort IssuedKeySize { get; set; }

        /// <summary>
        /// The Signature algorithm for issued certificates
        /// </summary>
        [JsonProperty(PropertyName = "issuedSignatureAlgorithm")]
        public SignatureAlgorithm IssuedSignatureAlgorithm { get; set; }
    }
}
