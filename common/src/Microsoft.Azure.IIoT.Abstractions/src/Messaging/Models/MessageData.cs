// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {

    /// <summary>
    /// Base message data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MessageData<T> : IMessageData<T> {

        /// <inheritdoc/>
        public string Id { get; set; }
        /// <inheritdoc/>
        public T Value { get; set; }

        /// <inheritdoc/>
        object IMessageData.Value => Value;

        /// <summary>
        /// Create message
        /// </summary>
        public MessageData() {
        }

        /// <summary>
        /// Create message
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        public MessageData(string id, T value) {
            Id = id;
            Value = value;
        }
     }
}