// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;

    /// <summary>
    /// The delegate used to receive pubish error notifications.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="e"></param>
    public delegate void PublishErrorEventHandler(ISession session,
        PublishErrorEventArgs e);

    /// <summary>
    /// Represents the event arguments provided when a publish error occurs.
    /// </summary>
    public sealed class PublishErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="status"></param>
        public PublishErrorEventArgs(ServiceResult status)
        {
            Status = status;
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="sequenceNumber"></param>
        internal PublishErrorEventArgs(ServiceResult status, uint subscriptionId, uint sequenceNumber)
        {
            Status = status;
            SubscriptionId = subscriptionId;
            SequenceNumber = sequenceNumber;
        }

        /// <summary>
        /// Gets the status associated with the keep alive operation.
        /// </summary>
        public ServiceResult Status { get; }

        /// <summary>
        /// Gets the subscription with the message that could not be republished.
        /// </summary>
        public uint SubscriptionId { get; }

        /// <summary>
        /// Gets the sequence number for the message that could not be republished.
        /// </summary>
        public uint SequenceNumber { get; }
    }
}
