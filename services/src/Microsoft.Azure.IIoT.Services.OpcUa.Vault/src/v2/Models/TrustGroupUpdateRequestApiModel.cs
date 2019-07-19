// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Trust group update model
    /// </summary>
    public sealed class TrustGroupUpdateRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public TrustGroupUpdateRequestApiModel() {
        }

        /// <summary>
        /// Create trust group update model
        /// </summary>
        /// <param name="model"></param>
        public TrustGroupUpdateRequestApiModel(TrustGroupRegistrationUpdateModel model) {
            Name = model.Name;
            IssuedKeySize = model.IssuedKeySize;
            IssuedLifetime = model.IssuedLifetime;
            IssuedSignatureAlgorithm = model.IssuedSignatureAlgorithm;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public TrustGroupRegistrationUpdateModel ToServiceModel() {
            return new TrustGroupRegistrationUpdateModel {
                Name = Name,
                IssuedKeySize = IssuedKeySize,
                IssuedLifetime = IssuedLifetime,
                IssuedSignatureAlgorithm = IssuedSignatureAlgorithm,
            };
        }

        /// <summary>
        /// The name of the trust group
        /// </summary>
        [JsonProperty(PropertyName = "name",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string Name { get; set; }

        /// <summary>
        /// The issued certificate lifetime.
        /// </summary>
        [JsonProperty(PropertyName = "issuedLifetime",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public TimeSpan? IssuedLifetime { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        [JsonProperty(PropertyName = "issuedKeySize",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public ushort? IssuedKeySize { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        [JsonProperty(PropertyName = "issuedSignatureAlgorithm",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public SignatureAlgorithm? IssuedSignatureAlgorithm { get; set; }
    }
}
