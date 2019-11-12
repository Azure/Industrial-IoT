// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Message triggering support
    /// </summary>
    public interface IMessageTrigger {

        /// <summary>
        /// Identifier of the trigger
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Number of retries
        /// </summary>
        long NumberOfConnectionRetries { get; }

        /// <summary>
        /// Receive triggered messages
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Run the triggering mechanism
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RunAsync(CancellationToken ct);
    }
}