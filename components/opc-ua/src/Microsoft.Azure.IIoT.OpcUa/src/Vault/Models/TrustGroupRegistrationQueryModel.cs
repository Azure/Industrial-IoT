// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using System;

    /// <summary>
    /// Trust group query model
    /// </summary>
    public sealed class TrustGroupRegistrationQueryModel {

        /// <summary>
        /// The name of the trust group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The identifer of the parent trust group.
        /// Use empty string to only return root groups.
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// The type of trust group.
        /// </summary>
        public TrustGroupType? Type { get; set; }

        /// <summary>
        /// A subject name of the group as distinguished name.
        /// </summary>
        public string SubjectName { get; set; }

        /// <summary>
        /// The lifetime of the trust group certificate.
        /// </summary>
        public TimeSpan? Lifetime { get; set; }

        /// <summary>
        /// The trust group certificate key size in bits.
        /// </summary>
        public ushort? KeySize { get; set; }

        /// <summary>
        /// The certificate signature algorithm.
        /// </summary>
        public SignatureAlgorithm? SignatureAlgorithm { get; set; }

        /// <summary>
        /// The issued certificate lifetime in months.
        /// </summary>
        public TimeSpan? IssuedLifetime { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        public ushort? IssuedKeySize { get; set; }

        /// <summary>
        /// The Signature algorithm for issued certificates
        /// </summary>
        public SignatureAlgorithm? IssuedSignatureAlgorithm { get; set; }
    }
}
