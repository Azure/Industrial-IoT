// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Message sink for a writer group
    /// </summary>
    public interface IMessageSink
    {
        /// <summary>
        /// Subscribe to writer messages
        /// </summary>
        /// <param name="notification"></param>
        ValueTask OnMessageAsync(OpcUaSubscriptionNotification notification);

        /// <summary>
        /// Called when ValueChangesCount or DataChangesCount are resetted
        /// </summary>
        ValueTask OnCounterResetAsync();
    }
}
