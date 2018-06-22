// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Proxy.Exceptions {
    using Microsoft.Azure.IIoT.Exceptions;

    /// <summary>
    /// This exception is thrown when a client sends a request
    /// too large to handle, depending on the configured settings.
    /// </summary>
    public class HttpPayloadTooLargeException : MessageTooLargeException {

        /// <inheritdoc/>
        public HttpPayloadTooLargeException(string message) :
            base(message) {
        }

        /// <inheritdoc/>
        public HttpPayloadTooLargeException(string message, int messageSize,
            int maxMessageSize) : base(message, messageSize, maxMessageSize) {
        }
    }
}
