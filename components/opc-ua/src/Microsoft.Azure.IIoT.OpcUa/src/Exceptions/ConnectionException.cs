// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Exceptions {
    using Microsoft.Azure.IIoT.Exceptions;
    using System;

    /// <summary>
    /// Thrown when failing to connect to resource
    /// </summary>
    public class ConnectionException : CommunicationException {

        /// <inheritdoc/>
        public ConnectionException(string message) :
            base(message) {
        }

        /// <inheritdoc/>
        public ConnectionException(string message, Exception innerException) :
            base(message, innerException) {
        }
    }
}
