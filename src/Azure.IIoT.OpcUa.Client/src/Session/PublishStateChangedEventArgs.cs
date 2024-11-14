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
    public sealed class PublishStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="changeMask"></param>
        internal PublishStateChangedEventArgs(PublishStateChangedMask changeMask)
        {
            Status = changeMask;
        }

        /// <summary>
        /// The publish state changes.
        /// </summary>
        public PublishStateChangedMask Status { get; }
    }
}
