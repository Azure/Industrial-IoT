// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Writer group
    /// </summary>
    public interface IMessageSource
    {
        /// <summary>
        /// Subscribe to writer messages
        /// </summary>
        event EventHandler<OpcUaSubscriptionNotification>? OnMessage;

        /// <summary>
        /// Called when ValueChangesCount or DataChangesCount are resetted
        /// </summary>
        event EventHandler<EventArgs>? OnCounterReset;

        /// <summary>
        /// Start trigger
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask StartAsync(CancellationToken ct);

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask UpdateAsync(WriterGroupModel writerGroup,
            CancellationToken ct);
    }
}
