// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

extern alias Quickstarts;

namespace Azure.IIoT.OpcUa.Publisher.Stack.Sample
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using Opc.Ua.Test;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Adapts the authoritative OPC Foundation Quickstarts node-manager factories
    /// to the Publisher test-host factory contract.
    /// </summary>
    /// <remarks>
    /// The upstream implementations supersede the previously forked TestData,
    /// MemoryBuffer, Boiler, Alarms, and Reference cohorts. The adapter retains
    /// Publisher fixture composition while intentionally inheriting upstream
    /// protocol and address-space behavior.
    /// </remarks>
    public static class QuickstartsNodeManagerFactories
    {
        /// <summary>
        /// Creates the upstream TestData factory.
        /// </summary>
        public static INodeManagerFactory CreateTestData()
        {
            return new AsyncNodeManagerFactoryAdapter(
                new Quickstarts::TestData.TestDataNodeManagerFactory());
        }

        /// <summary>
        /// Creates the upstream MemoryBuffer factory.
        /// </summary>
        public static INodeManagerFactory CreateMemoryBuffer()
        {
            return new Quickstarts::MemoryBuffer.MemoryBufferNodeManagerFactory();
        }

        /// <summary>
        /// Creates the upstream Boiler factory.
        /// </summary>
        public static INodeManagerFactory CreateBoiler()
        {
            return new Quickstarts::Boiler.BoilerNodeManagerFactory();
        }

        /// <summary>
        /// Creates the upstream Alarms factory.
        /// </summary>
        public static INodeManagerFactory CreateAlarms()
        {
            return new AsyncNodeManagerFactoryAdapter(
                new Quickstarts::Alarms.AlarmNodeManagerFactory());
        }

        /// <summary>
        /// Creates the upstream Reference node-manager factory.
        /// </summary>
        public static INodeManagerFactory CreateReference()
        {
            return new AsyncNodeManagerFactoryAdapter(
                new ReferenceNodeManagerFactory());
        }

        /// <summary>
        /// Creates the standard upstream memory-buffer configuration used by the
        /// Publisher sample-server hosts.
        /// </summary>
        public static object CreateMemoryBufferConfiguration()
        {
            return new Quickstarts::MemoryBuffer.MemoryBufferConfiguration
            {
                Buffers =
                [
                    new Quickstarts::MemoryBuffer.MemoryBufferInstance
                    {
                        Name = "UInt32",
                        TagCount = 10000,
                        DataType = "UInt32"
                    },
                    new Quickstarts::MemoryBuffer.MemoryBufferInstance
                    {
                        Name = "Double",
                        TagCount = 100,
                        DataType = "Double"
                    }
                ]
            };
        }

        private sealed class ReferenceNodeManagerFactory : IAsyncNodeManagerFactory
        {
            /// <inheritdoc/>
            public ArrayOf<string> NamespacesUris =>
                [Quickstarts::Quickstarts.ReferenceServer.Namespaces.ReferenceServer];

            /// <inheritdoc/>
            public ValueTask<IAsyncNodeManager> CreateAsync(
                IServerInternal server,
                ApplicationConfiguration configuration,
                CancellationToken cancellationToken = default)
            {
                _ = cancellationToken;

                return new ValueTask<IAsyncNodeManager>(
                    new Quickstarts::Quickstarts.ReferenceServer.ReferenceNodeManager(
                        server,
                        configuration));
            }
        }
    }

    /// <summary>
    /// Marks an asynchronous node-manager factory for the synchronous Publisher
    /// fixture contract.
    /// </summary>
    /// <remarks>
    /// <see cref="ServerFactory"/> detects this marker and gives the created
    /// manager directly to <see cref="MasterNodeManager"/>, preserving the
    /// upstream asynchronous implementation rather than wrapping it in a
    /// synchronous node manager.
    /// </remarks>
    public sealed class AsyncNodeManagerFactoryAdapter : INodeManagerFactory
    {
        /// <summary>
        /// Initializes the adapter.
        /// </summary>
        /// <param name="factory">The upstream asynchronous factory.</param>
        public AsyncNodeManagerFactoryAdapter(IAsyncNodeManagerFactory factory)
        {
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// The upstream asynchronous factory.
        /// </summary>
        public IAsyncNodeManagerFactory Factory { get; }

        /// <inheritdoc/>
        public ArrayOf<string> NamespacesUris => Factory.NamespacesUris;

        /// <inheritdoc/>
        /// <remarks>
        /// The Publisher host recognizes this adapter and invokes
        /// <see cref="IAsyncNodeManagerFactory.CreateAsync"/> directly.
        /// </remarks>
        public INodeManager Create(IServerInternal server, ApplicationConfiguration configuration)
        {
            _ = server;
            _ = configuration;

            throw new InvalidOperationException(
                "Asynchronous Quickstarts factories must be composed through ServerFactory.");
        }
    }

    /// <summary>
    /// Provides the fixture clock to upstream Quickstarts implementations.
    /// </summary>
    public sealed class TimeServiceTimeProvider : TimeProvider
    {
        /// <summary>
        /// Initializes the provider.
        /// </summary>
        /// <param name="timeService">The fixture time service.</param>
        public TimeServiceTimeProvider(TimeService timeService)
        {
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
        }

        /// <inheritdoc/>
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(_timeService.UtcNow);
        }

        private readonly TimeService _timeService;
    }
}
