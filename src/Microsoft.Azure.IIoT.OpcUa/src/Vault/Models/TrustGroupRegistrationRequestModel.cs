// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using System;

    /// <summary>
    /// Trust group registration request model
    /// </summary>
    public sealed class TrustGroupRegistrationRequestModel {

        /// <summary>
        /// The name of the trust group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The identifer of the parent trust group.
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// The subject name of the group as distinguished name.
        /// </summary>
        public string SubjectName { get; set; }

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
