// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Exceptions
{
    using System;

    /// <summary>
    /// Thrown when method call failed
    /// </summary>
    public class MethodCallException : Exception
    {
        /// <inheritdoc/>
        public MethodCallException(string message) :
            base(message)
        {
        }

        /// <inheritdoc/>
        public MethodCallException(string message, Exception innerException) :
            base(message, innerException)
        {
        }

        /// <inheritdoc/>
        public MethodCallException()
        {
        }
    }
}
