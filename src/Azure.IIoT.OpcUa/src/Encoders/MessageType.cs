// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    /// <summary>
    /// The kind of notification a subscription raised, and with it the kind of
    /// data set message it is published as.
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
