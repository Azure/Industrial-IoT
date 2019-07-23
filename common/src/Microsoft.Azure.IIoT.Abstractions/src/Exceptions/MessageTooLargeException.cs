// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {
    using System;

    /// <summary>
    /// Thrown when a message does not fit into the allowed
    /// max buffer size.
    /// </summary>
    public class MessageTooLargeException : Exception {

        /// <summary>
        /// Actual size
        /// </summary>
        public int MessageSize { get; set; }

        /// <summary>
        /// Max allowed size
        /// </summary>
        public int MaxMessageSize { get; set; }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageSize"></param>
        /// <param name="maxMessageSize"></param>
        public MessageTooLargeException(string message,
            int messageSize, int maxMessageSize) : base(message) {

            MessageSize = messageSize;
            MaxMessageSize = maxMessageSize;
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="message"></param>
        public MessageTooLargeException(string message) :
            this(message, -1, -1) {
        }
    }
}
