// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Trust group model
    /// </summary>
    public sealed class TrustGroupApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public TrustGroupApiModel() {
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public TrustGroupApiModel(TrustGroupModel model) {
            Name = model.Name;
            ParentId = model.ParentId;
            Type = model.Type;
            SubjectName = model.SubjectName;
            IssuedKeySize = model.IssuedKeySize;
            IssuedLifetime = model.IssuedLifetime;
            IssuedSignatureAlgorithm = model.IssuedSignatureAlgorithm;
            KeySize = model.KeySize;
            Lifetime = model.Lifetime;
            SignatureAlgorithm = model.SignatureAlgorithm;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public TrustGroupModel ToServiceModel() {
            return new TrustGroupModel {
                Name = Name,
                ParentId = ParentId,
                Type = Type,
                SubjectName = SubjectName,
                IssuedKeySize = IssuedKeySize,
                IssuedLifetime = IssuedLifetime,
                IssuedSignatureAlgorithm = IssuedSignatureAlgorithm,
                KeySize = KeySize,
                Lifetime = Lifetime,
                SignatureAlgorithm = SignatureAlgorithm
            };
        }

        /// <summary>
        /// The name of the trust group.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The identifer of the parent trust group.
        /// </summary>
        [JsonProperty(PropertyName = "parentId")]
        [DefaultValue(null)]
        public string ParentId { get; set; }

        /// <summary>
        /// The trust group type
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        [DefaultValue(TrustGroupType.ApplicationInstanceCertificate)]
        public TrustGroupType Type { get; set; }

        /// <summary>
        /// The subject name of the group as distinguished name.
        /// </summary>
        [JsonProperty(PropertyName = "subjectName")]
        [Required]
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
