// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription abstraction
    /// </summary>
    public interface ISubscription : IDisposable {

        /// <summary>
        /// Subscription change events
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> OnSubscriptionMessage;

        /// <summary>
        /// Item change events
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> OnMonitoredItemSample;

        /// <summary>
        /// Identifier of the subscription
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Connection
        /// </summary>
        ConnectionModel Connection { get; }

        /// <summary>
        /// Number of retries on the session
        /// </summary>
        long NumberOfConnectionRetries { get; }

        /// <summary>
        /// Last values
        /// </summary>
        Dictionary<string, DataValue> LastValues { get; }

        /// <summary>
        /// Retrieve current service message context
        /// </summary>
        /// <returns></returns>
        Task<ServiceMessageContext> GetServiceMessageContextAsync();

        /// <summary>
        /// Synchronize monitored items in the subscription
        /// </summary>
        /// <param name="monitoredItems"></param>
        /// <returns></returns>
        Task ApplyAsync(IEnumerable<MonitoredItemModel> monitoredItems);

        /// <summary>
        /// Close and delete subscription
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
    }
}