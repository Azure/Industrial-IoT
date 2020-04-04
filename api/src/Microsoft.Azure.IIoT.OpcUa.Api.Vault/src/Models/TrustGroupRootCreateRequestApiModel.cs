// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Trust group root registration model
    /// </summary>
    [DataContract]
    public sealed class TrustGroupRootCreateRequestApiModel {

        /// <summary>
        /// The new name of the trust group root
        /// </summary>
        [DataMember(Name = "name", Order = 0)]
        public string Name { get; set; }

        /// <summary>
        /// The trust group type.
        /// </summary>
        [DataMember(Name = "type", Order = 1)]
        public TrustGroupType Type { get; set; }

        /// <summary>
        /// The subject name of the group as distinguished name.
        /// </summary>
        [DataMember(Name = "subjectName", Order = 2)]
        public string SubjectName { get; set; }

        /// <summary>
        /// The lifetime of the trust group root certificate.
        /// </summary>
        [DataMember(Name = "lifetime", Order = 3)]
        public TimeSpan Lifetime { get; set; }

        /// <summary>
        /// The certificate key size in bits.
        /// </summary>
        [DataMember(Name = "keySize", Order = 4)]
        public ushort? KeySize { get; set; }

        /// <summary>
        /// The certificate signature algorithm.
        /// </summary>
        [DataMember(Name = "signatureAlgorithm", Order = 5)]
        public SignatureAlgorithm? SignatureAlgorithm { get; set; }

        /// <summary>
        /// The issued certificate lifetime.
        /// </summary>
        [DataMember(Name = "issuedLifetime", Order = 6)]
        public TimeSpan? IssuedLifetime { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        [DataMember(Name = "issuedKeySize", Order = 7)]
        public ushort? IssuedKeySize { get; set; }

        /// <summary>
        /// The issued certificate signature algorithm.
        /// </summary>
        [DataMember(Name = "issuedSignatureAlgorithm", Order = 8)]
        public SignatureAlgorithm? IssuedSignatureAlgorithm { get; set; }
    }
}
