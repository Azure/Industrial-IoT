// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Exceptions
{
    using System;

    /// <summary>
    /// This exception is thrown when a resource has been exhausted, e.g.
    /// a pool or quota has been used up.
    /// </summary>
    public class ResourceExhaustionException : Exception
    {
        /// <inheritdoc />
        public ResourceExhaustionException()
        {
        }

        /// <inheritdoc />
        public ResourceExhaustionException(string message) :
            base(message)
        {
        }

        /// <inheritdoc />
        public ResourceExhaustionException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
