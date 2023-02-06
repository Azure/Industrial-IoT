// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Writer group
    /// </summary>
    public interface IMessageSource : IAsyncDisposable {

        /// <summary>
        /// Subscribe to writer messages
        /// </summary>
        event EventHandler<SubscriptionNotificationModel> OnMessage;

        /// <summary>
        /// Called when ValueChangesCount or DataChangesCount are resetted
        /// </summary>
        event EventHandler<EventArgs> OnCounterReset;

        /// <summary>
        /// Start trigger
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask StartAsync(CancellationToken ct);

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="config"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask UpdateAsync(WriterGroupJobModel config,
            CancellationToken ct);
    }
}
