// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures {
    using Opc.Ua.Server;
    using System.Collections.Generic;

    /// <summary>
    /// Sample server fixture
    /// </summary>
    public class HistoryServerFixture : BaseServerFixture {

        /// <summary>
        /// Sample server nodes
        /// </summary>
        public static IEnumerable<INodeManagerFactory> SampleServer {
            get {
                yield return new HistoricalAccess.HistoricalAccessServer();
                yield return new HistoricalEvents.HistoricalEventsServer();
            }
        }

        /// <inheritdoc/>
        public HistoryServerFixture() :
            base(SampleServer) {
        }
    }
}
