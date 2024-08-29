// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This interface represents a registration of a
    /// subscription on a connection to a server. The
    /// registration must be disposed when done which will
    /// release the reference on the client.
    /// </summary>
    public interface ISubscription : IAsyncDisposable
    {
        /// <summary>
        /// State of the underlying client
        /// </summary>
        IOpcUaClientDiagnostics State { get; }

        /// <summary>
        /// Collect metadata
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="dataSetMetaData"></param>
        /// <param name="minorVersion"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<PublishedDataSetMetaDataModel> CollectMetaDataAsync(
            ISubscriber owner, DataSetMetaDataModel dataSetMetaData,
            uint minorVersion, CancellationToken ct = default);

        /// <summary>
        /// Create a keep alive notification
        /// </summary>
        /// <returns></returns>
        OpcUaSubscriptionNotification? CreateKeepAlive();

        /// <summary>
        /// Apply desired state of the subscription and its monitored items.
        /// This will attempt a differential update of the subscription
        /// and monitored items state. It is called periodically, when the
        /// configuration is updated or when a session is reconnected and
        /// the subscription needs to be recreated.
        /// </summary>
        void NotifyMonitoredItemsChanged();
    }
}
