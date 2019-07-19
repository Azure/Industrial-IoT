// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using System;

    /// <summary>
    /// Trust group update model
    /// </summary>
    public sealed class TrustGroupRegistrationUpdateModel {

        /// <summary>
        /// The name of the trust group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The issued certificate lifetime.
        /// </summary>
        public TimeSpan? IssuedLifetime { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        public ushort? IssuedKeySize { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        public SignatureAlgorithm? IssuedSignatureAlgorithm { get; set; }
    }
}
