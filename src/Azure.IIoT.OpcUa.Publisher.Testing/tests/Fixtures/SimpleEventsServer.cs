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
    /// Simple events fixture
    /// </summary>
    public class SimpleEventsServer : BaseServerFixture
    {
        /// <inheritdoc/>
        public static IEnumerable<INodeManagerFactory> SimpleEvents(
            ILoggerFactory? factory, TimeService timeservice)
        {
            yield return new SimpleEvents.SimpleEventsServer();
        }

        /// <inheritdoc/>
        public SimpleEventsServer()
            : base(SimpleEvents)
        {
        }

        /// <inheritdoc/>
        private SimpleEventsServer(ILoggerFactory loggerFactory)
            : base(SimpleEvents, loggerFactory)
        {
        }

        /// <inheritdoc/>
        public static SimpleEventsServer Create(ILoggerFactory loggerFactory)
        {
            return new SimpleEventsServer(loggerFactory);
        }
    }
}
