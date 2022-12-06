// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures {
    using Opc.Ua.Server;
    using System.Collections.Generic;

    /// <summary>
    /// Reference server fixture
    /// </summary>
    public class ReferenceServerFixture : BaseServerFixture {

        /// <summary>
        /// Sample server nodes
        /// </summary>
        public static IEnumerable<INodeManagerFactory> SampleServer {
            get {
                yield return new TestData.TestDataServer();
                yield return new MemoryBuffer.MemoryBufferServer();
                yield return new Boiler.BoilerServer();
                yield return new Vehicles.VehiclesServer();
                yield return new Reference.ReferenceServer();
                yield return new HistoricalEvents.HistoricalEventsServer();
                yield return new HistoricalAccess.HistoricalAccessServer();
                yield return new Views.ViewsServer();
                yield return new DataAccess.DataAccessServer();
                yield return new Alarms.AlarmConditionServer();
                yield return new SimpleEvents.SimpleEventsServer();
                yield return new Plc.PlcServer();
            }
        }

        /// <inheritdoc/>
        public ReferenceServerFixture() :
            base(SampleServer) {
        }
    }
}
