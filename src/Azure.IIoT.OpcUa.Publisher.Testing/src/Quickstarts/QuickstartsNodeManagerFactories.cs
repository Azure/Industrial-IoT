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
    using System.Collections.Generic;
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
                new AutoStartingAlarmNodeManagerFactory());
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

        /// <summary>
        /// Adds the standard upstream memory-buffer configuration.
        /// </summary>
        public static void AddMemoryBufferConfiguration(
            ApplicationConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            configuration.UpdateExtension(null,
                (Quickstarts::MemoryBuffer.MemoryBufferConfiguration)
                CreateMemoryBufferConfiguration());
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

        private sealed class AutoStartingAlarmNodeManagerFactory : IAsyncNodeManagerFactory
        {
            /// <inheritdoc/>
            public ArrayOf<string> NamespacesUris =>
            [
                Quickstarts::Alarms.Namespaces.Alarms,
                Quickstarts::Alarms.Namespaces.Alarms + "Instance"
            ];

            /// <inheritdoc/>
            public ValueTask<IAsyncNodeManager> CreateAsync(
                IServerInternal server,
                ApplicationConfiguration configuration,
                CancellationToken cancellationToken = default)
            {
                _ = cancellationToken;
                return new ValueTask<IAsyncNodeManager>(
                    new AutoStartingAlarmNodeManager(
                        server,
                        configuration,
                        [
                            Quickstarts::Alarms.Namespaces.Alarms,
                            Quickstarts::Alarms.Namespaces.Alarms + "Instance"
                        ]));
            }
        }

        private sealed class AutoStartingAlarmNodeManager :
            Quickstarts::Alarms.AlarmNodeManager
        {
            public AutoStartingAlarmNodeManager(
                IServerInternal server,
                ApplicationConfiguration configuration,
                string[] namespaceUris)
                : base(server, configuration, namespaceUris)
            {
            }

            /// <inheritdoc/>
            public override async ValueTask CreateAddressSpaceAsync(
                IDictionary<NodeId, IList<IReference>> externalReferences,
                CancellationToken cancellationToken = default)
            {
                await base.CreateAddressSpaceAsync(
                    externalReferences,
                    cancellationToken).ConfigureAwait(false);

                var result = OnStart(
                    SystemContext,
                    new MethodState(null)
                    {
                        NodeId = new NodeId("Alarms.Start", NamespaceIndex)
                    },
                    [new Variant(uint.MaxValue)],
                    []);
                if (ServiceResult.IsBad(result))
                {
                    throw new ServiceResultException(result.StatusCode);
                }
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

}
