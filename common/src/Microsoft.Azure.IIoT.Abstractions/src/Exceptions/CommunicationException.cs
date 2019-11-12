// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {
    using System;

    /// <summary>
    /// Thrown when failing to connect to resource
    /// </summary>
    public class CommunicationException : Exception {

        /// <inheritdoc />
        public CommunicationException(string message) :
            base(message) {
        }

        /// <inheritdoc />
        public CommunicationException(string message, Exception innerException) :
            base(message, innerException) {
        }

        /// <inheritdoc />
        public CommunicationException() {
        }
    }
}
