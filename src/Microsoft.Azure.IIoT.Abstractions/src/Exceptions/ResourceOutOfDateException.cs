// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {
    using System;

    /// <summary>
    /// This exception is thrown when a client attempts to update a resource
    /// providing the wrong Etag value. The client should retrieve the
    /// resource again, to have the new Etag, and retry.
    /// </summary>
    public class ResourceOutOfDateException : Exception {

        /// <inheritdoc />
        public ResourceOutOfDateException() {
        }

        /// <inheritdoc />
        public ResourceOutOfDateException(string message) :
            base(message) {
        }

        /// <inheritdoc />
        public ResourceOutOfDateException(string message, Exception innerException) :
            base(message, innerException) {
        }
    }
}
