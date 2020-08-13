// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Writer group
    /// </summary>
    public interface IMessageTrigger : IDisposable {

        /// <summary>
        /// Writer group id
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Number of retries
        /// </summary>
        int NumberOfConnectionRetries { get; }

        /// <summary>
        /// The number of all monitored items value changes
        /// that have been invoked by this message source.
        /// </summary>
        ulong ValueChangesCount { get; }

        /// <summary>
        /// The number of all dataChange Notifications
        /// that have been invoked by this message source.
        /// </summary>
        ulong DataChangesCount { get; }

        /// <summary>
        /// Writer events
        /// </summary>
        event EventHandler<DataSetMessageModel> OnMessage;

        /// <summary>
        /// Run the group triggering
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RunAsync(CancellationToken ct);
    }
}