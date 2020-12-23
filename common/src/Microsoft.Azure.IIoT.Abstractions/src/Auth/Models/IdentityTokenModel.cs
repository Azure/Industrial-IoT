// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Models {
    using System;

    /// <summary>
    /// Identity token
    /// </summary>
    public class IdentityTokenModel {

        /// <summary>
        /// Identity
        /// </summary>
        public string Identity { get; set; }

        /// <summary>
        /// Key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Expiration
        /// </summary>
        public DateTime Expires { get; set; }
    }
}