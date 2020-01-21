// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using System;

    /// <summary>
    /// Allowed characters
    /// </summary>
    [Flags]
    public enum AllowedChars {

        /// <summary>
        /// Upper case
        /// </summary>
        Uppercase = 1,

        /// <summary>
        /// Lower case
        /// </summary>
        Lowercase = 2,

        /// <summary>
        /// Digits
        /// </summary>
        Digits = 4,

        /// <summary>
        /// Special
        /// </summary>
        Special = 8,

        /// <summary>
        /// All
        /// </summary>
        All = 16
    }
}