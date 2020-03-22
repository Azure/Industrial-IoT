// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Trust group registration request model
    /// </summary>
    [DataContract]
    public sealed class TrustGroupRegistrationRequestApiModel {

        /// <summary>
        /// The new name of the trust group
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// The identifer of the parent trust group.
        /// </summary>
        [DataMember(Name = "parentId")]
        public string ParentId { get; set; }

        /// <summary>
        /// The subject name of the group as distinguished name.
        /// </summary>
        [DataMember(Name = "subjectName")]
        public string SubjectName { get; set; }

        /// <summary>
        /// The lifetime of certificates issued in the group.
        /// </summary>
        [DataMember(Name = "issuedLifetime")]
        public TimeSpan? IssuedLifetime { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        [DataMember(Name = "issuedKeySize")]
        public ushort? IssuedKeySize { get; set; }

        /// <summary>
        /// The issued certificate signature algorithm.
        /// </summary>
        [DataMember(Name = "issuedSignatureAlgorithm")]
        public SignatureAlgorithm? IssuedSignatureAlgorithm { get; set; }
    }
}
