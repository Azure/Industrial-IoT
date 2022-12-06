// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests {

    /// <summary>
    /// Contains constants using for End 2 End testing
    /// </summary>
    internal static class TestConstants {
        /// <summary>
        /// Character that need to be used when split value of "PLC_SIMULATION_URLS"
        /// </summary>
        public static char SimulationUrlsSeparator = ';';

        /// <summary>
        /// Name of the test assembly
        /// </summary>
        public const string TestAssemblyName = "IIoTPlatform-E2E-Tests";

        /// <summary>
        /// Await time for initialization/setup or no data expected.
        /// </summary>
        public const int AwaitInitInMilliseconds = 30 * 1000;

        /// <summary>
        /// Await time for waiting new data
        /// </summary>
        public const int AwaitDataInMilliseconds = 90 * 1000;

        /// <summary>
        /// Await time for cleanup or no data expected.
        /// </summary>
        public const int AwaitCleanupInMilliseconds = 20 * 1000;

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
        public const int MaxTestTimeoutMilliseconds = 25 * 60 * 1000;

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
        public static readonly string MicrosoftContainerRegistry = "mcr.microsoft.com";

        /// <summary>
        /// Contains constants for all routes to Industrial IoT Platform
        /// </summary>
        internal static class APIRoutes {
            /// <summary>
            /// Route to enable an OPC UA endpoint
            /// </summary>
            public static string RegistryActivateEndpointsFormat = "/registry/v2/endpoints/{0}/activate";

            /// <summary>
            /// Route to enable an OPC UA endpoint
            /// </summary>
            public static string RegistryDeactivateEndpointsFormat = "/registry/v2/endpoints/{0}/deactivate";

            /// <summary>
            /// Route to applications within registry
            /// </summary>
            public const string RegistryApplications = "/registry/v2/applications";

            /// <summary>
            /// Route to applications within registry with application ID
            /// </summary>
            public const string RegistryApplicationsWithApplicationIdFormat = "/registry/v2/applications/{0}";

            /// <summary>
            /// Route for discovery within registry
            /// </summary>
            public const string RegistryDiscover = "/registry/v2/applications/discover";

            /// <summary>
            /// Route to endpoints within registry
            /// </summary>
            public const string RegistryEndpoints = "/registry/v2/endpoints";

            /// <summary>
            /// Route to registry health endpoint
            /// </summary>
            public const string RegistryHealth = "/registry/healthz";

            /// <summary>
            /// Route to publish single OPC UA node from OPC Publisher
            /// </summary>
            public static string PublisherStartFormat = "/publisher/v2/publish/{0}/start";

            /// <summary>
            /// Route to start or stop publishing multiple OPC UA nodes from OPC Publisher
            /// </summary>
            public static string PublisherBulkFormat = "/publisher/v2/publish/{0}/bulk";

            /// <summary>
            /// Route to read all publishing jobs
            /// </summary>
            public static string PublisherJobs = "/publisher/v2/jobs";

            /// <summary>
            /// Route to read single publishing jobs
            /// </summary>
            public static string PublisherJobsFormat = "/publisher/v2/jobs/{0}";

            /// <summary>
            /// Route to publisher health endpoint
            /// </summary>
            public const string PublisherHealth = "/publisher/healthz";

            /// <summary>
            /// Route to twin health endpoint
            /// </summary>
            public const string TwinHealth = "/twin/healthz";

            /// <summary>
            /// Route to job orchestrator health endpoint
            /// </summary>
            public const string JobOrchestratorHealth = "/edge/publisher/healthz";
        }

        /// <summary>
        /// Contains constants for all HTTP headers
        /// </summary>
        internal static class HttpHeaderNames {
            /// <summary>
            /// Name of header used for authentication
            /// </summary>
            public const string Authorization = "Authorization";
        }

        /// <summary>
        /// Contains constants for OPC PLC
        /// </summary>
        internal static class OpcSimulation {
            /// <summary>
            /// Default port of OPC UA Server endpoint of OPC PLC
            /// </summary>
            public const ushort Port = 50000;

            /// <summary>
            /// Name of Published Nodes Json file generated by OPC PLC, containing information
            /// of provided (simulated) OPC UA Nodes
            /// </summary>
            public const string PublishedNodesFile = "pn.json";
        }

        /// <summary>
        /// Contains names of Environment variables available for tests
        /// </summary>
        internal static class EnvironmentVariablesNames {
            /// <summary>
            /// Base URL of Industrial IoT Platform
            /// </summary>
            public const string PCS_SERVICE_URL = "PCS_SERVICE_URL";

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
            /// Name of Industrial IoT Platform instance
            /// </summary>
            public const string ApplicationName = "ApplicationName";

            /// <summary>
            /// Semicolon separated URLs to load published_nodes.json from OPC-PLCs
            /// </summary>
            public const string PLC_SIMULATION_URLS = "PLC_SIMULATION_URLS";

            /// <summary>
            /// IoTEdge version
            /// </summary>
            public const string IOT_EDGE_VERSION = "IOT_EDGE_VERSION";

            /// <summary>
            /// Nested edge flag
            /// </summary>
            public const string NESTED_EDGE_FLAG = "NESTED_EDGE_FLAG";

            /// <summary>
            /// Nested edge ssh connections
            /// </summary>
            public const string NESTED_EDGE_SSH_CONNECTIONS = "NESTED_EDGE_SSH_CONNECTIONS";

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
            /// </summary>
            public const string IOTHUB_EVENTHUB_CONNECTIONSTRING = "IOTHUB_EVENTHUB_CONNECTIONSTRING";

            /// <summary>
            /// The connection string of the storage account that will be used for checkpointing (when monitoring IoT Hub)
            /// </summary>
            public const string STORAGEACCOUNT_IOTHUBCHECKPOINT_CONNECTIONSTRING = "STORAGEACCOUNT_IOTHUBCHECKPOINT_CONNECTIONSTRING";

            /// <summary>
            /// The service base url of the TestEventProcessor
            /// </summary>
            public const string TESTEVENTPROCESSOR_BASEURL = "TESTEVENTPROCESSOR_BASEURL";

            /// <summary>
            /// The username to authenticate against the TestEventProcessor (Basic Auth)
            /// </summary>
            public const string TESTEVENTPROCESSOR_USERNAME = "TESTEVENTPROCESSOR_USERNAME";

            /// <summary>
            /// The password to authenticate against the TestEventProcessor (Basic Auth)
            /// </summary>
            public const string TESTEVENTPROCESSOR_PASSWORD = "TESTEVENTPROCESSOR_PASSWORD";

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
        }

        /// <summary>
        /// Constants related to xUnit traits
        /// </summary>
        internal static class TraitConstants {
            /// <summary>
            /// The trait name of the Publisher Mode
            /// </summary>
            public const string PublisherModeTraitName = "PublisherMode";

            /// <summary>
            /// The trait value for PublisherMode = orchestrated
            /// </summary>
            public const string PublisherModeOrchestratedTraitValue = "orchestrated";

            /// <summary>
            /// The trait value for PublisherMode = standalone
            /// </summary>
            public const string PublisherModeTraitValue = "standalone";

            /// <summary>
            /// The trait name of the Twin Mode
            /// </summary>
            public const string TwinModeTraitName = "TwinMode";

            /// <summary>
            /// The trait name of the Discovery Mode
            /// </summary>
            public const string DiscoveryModeTraitName = "DiscoveryMode";

            /// <summary>
            /// The trait value for default trait
            /// </summary>
            public const string DefaultTraitValue = "default";
        }

        /// <summary>
        /// Constants related to state returned in result
        /// </summary>
        internal static class StateConstants {
            /// <summary>
            /// Endpoint activation state activated and connected
            /// </summary>
            public const string ActivatedAndConnected = "ActivatedAndConnected";

            /// <summary>
            /// Endpoint state ready
            /// </summary>
            public const string Ready = "Ready";

            /// <summary>
            /// Endpoint activation state ready
            /// </summary>
            public const string Deactivated = "Deactivated";

            /// <summary>
            /// Endpoint state Disconnected
            /// </summary>
            public const string Disconnected = "Disconnected";
        }

        /// <summary>
        /// Direct Method names
        /// </summary>
        internal static class DirectMethodNames {
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
