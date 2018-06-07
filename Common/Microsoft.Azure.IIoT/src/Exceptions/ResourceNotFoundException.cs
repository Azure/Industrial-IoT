// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {
    using System;

    /// <summary>
    /// This exception is thrown when a client is requesting a resource that
    /// doesn't exist.
    /// </summary>
    public class ResourceNotFoundException : Exception {

        /// <inheritdoc />
        public ResourceNotFoundException() {
        }

        /// <inheritdoc />
        public ResourceNotFoundException(string message) :
            base(message) {
        }

        /// <inheritdoc />
        public ResourceNotFoundException(string message, Exception innerException) :
            base(message, innerException) {
        }
    }
}
