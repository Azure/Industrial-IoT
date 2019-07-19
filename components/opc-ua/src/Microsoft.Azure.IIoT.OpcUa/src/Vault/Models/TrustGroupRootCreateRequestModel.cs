// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using System;

    /// <summary>
    /// Trust group anchor registration request model
    /// </summary>
    public sealed class TrustGroupRootCreateRequestModel {

        /// <summary>
        /// The name of the trust group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The trust group type.
        /// </summary>
        public TrustGroupType Type { get; set; }

        /// <summary>
        /// The subject name of the group as distinguished name.
        /// </summary>
        public string SubjectName { get; set; }

        /// <summary>
        /// The lifetime of the trust group certificate.
        /// </summary>
        public TimeSpan Lifetime { get; set; }

        /// <summary>
        /// The trust group certificate key size in bits.
        /// </summary>
        public ushort? KeySize { get; set; }

        /// <summary>
        /// The certificate signature algorithm.
        /// </summary>
        public SignatureAlgorithm? SignatureAlgorithm { get; set; }

        /// <summary>
        /// The lifetime of certificates issued in the group.
        /// </summary>
        public TimeSpan? IssuedLifetime { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        public ushort? IssuedKeySize { get; set; }

        /// <summary>
        /// The issued certificate signature algorithm.
        /// </summary>
        public SignatureAlgorithm? IssuedSignatureAlgorithm { get; set; }
    }
}
