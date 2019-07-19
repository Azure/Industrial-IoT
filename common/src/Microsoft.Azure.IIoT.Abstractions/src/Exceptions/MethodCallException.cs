// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {
    using System;

    /// <summary>
    /// Thrown when method call failed
    /// </summary>
    public class MethodCallException : Exception {

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="message"></param>
        public MethodCallException(string message) :
            base(message) {
        }

        /// <summary>
        /// Create exception with inner
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public MethodCallException(string message, Exception innerException) :
            base(message, innerException) {
        }
    }
}
