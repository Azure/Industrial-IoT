// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Fixtures
{
    using Furly.Extensions.Logging;
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Server;
    using Opc.Ua.Test;
    using System.Collections.Generic;

    /// <summary>
    /// Reference server fixture
    /// </summary>
    public class ReferenceServer : BaseServerFixture
    {
        /// <summary>
        /// Sample server nodes
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="timeservice"></param>
        public static IEnumerable<INodeManagerFactory> Reference(
            ILoggerFactory? factory, TimeService timeservice)
        {
            yield return new TestData.TestDataServer();
            yield return new MemoryBuffer.MemoryBufferServer();
            yield return new Boiler.BoilerServer();
            yield return new Vehicles.VehiclesServer();
            yield return new Reference.ReferenceServer();
            yield return new HistoricalEvents.HistoricalEventsServer(timeservice);
            yield return new HistoricalAccess.HistoricalAccessServer(timeservice);
            yield return new Views.ViewsServer();
            yield return new DataAccess.DataAccessServer();
            yield return new Alarms.AlarmConditionServer(timeservice);
            yield return new SimpleEvents.SimpleEventsServer();
            yield return new Plc.PlcServer(timeservice,
                (factory ?? Log.ConsoleFactory()).CreateLogger<Plc.PlcServer>());
        }

        /// <inheritdoc/>
        public ReferenceServer()
            : base(Reference)
        {
        }

        /// <inheritdoc/>
        private ReferenceServer(ILoggerFactory loggerFactory,
            bool useReverseConnect)
            : base(Reference, loggerFactory, useReverseConnect)
        {
        }

        /// <inheritdoc/>
        public static ReferenceServer Create(ILoggerFactory loggerFactory,
            bool useReverseConnect = false)
        {
            return new ReferenceServer(loggerFactory, useReverseConnect);
        }
    }
}
