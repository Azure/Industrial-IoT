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
        /// Is connection ok?
        /// </summary>
        bool IsConnectionOk { get; }

        /// <summary>
        /// Number of nodes currently connected and returning data
        /// </summary>
        int NumberOfGoodNodes { get; }

        /// <summary>
        /// Number of nodes currently not connected
        /// </summary>
        int NumberOfBadNodes { get; }

        /// <summary>
        /// The number of all monitored items value changes
        /// that have been invoked by this message source.
        /// </summary>
        ulong ValueChangesCount { get; }

        /// <summary>
        /// <see cref="ValueChangesCount"/> from the last minute
        /// </summary>
        ulong ValueChangesCountLastMinute { get; }

        /// <summary>
        /// The number of all dataChange Notifications
        /// that have been invoked by this message source.
        /// </summary>
        ulong DataChangesCount { get; }

        /// <summary>
        /// <see cref="DataChangesCount"/> from the last minute
        /// </summary>
        ulong DataChangesCountLastMinute { get; }

        /// <summary>
        /// Writer events
        /// </summary>
        event EventHandler<DataSetMessageModel> OnMessage;

        /// <summary>
        /// Called when ValueChangesCount or DataChangesCount are resetted
        /// </summary>
        event EventHandler<EventArgs> OnCounterReset;

        /// <summary>
        /// Run the group triggering
        /// </summary>
        Task RunAsync(CancellationToken ct);

        /// <summary>
        /// Allow takeover of newly configured subscriptions
        /// </summary>
        void Reconfigure(object config);
    }
}