// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using Microsoft.Azure.IIoT.Exceptions;

    /// <summary>
    /// Identity token not found
    /// </summary>
    public class IdentityTokenNotFoundException : ResourceNotFoundException {

        /// <inheritdoc/>
        public IdentityTokenNotFoundException(string identity) :
            base($"The device with id '{identity}' does not contain" +
                $" desired property '{Constants.IdentityTokenPropertyName}'.") {
        }
    }
}