// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Exceptions
{
    using System;

    /// <summary>
    /// This exception is thrown when a resource the client is trying to
    /// create already exists.
    /// </summary>
    public class ResourceConflictException : Exception
    {
        /// <inheritdoc />
        public ResourceConflictException()
        {
        }

        /// <inheritdoc />
        public ResourceConflictException(string message) :
            base(message)
        {
        }

        /// <inheritdoc />
        public ResourceConflictException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
