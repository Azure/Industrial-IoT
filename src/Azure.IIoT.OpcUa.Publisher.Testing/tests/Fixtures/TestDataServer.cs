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
    public class TestDataServer : BaseServerFixture
    {
        /// <summary>
        /// Sample server nodes
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="timeservice"></param>
        public static IEnumerable<INodeManagerFactory> TestData(
            ILoggerFactory? factory, TimeService timeservice)
        {
            yield return new TestData.TestDataServer();
            yield return new MemoryBuffer.MemoryBufferServer();
            yield return new Boiler.BoilerServer();
            yield return new DataAccess.DataAccessServer();
        }

        /// <inheritdoc/>
        public TestDataServer() : base(TestData)
        {
        }

        /// <inheritdoc/>
        private TestDataServer(ILoggerFactory loggerFactory)
            : base(TestData, loggerFactory)
        {
        }

        /// <inheritdoc/>
        public static TestDataServer Create(ILoggerFactory loggerFactory)
        {
            return new TestDataServer(loggerFactory);
        }
    }
}
