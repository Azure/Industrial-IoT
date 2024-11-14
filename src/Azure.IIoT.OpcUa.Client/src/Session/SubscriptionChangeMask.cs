// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;

    /// <summary>
    /// Flags indicating what has changed in a subscription.
    /// </summary>
    [Flags]
    public enum SubscriptionChangeMask
    {
        /// <summary>
        /// The subscription has not changed.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// The subscription was created on the server.
        /// </summary>
        Created = 0x01,

        /// <summary>
        /// The subscription was deleted on the server.
        /// </summary>
        Deleted = 0x02,

        /// <summary>
        /// The subscription was modified on the server.
        /// </summary>
        Modified = 0x04,

        /// <summary>
        /// Monitored items were added to the subscription
        /// (but not created on the server)
        /// </summary>
        ItemsAdded = 0x08,

        /// <summary>
        /// Monitored items were removed to the
        /// subscription (but not deleted on the server)
        /// </summary>
        ItemsRemoved = 0x10,

        /// <summary>
        /// Monitored items were created on the server.
        /// </summary>
        ItemsCreated = 0x20,

        /// <summary>
        /// Monitored items were deleted on the server.
        /// </summary>
        ItemsDeleted = 0x40,

        /// <summary>
        /// Monitored items were modified on the server.
        /// </summary>
        ItemsModified = 0x80,

        /// <summary>
        /// Subscription was transferred on the server.
        /// </summary>
        Transferred = 0x100
    }
}
