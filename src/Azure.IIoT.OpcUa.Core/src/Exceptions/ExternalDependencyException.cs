// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Exceptions
{
    using System;

    /// <summary>
    /// This exception is thrown when an external dependency the service
    /// relies on fails.
    /// </summary>
    public class ExternalDependencyException : Exception
    {
        /// <inheritdoc />
        public ExternalDependencyException()
        {
        }

        /// <inheritdoc />
        public ExternalDependencyException(string message) :
            base(message)
        {
        }

        /// <inheritdoc />
        public ExternalDependencyException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
