// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Opc.Ua;
    using System.Collections.Generic;

    /// <summary>
    /// Subscriptions in a session
    /// </summary>
    public interface ISubscriptions
    {
        /// <summary>
        /// If the subscriptions are transferred when a session is
        /// recreated.
        /// </summary>
        /// <remarks>
        /// Default <c>false</c>, set to <c>true</c> if subscriptions
        /// should be transferred after recreating the session.
        /// Service must be supported by server.
        /// </remarks>
        bool TransferSubscriptionsOnRecreate { get; set; }

        /// <summary>
        /// Return diagnostics mask to use when sending publish requests.
        /// </summary>
        DiagnosticsMasks ReturnDiagnostics { get; set; }

        /// <summary>
        /// Gets and sets the maximum number of publish requests to
        /// be used in the session.
        /// </summary>
        int MaxPublishWorkerCount { get; set; }

        /// <summary>
        /// Gets and sets the minimum number of publish requests to be
        /// used in the session.
        /// </summary>
        int MinPublishWorkerCount { get; set; }

        /// <summary>
        /// Number of subscriptions
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Subscriptions
        /// </summary>
        IEnumerable<Subscription> Items { get; }

        /// <summary>
        /// Get the number of current publishing workers
        /// </summary>
        int PublishWorkerCount { get; }

        /// <summary>
        /// Good publish requests
        /// </summary>
        int GoodPublishRequestCount { get; }

        /// <summary>
        /// Bad publish requests
        /// </summary>
        int BadPublishRequestCount { get; }

        /// <summary>
        /// Update subscriptions
        /// </summary>
        void Update();
    }
}
