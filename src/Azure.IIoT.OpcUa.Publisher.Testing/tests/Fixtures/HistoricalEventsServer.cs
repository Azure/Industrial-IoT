// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Fixtures
{
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Server;
    using Opc.Ua.Test;
    using System.Collections.Generic;

    /// <summary>
    /// Sample server fixture
    /// </summary>
    public class HistoricalEventsServer : BaseServerFixture
    {
        /// <summary>
        /// Sample server nodes
        /// </summary>
        public static IEnumerable<INodeManagerFactory> HistoricalEvents(
            ILoggerFactory factory, TimeService timeservice)
        {
            yield return new global::HistoricalEvents.HistoricalEventsServer(timeservice);
        }

        /// <inheritdoc/>
        public HistoricalEventsServer()
            : base(HistoricalEvents)
        {
        }

        /// <inheritdoc/>
        private HistoricalEventsServer(ILoggerFactory loggerFactory)
            : base(HistoricalEvents, loggerFactory)
        {
        }

        /// <inheritdoc/>
        public static HistoricalEventsServer Create(ILoggerFactory loggerFactory)
        {
            return new HistoricalEventsServer(loggerFactory);
        }
    }
}
