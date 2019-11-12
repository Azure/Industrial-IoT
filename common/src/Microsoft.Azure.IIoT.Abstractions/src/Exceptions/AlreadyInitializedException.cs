// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {
    /// <summary>
    /// Host already initialized
    /// </summary>
    public class AlreadyInitializedException : ResourceInvalidStateException {

        /// <inheritdoc/>
        public AlreadyInitializedException() :
            base("Instance has already been initialized.") {
        }
    }
}