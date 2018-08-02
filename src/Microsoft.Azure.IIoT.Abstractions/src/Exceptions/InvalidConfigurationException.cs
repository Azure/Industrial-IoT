// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {
    using System;

    /// <summary>
    /// This exception is thrown when the service is configured incorrectly.
    /// In order to recover, the service owner should fix the configuration
    /// and re-deploy the service.
    /// </summary>
    public class InvalidConfigurationException : Exception {

        /// <inheritdoc />
        public InvalidConfigurationException() {
        }

        /// <inheritdoc />
        public InvalidConfigurationException(string message) :
            base(message) {
        }

        /// <inheritdoc />
        public InvalidConfigurationException(string message, Exception innerException) :
            base(message, innerException) {
        }
    }
}
