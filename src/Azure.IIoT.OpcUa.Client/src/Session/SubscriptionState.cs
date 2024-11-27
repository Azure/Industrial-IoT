// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    /// <summary>
    /// Session state
    /// </summary>
    public enum SubscriptionState
    {
        /// <summary>
        /// Subscription is opened
        /// </summary>
        Opened,

        /// <summary>
        /// Subscription created
        /// </summary>
        Created,

        /// <summary>
        /// Subscription modified
        /// </summary>
        Modified,

        /// <summary>
        /// Subscription error
        /// </summary>
        Error,

        /// <summary>
        /// Subscription closed
        /// </summary>
        Deleted
    }
}
