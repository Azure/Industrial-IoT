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
    /// Trust group root registration model
    /// </summary>
    public sealed class TrustGroupRootCreateRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public TrustGroupRootCreateRequestApiModel() {
        }

        /// <summary>
        /// Create trust group model
        /// </summary>
        /// <param name="model"></param>
        public TrustGroupRootCreateRequestApiModel(TrustGroupRootCreateRequestModel model) {
            Name = model.Name;
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
        public TrustGroupRootCreateRequestModel ToServiceModel() {
            return new TrustGroupRootCreateRequestModel {
                Name = Name,
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
        /// The new name of the trust group root
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The trust group type.
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
        /// The lifetime of the trust group root certificate.
        /// </summary>
        [JsonProperty(PropertyName = "lifetime")]
        [Required]
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
