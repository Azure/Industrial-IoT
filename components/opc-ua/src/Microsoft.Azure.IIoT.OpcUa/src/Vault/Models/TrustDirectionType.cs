// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using System;

    /// <summary>
    /// Trust direction type
    /// </summary>
    [Flags]
    public enum TrustDirectionType {

        /// <summary>
        /// Subject is trusting object entities
        /// </summary>
        Trusting = 0x1,

        /// <summary>
        /// Object entity is trusted by subjects
        /// </summary>
        Trusted = 0x10
    }
}