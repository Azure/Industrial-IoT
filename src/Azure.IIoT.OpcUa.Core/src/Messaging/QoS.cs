// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging
{
    /// <summary>
    /// Quality of service
    /// </summary>
    public enum QoS
    {
        /// <summary>
        /// At most once delivery
        /// </summary>
        AtMostOnce = 0x00,

        /// <summary>
        /// At least once (with ack)
        /// </summary>
        AtLeastOnce = 0x01,

        /// <summary>
        /// Exactly once
        /// </summary>
        ExactlyOnce = 0x02
    }
}
