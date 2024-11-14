// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;

    /// <summary>
    /// The delegate used to modify publish response sequence numbers to acknowledge.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="e"></param>
    public delegate void PublishSequenceNumbersToAcknowledgeEventHandler(
        ISession session, PublishSequenceNumbersToAcknowledgeEventArgs e);

    /// <summary>
    /// Represents the event arguments provided when publish response
    /// sequence numbers are about to be achknoledged with a publish request.
    /// </summary>
    /// <remarks>
    /// A callee can defer an acknowledge to the next publish request by
    /// moving the <see cref="SubscriptionAcknowledgement"/> to the deferred list.
    /// The callee can modify the list of acknowledgements to send, it is the
    /// responsibility of the caller to protect the lists for modifications.
    /// </remarks>
    public sealed class PublishSequenceNumbersToAcknowledgeEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="acknowledgementsToSend"></param>
        /// <param name="deferredAcknowledgementsToSend"></param>
        public PublishSequenceNumbersToAcknowledgeEventArgs(
            SubscriptionAcknowledgementCollection acknowledgementsToSend,
            SubscriptionAcknowledgementCollection deferredAcknowledgementsToSend)
        {
            AcknowledgementsToSend = acknowledgementsToSend;
            DeferredAcknowledgementsToSend = deferredAcknowledgementsToSend;
        }

        /// <summary>
        /// The acknowledgements which are sent with the next publish request.
        /// </summary>
        /// <remarks>
        /// A client may also chose to remove an acknowledgement from this list to add it back
        /// to the list in a subsequent callback when the request is fully processed.
        /// </remarks>
        public SubscriptionAcknowledgementCollection AcknowledgementsToSend { get; }

        /// <summary>
        /// The deferred list of acknowledgements.
        /// </summary>
        /// <remarks>
        /// The callee can transfer an outstanding <see cref="SubscriptionAcknowledgement"/>
        /// to this list to defer the acknowledge of a sequence number to the next publish request.
        /// </remarks>
        public SubscriptionAcknowledgementCollection DeferredAcknowledgementsToSend { get; }
    }
}
