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
        public string ResponsePayload { get; }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="result"></param>
        /// <param name="errorMessage"></param>
        public MethodCallStatusException(int result, string errorMessage = null) :
            this("{}", result, errorMessage) {
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="responsePayload"></param>
        /// <param name="result"></param>
        /// <param name="errorMessage"></param>
        public MethodCallStatusException(string responsePayload, int result,
            string errorMessage = null) :
            base($"{{\"Message\":\"Response {result} {errorMessage ?? ""}\",\"Details\":{responsePayload ?? "null"}}}") {
            Result = result;
            ResponsePayload = $"{{\"Message\":\"Response {result} {errorMessage ?? ""}\",\"Details\":{responsePayload ?? "null"}}}";
        }
    }
}
