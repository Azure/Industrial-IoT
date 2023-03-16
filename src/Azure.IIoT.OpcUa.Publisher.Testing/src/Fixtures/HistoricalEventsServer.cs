// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Fixtures
{
    using Opc.Ua.Server;
    using System.Collections.Generic;

    /// <summary>
    /// Sample server fixture
    /// </summary>
    public class HistoricalEventsServer : BaseServerFixture
    {
        /// <summary>
        /// Sample server nodes
        /// </summary>
        public static IEnumerable<INodeManagerFactory> SampleServer
        {
            get
            {
                yield return new HistoricalEvents.HistoricalEventsServer();
            }
        }

        /// <inheritdoc/>
        public HistoricalEventsServer() :
            base(SampleServer)
        {
        }
    }
}
