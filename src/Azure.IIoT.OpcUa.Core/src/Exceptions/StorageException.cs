// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Exceptions
{
    using System;

    /// <summary>
    /// This exception is thrown when an underlying storage operation fails.
    /// </summary>
    public class StorageException : ExternalDependencyException
    {
        /// <inheritdoc />
        public StorageException() :
            base("Storage error.")
        {
        }

        /// <inheritdoc />
        public StorageException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public StorageException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
