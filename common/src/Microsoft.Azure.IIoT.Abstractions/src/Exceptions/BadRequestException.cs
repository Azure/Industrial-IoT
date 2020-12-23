// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {
    using System;

    /// <summary>
    /// This exception is thrown when the input validation
    /// fails. The client should fix the request before retrying.
    /// </summary>
    public class BadRequestException : ArgumentException {

        /// <inheritdoc />
        public BadRequestException() {
        }

        /// <inheritdoc />
        public BadRequestException(string message) :
            base(message) {
        }

        /// <inheritdoc />
        public BadRequestException(string message, Exception innerException) :
            base(message, innerException) {
        }
    }
}
