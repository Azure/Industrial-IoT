// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Exceptions {
    using Microsoft.Azure.IoTSolutions.Common.Exceptions;
    using System;

    /// <summary>
    /// This exception is thrown when method call returned a
    /// status other than 200
    /// </summary>
    public class MethodCallStatusException : Exception {

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="result"></param>
        /// <param name="response"></param>
        public MethodCallStatusException(int result, string response) :
            base ($"Response {result}", new ExternalDependencyException(response)){
            Result = result;
        }

        public int Result { get; }
    }
}
