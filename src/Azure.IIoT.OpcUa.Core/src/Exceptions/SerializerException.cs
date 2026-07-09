// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Exceptions
{
    using System;

    /// <summary>
    /// This exception is thrown when serialization or deserialization of a
    /// payload fails.
    /// </summary>
    public class SerializerException : Exception
    {
        /// <inheritdoc />
        public SerializerException() :
            base("Serialization error.")
        {
        }

        /// <inheritdoc />
        public SerializerException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public SerializerException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
