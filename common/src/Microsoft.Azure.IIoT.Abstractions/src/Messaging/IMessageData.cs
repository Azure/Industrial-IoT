// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {

    /// <summary>
    /// Generic message data
    /// </summary>
    public interface IMessageData {

        /// <summary>
        /// Identifier
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Message
        /// </summary>
        object Value { get; }
    }
}