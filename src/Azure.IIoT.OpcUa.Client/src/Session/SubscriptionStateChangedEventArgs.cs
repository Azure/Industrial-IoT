// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;

    /// <summary>
    /// The event arguments provided when the state of a subscription changes.
    /// </summary>
    public sealed class SubscriptionStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="changeMask"></param>
        internal SubscriptionStateChangedEventArgs(
            SubscriptionChangeMask changeMask)
        {
            Status = changeMask;
        }

        /// <summary>
        /// The changes that have affected the subscription.
        /// </summary>
        public SubscriptionChangeMask Status { get; }
    }
}
