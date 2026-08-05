// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Contains constants using for End 2 End testing
    /// </summary>
    internal static partial class TestConstants
    {
        /// <summary>
        /// Character that need to be used when split value of "PLC_SIMULATION_XYZ" environment variables
        /// </summary>
        public static char SimulationUrlsSeparator = ';';

        /// <summary>
        /// Name of the test assembly
        /// </summary>
        public const string TestAssemblyName = "OpcPublisher-AE-E2E-Tests";

        /// <summary>
        /// Default timeout of web calls
        /// </summary>
        public const int DefaultTimeoutInMilliseconds = 90 * 1000;

        /// <summary>
        /// Default delay interval in milliseconds
        /// </summary>
        public const int DefaultDelayMilliseconds = 5 * 1000;

        /// <summary>
        /// Maximum timeout for a test case
        /// </summary>
        public const int MaxTestTimeoutMilliseconds = 10 * 60 * 1000;

        /// <summary>
        /// Name of Published Nodes Json used by publisher module
        /// </summary>
        public const string PublishedNodesFilename = "published_nodes.json";

        /// <summary>
        /// Folder to store published_nodes.json file
        /// </summary>
        public const string PublishedNodesFolder = "/mount/opc_publisher";

        /// <summary>
        /// The full name of the publishednodes.json on the Edge
        /// </summary>
        public static readonly string PublishedNodesFullName =
            PublishedNodesFolder.TrimEnd('/') + "/" + PublishedNodesFilename;

        /// <summary>
        /// Default Microsoft Container Registry
        /// </summary>
        public const string MicrosoftContainerRegistry = "mcr.microsoft.com";

        /// <summary>
        /// IoT Hub Event Hubs endpoint consumer group for tests
        /// </summary>
        public const string TestConsumerGroupName = "TestConsumer";

        /// <summary>
        /// <para>
        /// Dedicated IoT Hub Event Hubs consumer groups for the long running
        /// telemetry quality tests.
        /// </para>
        /// <para>
        /// They run in parallel with the A&amp;E job and with each other, and
        /// each of them keeps a reader open on every partition for the whole
        /// observation window. The built-in endpoint permits five concurrent
        /// readers per partition and per consumer group, so giving each soak
        /// its own group keeps them clear of that budget and of each other.
        /// </para>
        /// </summary>
        public const string SoakCountersConsumerGroupName = "SoakCounters";

        /// <inheritdoc cref="SoakCountersConsumerGroupName"/>
        public const string SoakHeartbeatConsumerGroupName = "SoakHeartbeat";

        /// <summary>
        /// Contains constants for the long running telemetry quality tests.
        /// </summary>
        internal static class Soak
        {
            /// <summary>
            /// How long telemetry is observed, in minutes, after the warm up.
            /// </summary>
            public const string DurationMinutesVariable = "IIOT_E2E_SOAK_MINUTES";

            /// <summary>
            /// Number of counter nodes to publish.
            /// </summary>
            public const string NodeCountVariable = "IIOT_E2E_SOAK_NODES";

            /// <summary>
            /// Default observation window.
            /// </summary>
            public const int DefaultDurationMinutes = 30;

            /// <summary>
            /// Default number of fast counter nodes. Bounded by the IoT Hub
            /// S1 daily message quota, which is shared with the A&amp;E job,
            /// and by the two vCPU IoT Edge VM. Scale beyond this is covered
            /// by the in-process soak, not by the end to end test.
            /// </summary>
            public const int DefaultFastNodeCount = 100;

            /// <summary>
            /// Number of slow counter nodes used by the heartbeat scenario.
            /// </summary>
            public const int SlowNodeCount = 20;

            /// <summary>
            /// Interval at which the fast counters increment.
            /// </summary>
            public static readonly TimeSpan FastUpdateInterval = TimeSpan.FromSeconds(2);

            /// <summary>
            /// Interval at which the slow counters increment.
            /// </summary>
            public static readonly TimeSpan SlowUpdateInterval = TimeSpan.FromMinutes(2);

            /// <summary>
            /// Publishing and sampling interval used by both scenarios.
            /// </summary>
            public static readonly TimeSpan PublishingInterval = TimeSpan.FromSeconds(2);

            /// <summary>
            /// Heartbeat interval for the counter scenario. Equal to the
            /// publishing interval, which is exactly the configuration that
            /// used to make the watchdog race the value it waits for.
            /// </summary>
            public static readonly TimeSpan FastHeartbeatInterval = TimeSpan.FromSeconds(2);

            /// <summary>
            /// Heartbeat interval for the slow scenario. Chosen so that
            /// roughly eleven heartbeats fall between two value changes
            /// without multiplying the IoT Hub message volume.
            /// </summary>
            public static readonly TimeSpan SlowHeartbeatInterval = TimeSpan.FromSeconds(10);

            /// <summary>
            /// Queue size configured on every monitored item.
            /// </summary>
            public const uint QueueSize = 10;

            /// <summary>
            /// Node id of the n-th fast counter exposed by OPC PLC. The value
            /// increments by exactly one every <c>--fr</c> seconds.
            /// </summary>
            /// <param name="index">One based node index</param>
            public static string FastNodeId(int index)
            {
                return string.Create(CultureInfo.InvariantCulture,
                    $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{index}");
            }

            /// <summary>
            /// Node id of the n-th slow counter exposed by OPC PLC.
            /// </summary>
            /// <param name="index">One based node index</param>
            public static string SlowNodeId(int index)
            {
                return string.Create(CultureInfo.InvariantCulture,
                    $"nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt{index}");
            }

            /// <summary>
            /// Observation window, overridable through
            /// <see cref="DurationMinutesVariable"/>.
            /// </summary>
            public static TimeSpan Duration
                => TimeSpan.FromMinutes(GetPositiveInt(DurationMinutesVariable,
                    DefaultDurationMinutes));

            /// <summary>
            /// Number of fast counter nodes, overridable through
            /// <see cref="NodeCountVariable"/>.
            /// </summary>
            public static int FastNodeCount
                => GetPositiveInt(NodeCountVariable, DefaultFastNodeCount);

            private static int GetPositiveInt(string variable, int fallback)
            {
                return int.TryParse(Environment.GetEnvironmentVariable(variable),
                    CultureInfo.InvariantCulture, out var value) && value > 0
                        ? value : fallback;
            }
        }

        /// <summary>
        /// Contains constants for OPC PLC
        /// </summary>
        internal static class OpcSimulation
        {
            /// <summary>
            /// Default port of OPC UA Server endpoint of OPC PLC
            /// </summary>
            public const ushort Port = 50000;

            /// <summary>
            /// Name of Published Nodes Json file generated by OPC PLC, containing information
            /// of provided (simulated) OPC UA Nodes
            /// </summary>
            public const string PublishedNodesFile = "pn.json";

            /// <summary>
            /// Name of Tag in Resource Group
            /// </summary>
            public const string TestingResourcesSuffixName = "TestingResourcesSuffix";
        }

        /// <summary>
        /// Contains names of Environment variables available for tests
        /// </summary>
        internal static class EnvironmentVariablesNames
        {
            /// <summary>
            /// Tenant name used for authentication of Industrial IoT Platform
            /// </summary>
            public const string PCS_AUTH_TENANT = "PCS_AUTH_TENANT";

            /// <summary>
            /// Client App ID used for authentication of Industrial IoT Platform
            /// </summary>
            public const string PCS_AUTH_CLIENT_APPID = "PCS_AUTH_CLIENT_APPID";

            /// <summary>
            /// Client Secrete used for authentication of Industrial IoT Platform
            /// </summary>
            public const string PCS_AUTH_CLIENT_SECRET = "PCS_AUTH_CLIENT_SECRET";

            /// <summary>
            /// Semicolon separated URLs to load published_nodes.json from OPC-PLCs
            /// </summary>
            public const string PLC_SIMULATION_URLS = "PLC_SIMULATION_URLS";

            /// <summary>
            /// Semicolon separated ip addresses of OPC Plcs
            /// </summary>
            public const string PLC_SIMULATION_IPS = "PLC_SIMULATION_IPS";

            /// <summary>
            /// IoTEdge version
            /// </summary>
            public const string IOT_EDGE_VERSION = "IOT_EDGE_VERSION";

            /// <summary>
            /// Device identity of edge device at IoT Hub
            /// </summary>
            public const string IOT_EDGE_DEVICE_ID = "IOT_EDGE_DEVICE_ID";

            /// <summary>
            /// DNS name of edge device
            /// </summary>
            public const string IOT_EDGE_DEVICE_DNSNAME = "IOT_EDGE_DEVICE_DNSNAME";

            /// <summary>
            /// User name of vm that hosting edge device
            /// </summary>
            public const string IOT_EDGE_VM_USERNAME = "IOT_EDGE_VM_USERNAME";

            /// <summary>
            /// SSH public key of vm that hosting edge device
            /// </summary>
            public const string IOT_EDGE_VM_PUBLICKEY = "IOT_EDGE_VM_PUBLICKEY";

            /// <summary>
            /// SSH private key of vm that hosting edge device
            /// </summary>
            public const string IOT_EDGE_VM_PRIVATEKEY = "IOT_EDGE_VM_PRIVATEKEY";

            /// <summary>
            /// IoT Hub connection string
            /// </summary>
            public const string PCS_IOTHUB_CONNSTRING = "PCS_IOTHUB_CONNSTRING";

            /// <summary>
            /// The connection string of the event-hub compatible endpoint of IoT Hub.
            /// Deprecated: tests now use IOTHUB_EVENTHUB_NAMESPACE + IOTHUB_EVENTHUB_NAME with
            /// AAD (TokenCredential) instead of the SAS-key-embedded connection string. This
            /// env var is still set by the deployment pipeline for backward compatibility
            /// and may be removed in a future iteration.
            /// </summary>
            public const string IOTHUB_EVENTHUB_CONNECTIONSTRING = "IOTHUB_EVENTHUB_CONNECTIONSTRING";

            /// <summary>
            /// Fully-qualified namespace of the IoT Hub's built-in Event Hub-compatible
            /// endpoint (e.g. "iothub-ns-myhub-12345-abc123.servicebus.windows.net"). Used
            /// with AAD auth.
            /// </summary>
            public const string IOTHUB_EVENTHUB_NAMESPACE = "IOTHUB_EVENTHUB_NAMESPACE";

            /// <summary>
            /// Event Hub "entity path" for the IoT Hub's built-in endpoint. Equals the IoT
            /// Hub name. Used with AAD auth.
            /// </summary>
            public const string IOTHUB_EVENTHUB_NAME = "IOTHUB_EVENTHUB_NAME";

            /// <summary>
            /// Container Registry server
            /// </summary>
            public const string PCS_DOCKER_SERVER = "PCS_DOCKER_SERVER";

            /// <summary>
            /// Container Registry user name
            /// </summary>
            public const string PCS_DOCKER_USER = "PCS_DOCKER_USER";

            /// <summary>
            /// Container Registry password
            /// </summary>
            public const string PCS_DOCKER_PASSWORD = "PCS_DOCKER_PASSWORD";

            /// <summary>
            ///Images namespace
            /// </summary>
            public const string PCS_IMAGES_NAMESPACE = "PCS_IMAGES_NAMESPACE";

            /// <summary>
            /// Images tag
            /// </summary>
            public const string PCS_IMAGES_TAG = "PCS_IMAGES_TAG";

            /// <summary>
            /// Resource group
            /// </summary>
            public const string PCS_RESOURCE_GROUP = "PCS_RESOURCE_GROUP";

            /// <summary>
            /// Subscription Id
            /// </summary>
            public const string PCS_SUBSCRIPTION_ID = "PCS_SUBSCRIPTION_ID";
        }

        /// <summary>
        /// Constants related to xUnit traits
        /// </summary>
        internal static class TraitConstants
        {
            /// <summary>
            /// The trait name of the Publisher Mode
            /// </summary>
            public const string PublisherModeTraitName = "PublisherMode";

            /// <summary>
            /// The trait value for PublisherMode = AE
            /// </summary>
            public const string PublisherModeTraitValue = "AE";

            /// <summary>
            /// The trait value for PublisherMode = standalone
            /// </summary>
            public const string PublisherModeStandaloneTraitValue = "standaloneX";

            /// <summary>
            /// <para>
            /// The trait value for the long running counter telemetry quality
            /// test. It gets its own value so that the soak runs in its own
            /// <c>dotnet test</c> process, and therefore in parallel with the
            /// A&amp;E job, rather than being serialized behind it by the
            /// <c>parallelizeTestCollections: false</c> runner setting.
            /// </para>
            /// </summary>
            public const string PublisherModeSoakCountersTraitValue = "soakcounters";

            /// <summary>
            /// The trait value for the long running heartbeat telemetry
            /// quality test.
            /// </summary>
            public const string PublisherModeSoakHeartbeatTraitValue = "soakheartbeat";
        }

        /// <summary>
        /// Direct Method names
        /// </summary>
        internal static class DirectMethodNames
        {
            /// <summary>
            /// Publish Nodes
            /// </summary>
            public const string PublishNodes = "PublishNodes_V1";

            /// <summary>
            /// Unpublish Nodes
            /// </summary>
            public const string UnpublishNodes = "UnpublishNodes_V1";

            /// <summary>
            /// GetConfiguredNodesOnEndpoint
            /// </summary>
            public const string GetConfiguredNodesOnEndpoint = "GetConfiguredNodesOnEndpoint_V1";

            /// <summary>
            /// GetConfiguredEndpoints
            /// </summary>
            public const string GetConfiguredEndpoints = "GetConfiguredEndpoints_V1";

            /// <summary>
            /// UnpublishAllNodes
            /// </summary>
            public const string UnpublishAllNodes = "UnpublishAllNodes_V1";

            /// <summary>
            /// GetDiagnosticInfo
            /// </summary>
            public const string GetDiagnosticInfo = "GetDiagnosticInfo_V1";

            /// <summary>
            /// AddOrUpdateEndpoints
            /// </summary>
            public const string AddOrUpdateEndpoints = "AddOrUpdateEndpoints_V1";
        }
    }
}
