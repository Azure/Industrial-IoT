// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Text;

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
        public string ResponsePayload { get; }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="result"></param>
        public MethodCallStatusException(int result) :
            this("{}", result) {
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="responsePayload"></param>
        /// <param name="result"></param>
        public MethodCallStatusException(string responsePayload, int result) :
            base($"Response {result}: {responsePayload}") {
            Result = result;
            ResponsePayload = responsePayload;
        }
    }
}
