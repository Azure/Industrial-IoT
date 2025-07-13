// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    /// <summary>
    /// Message type
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Delta frame
        /// </summary>
        DeltaFrame,

        /// <summary>
        /// Key frame
        /// </summary>
        KeyFrame,

        /// <summary>
        /// Event
        /// </summary>
        Event,

        /// <summary>
        /// Keep alive
        /// </summary>
        KeepAlive,

        /// <summary>
        /// Condition
        /// </summary>
        Condition,

        /// <summary>
        /// Metadata
        /// </summary>
        Metadata,

        /// <summary>
        /// Close notification
        /// </summary>
        Closed
    }
}
