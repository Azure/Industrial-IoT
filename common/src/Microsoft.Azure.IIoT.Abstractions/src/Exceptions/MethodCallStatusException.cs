// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {

    /// <summary>
    /// This exception is thrown when method call returned a
    /// status other than 200
    /// </summary>
    public class MethodCallStatusException : MethodCallException {

        /// <summary>
        /// Result of method call
        /// </summary>
        public int Result { get; }

        /// <summary>
        /// Payload
        /// </summary>
        public byte[] Payload { get; }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="result"></param>
        public MethodCallStatusException(int result) :
            this(new byte[0], result) {
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="result"></param>
        public MethodCallStatusException(byte[] payload, int result) :
            base($"Response {result}") {
            Result = result;
            Payload = payload;
        }
    }
}
