// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {
    using System;

    /// <summary>
    /// This exception is thrown when a resource or service outside of
    /// the current process fails to perform its task.  It is meant to
    /// be extended to provide more detailed semantics.
    /// </summary>
    public class ExternalDependencyException : Exception {

        /// <inheritdoc />
        public ExternalDependencyException() {
        }

        /// <inheritdoc />
        public ExternalDependencyException(string message) :
            base(message) {
        }

        /// <inheritdoc />
        public ExternalDependencyException(string message, Exception innerException) :
            base(message, innerException) {
        }
    }
}
