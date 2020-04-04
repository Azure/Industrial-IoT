// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Trust group model
    /// </summary>
    [DataContract]
    public sealed class TrustGroupApiModel {

        /// <summary>
        /// The name of the trust group.
        /// </summary>
        [DataMember(Name = "name", Order = 0)]
        public string Name { get; set; }

        /// <summary>
        /// The identifer of the parent trust group.
        /// </summary>
        [DataMember(Name = "parentId", Order = 1)]
        public string ParentId { get; set; }

        /// <summary>
        /// The trust group type
        /// </summary>
        [DataMember(Name = "type", Order = 2)]
        public TrustGroupType Type { get; set; }

        /// <summary>
        /// The subject name of the group as distinguished name.
        /// </summary>
        [DataMember(Name = "subjectName", Order = 3)]
        public string SubjectName { get; set; }

        /// <summary>
        /// The lifetime of the trust group certificate.
        /// </summary>
        [DataMember(Name = "lifetime", Order = 4)]
        public TimeSpan Lifetime { get; set; }

        /// <summary>
        /// The trust group certificate key size in bits.
        /// </summary>
        [DataMember(Name = "keySize", Order = 5)]
        public ushort KeySize { get; set; }

        /// <summary>
        /// The certificate signature algorithm.
        /// </summary>
        [DataMember(Name = "signatureAlgorithm", Order = 6)]
        public SignatureAlgorithm SignatureAlgorithm { get; set; }

        /// <summary>
        /// The issued certificate lifetime in months.
        /// </summary>
        [DataMember(Name = "issuedLifetime", Order = 7)]
        public TimeSpan IssuedLifetime { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        [DataMember(Name = "issuedKeySize", Order = 8)]
        public ushort IssuedKeySize { get; set; }

        /// <summary>
        /// The Signature algorithm for issued certificates
        /// </summary>
        [DataMember(Name = "issuedSignatureAlgorithm", Order = 9)]
        public SignatureAlgorithm IssuedSignatureAlgorithm { get; set; }
    }
}
