// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Exceptions
{
    using System;

    /// <summary>
    /// This exception is thrown when the configuration provided to the
    /// service is invalid and must be corrected.
    /// </summary>
    public class InvalidConfigurationException : Exception
    {
        /// <inheritdoc />
        public InvalidConfigurationException()
        {
        }

        /// <inheritdoc />
        public InvalidConfigurationException(string message) :
            base(message)
        {
        }

        /// <inheritdoc />
        public InvalidConfigurationException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
