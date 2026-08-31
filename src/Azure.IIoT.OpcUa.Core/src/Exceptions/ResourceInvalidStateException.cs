// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Exceptions
{
    using System;

    /// <summary>
    /// This exception is thrown when a resource is in an invalid state for
    /// the requested operation.
    /// </summary>
    public class ResourceInvalidStateException : Exception
    {
        /// <inheritdoc />
        public ResourceInvalidStateException()
        {
        }

        /// <inheritdoc />
        public ResourceInvalidStateException(string message) :
            base(message)
        {
        }

        /// <inheritdoc />
        public ResourceInvalidStateException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
