// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Fixtures
{
    using global::Counter;
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Server;
    using Opc.Ua.Test;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Hosts the deterministic <see cref="global::Counter.CounterServer"/>
    /// whose variables all count up from zero in lockstep. Used by the long
    /// running telemetry quality tests to detect lost, reordered and stale
    /// values.
    /// </summary>
    public class CounterServer : BaseServerFixture
    {
        /// <summary>
        /// The node manager backing the counter variables. Exposes the
        /// highest value the server has produced so far.
        /// </summary>
        public CounterNodeManager NodeManager => _factory.NodeManager;

        /// <summary>
        /// Options the server was created with
        /// </summary>
        public CounterServerOptions Options { get; }

        /// <summary>
        /// Node id of the counter variable with the given index
        /// </summary>
        /// <param name="index"></param>
        public static string GetNodeId(int index)
        {
            return CounterNodeManager.GetNodeId(index);
        }

        /// <summary>
        /// Default fixture instance used by xUnit IClassFixture.
        /// </summary>
        public CounterServer()
            : this(new CounterServerOptions())
        {
        }

        /// <summary>
        /// Create fixture with explicit options
        /// </summary>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        public CounterServer(CounterServerOptions options,
            ILoggerFactory? loggerFactory = null)
            : this(new global::Counter.CounterServer(options), options, loggerFactory)
        {
        }

        /// <summary>
        /// Create fixture. The factory instance has to exist before the base
        /// constructor runs because the base constructor is what asks it to
        /// create the node manager.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        private CounterServer(global::Counter.CounterServer factory,
            CounterServerOptions options, ILoggerFactory? loggerFactory)
            : base((_, _) => Nodes(factory), loggerFactory)
        {
            _factory = factory;
            Options = options;
        }

        /// <summary>
        /// Counter server nodes
        /// </summary>
        /// <param name="factory"></param>
        private static IEnumerable<INodeManagerFactory> Nodes(
            global::Counter.CounterServer factory)
        {
            yield return factory;
        }

        /// <summary>
        /// Create a counter server with the given number of variables that
        /// increments every <paramref name="updateInterval"/>.
        /// </summary>
        /// <param name="nodeCount"></param>
        /// <param name="updateInterval"></param>
        /// <param name="loggerFactory"></param>
        public static CounterServer Create(int nodeCount, TimeSpan updateInterval,
            ILoggerFactory? loggerFactory = null)
        {
            return new CounterServer(new CounterServerOptions
            {
                NodeCount = nodeCount,
                UpdateInterval = updateInterval
            }, loggerFactory);
        }

        private readonly global::Counter.CounterServer _factory;
    }
}
