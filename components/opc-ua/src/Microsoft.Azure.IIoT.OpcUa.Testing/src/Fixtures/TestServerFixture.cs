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
    public class TestServerFixture : BaseServerFixture {

        /// <summary>
        /// Sample server nodes
        /// </summary>
        public static IEnumerable<INodeManagerFactory> SampleServer {
            get {
                yield return new TestData.TestDataServer();
                yield return new MemoryBuffer.MemoryBufferServer();
                yield return new Boiler.BoilerServer();
                yield return new DataAccess.DataAccessServer();
            }
        }

        /// <inheritdoc/>
        public TestServerFixture() :
            base(SampleServer) {
        }
    }
}
