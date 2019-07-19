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
    /// Trust group registration request model
    /// </summary>
    public sealed class TrustGroupRegistrationRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public TrustGroupRegistrationRequestApiModel() {
        }

        /// <summary>
        /// Create trust group registration model
        /// </summary>
        /// <param name="model"></param>
        public TrustGroupRegistrationRequestApiModel(TrustGroupRegistrationRequestModel model) {
            Name = model.Name;
            ParentId = model.ParentId;
            SubjectName = model.SubjectName;
            IssuedKeySize = model.IssuedKeySize;
            IssuedLifetime = model.IssuedLifetime;
            IssuedSignatureAlgorithm = model.IssuedSignatureAlgorithm;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public TrustGroupRegistrationRequestModel ToServiceModel() {
            return new TrustGroupRegistrationRequestModel {
                Name = Name,
                ParentId = ParentId,
                SubjectName = SubjectName,
                IssuedKeySize = IssuedKeySize,
                IssuedLifetime = IssuedLifetime,
                IssuedSignatureAlgorithm = IssuedSignatureAlgorithm,
            };
        }

        /// <summary>
        /// The new name of the trust group
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The identifer of the parent trust group.
        /// </summary>
        [JsonProperty(PropertyName = "parentId")]
        [Required]
        public string ParentId { get; set; }

        /// <summary>
        /// The subject name of the group as distinguished name.
        /// </summary>
        [JsonProperty(PropertyName = "subjectName")]
        [Required]
        public string SubjectName { get; set; }

        /// <summary>
        /// The lifetime of certificates issued in the group.
        /// </summary>
        [JsonProperty(PropertyName = "issuedLifetime")]
        [DefaultValue(null)]
        public TimeSpan? IssuedLifetime { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        [JsonProperty(PropertyName = "issuedKeySize")]
        [DefaultValue(null)]
        public ushort? IssuedKeySize { get; set; }

        /// <summary>
        /// The issued certificate signature algorithm.
        /// </summary>
        [JsonProperty(PropertyName = "issuedSignatureAlgorithm")]
        [DefaultValue(null)]
        public SignatureAlgorithm? IssuedSignatureAlgorithm { get; set; }
    }
}
