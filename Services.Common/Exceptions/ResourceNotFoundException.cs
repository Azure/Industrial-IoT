// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Common.Exceptions {
    using System;

    /// <summary>
    /// This exception is thrown when a client is requesting a resource that
    /// doesn't exist.
    /// </summary>
    public class ResourceNotFoundException : Exception {
        public ResourceNotFoundException() {
        }

        public ResourceNotFoundException(string message) :
            base(message) {
        }

        public ResourceNotFoundException(string message, Exception innerException) :
            base(message, innerException) {
        }
    }
}
