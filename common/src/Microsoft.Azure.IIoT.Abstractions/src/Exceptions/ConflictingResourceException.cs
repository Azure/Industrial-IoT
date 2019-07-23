// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {
    using System;

    /// <summary>
    /// This exception is thrown when a client attempts to create a resource
    /// which would conflict with an existing one, for instance using the same
    /// identifier. The client should change the identifier or assume the
    /// resource has already been created.
    /// </summary>
    public class ConflictingResourceException : Exception {

        /// <inheritdoc />
        public ConflictingResourceException() {
        }

        /// <inheritdoc />
        public ConflictingResourceException(string message) :
            base(message) {
        }

        /// <inheritdoc />
        public ConflictingResourceException(string message, Exception innerException) :
            base(message, innerException) {
        }
    }
}
