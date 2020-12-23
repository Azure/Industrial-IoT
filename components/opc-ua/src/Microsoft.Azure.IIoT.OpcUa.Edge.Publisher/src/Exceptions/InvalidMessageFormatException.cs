// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Exceptions {
    using System;

    /// <summary>
    /// Invalid message format specified
    /// </summary>
    public class InvalidMessageFormatException : ArgumentException {

        /// <inheritdoc/>
        public InvalidMessageFormatException() {
        }

        /// <inheritdoc/>
        public InvalidMessageFormatException(string message) :
            base(message) {
        }

        /// <inheritdoc/>
        public InvalidMessageFormatException(string message, Exception innerException) :
            base(message, innerException) {
        }
    }
}