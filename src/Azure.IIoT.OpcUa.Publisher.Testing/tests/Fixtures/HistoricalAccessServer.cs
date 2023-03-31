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
    public class HistoricalAccessServer : BaseServerFixture
    {
        /// <summary>
        /// Sample server nodes
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="timeservice"></param>
        public static IEnumerable<INodeManagerFactory> HistoricalAccess(
            ILoggerFactory? factory, TimeService timeservice)
        {
            yield return new HistoricalAccess.HistoricalAccessServer(timeservice);
        }

        /// <inheritdoc/>
        public HistoricalAccessServer()
            : base(HistoricalAccess)
        {
        }

        /// <inheritdoc/>
        private HistoricalAccessServer(ILoggerFactory loggerFactory)
            : base(HistoricalAccess, loggerFactory)
        {
        }

        /// <inheritdoc/>
        public static HistoricalAccessServer Create(ILoggerFactory loggerFactory)
        {
            return new HistoricalAccessServer(loggerFactory);
        }
    }
}
