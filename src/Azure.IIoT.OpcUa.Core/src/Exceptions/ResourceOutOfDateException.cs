// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Exceptions
{
    using System;

    /// <summary>
    /// This exception is thrown when a resource is out of date, e.g. an
    /// optimistic concurrency (etag) check failed.
    /// </summary>
    public class ResourceOutOfDateException : Exception
    {
        /// <inheritdoc />
        public ResourceOutOfDateException()
        {
        }

        /// <inheritdoc />
        public ResourceOutOfDateException(string message) :
            base(message)
        {
        }

        /// <inheritdoc />
        public ResourceOutOfDateException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
