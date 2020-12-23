// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using System;

    /// <summary>
    /// Event args for message receive events
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs {

        /// <summary>
        /// Message
        /// </summary>
        public IMessageData Message { get; }

        /// <summary>
        /// Event args for message
        /// </summary>
        /// <param name="message"></param>
        public MessageReceivedEventArgs(IMessageData message) {
            Message = message;
        }
    }
}