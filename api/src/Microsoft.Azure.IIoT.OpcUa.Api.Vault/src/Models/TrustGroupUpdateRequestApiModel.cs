// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Trust group update model
    /// </summary>
    [DataContract]
    public sealed class TrustGroupUpdateRequestApiModel {

        /// <summary>
        /// The name of the trust group
        /// </summary>
        [DataMember(Name = "name", Order = 0,
            EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// The issued certificate lifetime.
        /// </summary>
        [DataMember(Name = "issuedLifetime", Order = 1,
            EmitDefaultValue = false)]
        public TimeSpan? IssuedLifetime { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        [DataMember(Name = "issuedKeySize", Order = 2,
            EmitDefaultValue = false)]
        public ushort? IssuedKeySize { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        [DataMember(Name = "issuedSignatureAlgorithm", Order = 3,
            EmitDefaultValue = false)]
        public SignatureAlgorithm? IssuedSignatureAlgorithm { get; set; }
    }
}
