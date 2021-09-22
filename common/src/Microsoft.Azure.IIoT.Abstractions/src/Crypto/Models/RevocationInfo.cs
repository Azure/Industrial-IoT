// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;

    /// <summary>
    /// Certificate Revocation information
    /// </summary>
    public class RevocationInfo {

        /// <summary>
        /// Revocation date
        /// </summary>
        public DateTime? Date { get; set; }
    }
}

