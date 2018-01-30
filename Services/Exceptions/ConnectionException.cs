// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Exceptions {
    using System;

    /// <summary>
    /// Thrown when failing to connect to resource
    /// </summary>
    public class ConnectionException : ExternalDependencyException {

        public ConnectionException(string message) :
            base(message) {
        }

        public ConnectionException(string message, Exception innerException) :
            base(message, innerException) {
        }
    }
}