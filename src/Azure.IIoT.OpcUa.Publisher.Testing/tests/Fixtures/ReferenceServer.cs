// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Fixtures
{
    using Azure.IIoT.OpcUa.Core.Logging;
    using Azure.IIoT.OpcUa.Publisher.Stack.Sample;
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
            _ = timeservice;
            yield return QuickstartsNodeManagerFactories.CreateTestData();
            yield return QuickstartsNodeManagerFactories.CreateMemoryBuffer();
            yield return QuickstartsNodeManagerFactories.CreateBoiler();
            yield return new Vehicles.VehiclesServer();
            yield return QuickstartsNodeManagerFactories.CreateReference();
            yield return new HistoricalEvents.HistoricalEventsServer(timeservice);
            yield return new HistoricalAccess.HistoricalAccessServer(timeservice);
            yield return new Views.ViewsServer();
            yield return new DataAccess.DataAccessServer();
            yield return QuickstartsNodeManagerFactories.CreateAlarms();
            yield return new SimpleEvents.SimpleEventsServer();
            yield return new Plc.PlcServer(timeservice,
                (factory ?? Log.ConsoleFactory()).CreateLogger<Plc.PlcServer>(), 0);
        }

        /// <summary>
        /// Default fixture instance used by xUnit IClassFixture.
        /// </summary>
        public ReferenceServer()
            : this(useReverseConnect: false)
        {
        }

        /// <inheritdoc/>
        private ReferenceServer(bool useReverseConnect)
            : base(Reference, null, useReverseConnect)
        {
        }

        /// <inheritdoc/>
        private ReferenceServer(ILoggerFactory loggerFactory,
            bool useReverseConnect)
            : base(Reference, loggerFactory, useReverseConnect)
        {
        }

        /// <summary>
        /// Create a reference server with reverse connect enabled.
        /// </summary>
        public static ReferenceServer WithReverseConnect()
        {
            return new ReferenceServer(useReverseConnect: true);
        }

        /// <inheritdoc/>
        public static ReferenceServer Create(ILoggerFactory loggerFactory,
            bool useReverseConnect = false)
        {
            return new ReferenceServer(loggerFactory, useReverseConnect);
        }
    }
}
