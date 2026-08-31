// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Exceptions
{
    using System;

    /// <summary>
    /// Thrown when a message does not fit into the allowed
    /// max buffer size.
    /// </summary>
    public class MessageSizeLimitException : Exception
    {
        /// <summary>
        /// Actual size
        /// </summary>
        public int MessageSize { get; set; }

        /// <summary>
        /// Max allowed size
        /// </summary>
        public int MaxMessageSize { get; set; }

        /// <inheritdoc />
        public MessageSizeLimitException() :
            this("Message size limit exceeded.")
        {
        }

        /// <inheritdoc />
        public MessageSizeLimitException(string message) :
            this(message, -1, -1)
        {
        }

        /// <inheritdoc />
        public MessageSizeLimitException(string message,
            Exception innerException) :
            this(message, -1, -1, innerException)
        {
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageSize"></param>
        /// <param name="maxMessageSize"></param>
        public MessageSizeLimitException(string message, int messageSize,
            int maxMessageSize) : base(message)
        {
            MessageSize = messageSize;
            MaxMessageSize = maxMessageSize;
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageSize"></param>
        /// <param name="maxMessageSize"></param>
        /// <param name="innerException"></param>
        public MessageSizeLimitException(string message, int messageSize,
            int maxMessageSize, Exception innerException) :
            base(message, innerException)
        {
            MessageSize = messageSize;
            MaxMessageSize = maxMessageSize;
        }
    }
}
