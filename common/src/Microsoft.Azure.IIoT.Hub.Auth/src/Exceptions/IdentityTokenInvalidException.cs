// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using System;

    /// <summary>
    /// Identity token invalid
    /// </summary>
    public class IdentityTokenInvalidException : FormatException {

        /// <inheritdoc/>
        public IdentityTokenInvalidException(string id) : base(
            $"The device with id '{id}' contains desired property " +
            $"'{Constants.IdentityTokenPropertyName}', but the token value is empty.") {
        }

        /// <inheritdoc/>
        public IdentityTokenInvalidException(string id, Exception ex) : base(
            $"The device with id '{id}' contains desired property " +
            $"'{Constants.IdentityTokenPropertyName}', but the token value is invalid.", ex) {
        }
    }
}