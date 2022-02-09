// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime {
    using Microsoft.Azure.IIoT.Hub.Module.Client.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;

    /// <summary>
    /// Static class that contains the default keys for the legacy command line arguments how they will be represented in
    /// the IConfiguration-instance.
    /// </summary>
    public static class LegacyCliConfigKeys {
        /// <summary>
        /// Key for default published nodes file.
        /// </summary>
        public const string DefaultPublishedNodesFilename = "publishednodes.json";

        /// <summary>
        /// Key for default published nodes schema file.
        /// </summary>
        public const string DefaultPublishedNodesSchemaFilename = "";

        /// <summary>
        /// Key for the publisher site.
        /// </summary>
        public const string PublisherSite = "Site";

        /// <summary>
        /// Key for the specified published nodes filename.
        /// </summary>
        public const string PublishedNodesConfigurationFilename = "PublishedNodesFile";

        /// <summary>
        /// Key for the specified published nodes schema filename.
        /// </summary>
        public const string PublishedNodesConfigurationSchemaFilename = "PublishedNodesSchemaFile";

        /// <summary>
        /// Key for the default heartbeat interval in seconds.
        /// </summary>
        public const string HeartbeatIntervalDefault = "DefaultHeartbeatInterval";

        /// <summary>
        /// Key for the default flag to skip the first event
        /// </summary>
        public const string SkipFirstDefault = "DefaultSkipFirst";

        /// <summary>
        /// Key for the messaging mode
        /// </summary>
        public const string MessagingMode = "MessagingMode";

        /// <summary>
        /// Key for the messaging mode
        /// </summary>
        public const string MessageEncoding = "MessageEncoding";

        /// <summary>
        /// Flag to demand full featured message creation from publisher
        /// </summary>
        public const string FullFeaturedMessage = "FullFeaturedMessage";

        /// <summary>
        /// Key for the default sampling interval in milliseconds.
        /// </summary>
        public const string OpcSamplingInterval = "DefaultSamplingInterval";

        /// <summary>
        /// Key for the default publishing interval in milliseconds.
        /// </summary>
        public const string OpcPublishingInterval = "DefaultPublishingInterval";

        /// <summary>
        /// Key for the default queue size fro monitored items
        /// </summary>
        public const string DefaultQueueSize = "DefaultQueueSize";

        /// <summary>
        /// Key for the flag whether to fetch the display names of the nodes.
        /// </summary>
        public const string FetchOpcNodeDisplayName = "FetchOpcNodeDisplayName";

        /// <summary>
        /// Key for the diagnostics interval in seconds.
        /// </summary>
        public const string DiagnosticsInterval = "DiagnosticsInterval";

        /// <summary>
        /// Key for the batch size of the batching buffer
        /// </summary>
        public const string BatchSize = "BatchSize";

        /// <summary>
        /// Key for the batch size of the batching buffer
        /// </summary>
        public const string BatchTriggerInterval = "BatchTriggerInterval";

        /// <summary>
        /// Key for the max (IoT Hub D2C)message size
        /// </summary>
        public const string IoTHubMaxMessageSize = "IoTHubMaxMessageSize";

        /// <summary>
        /// Key for the max (IoT Hub D2C) messages
        /// </summary>
        public const string MaxOutgressMessages = "MaxOutgressMessages";

        /// <summary>
        /// Key for the scale test monitored items clones count .
        /// </summary>
        public const string ScaleTestCount = "ScaleTestCount";

        /// <summary>
        /// Key for the time for the logfile to flush to disc in seconds.
        /// </summary>
        public const string LogFileFlushTimeSpanSec = "LogFileFlushTimeSpan";

        /// <summary>
        /// Key for the log file name.
        /// </summary>
        public const string LogFileName = "LogFileName";

        /// <summary>
        /// Key for the transport mode to IoT Hub.
        /// </summary>
        public const string HubTransport = ModuleConfig.kTransportKey;

        /// <summary>
        /// Key for the EdgeHub connection string.
        /// </summary>
        public const string EdgeHubConnectionString = ModuleConfig.kEdgeHubConnectionStringKey;

        /// <summary>
        /// Key for the operation timeout in milliseconds.
        /// </summary>
        public const string OpcOperationTimeout = TransportQuotaConfig.OperationTimeoutKey;

        /// <summary>
        /// Key for the max string length.
        /// </summary>
        public const string OpcMaxStringLength = TransportQuotaConfig.MaxStringLengthKey;

        /// <summary>
        /// Key for the default OPC Session timeout in seconds - to request from the OPC server at session creation.
        /// </summary>
        public const string OpcSessionCreationTimeout = ClientServicesConfig.DefaultSessionTimeoutKey;

        /// <summary>
        /// Key for the OPC Keep Alive Interval in seconds.
        /// </summary>
        public const string OpcKeepAliveIntervalInSec = ClientServicesConfig.KeepAliveIntervalKey;

        /// <summary>
        /// Key for the disconnect thresholt for missed keep alive signals.
        /// </summary>
        public const string OpcKeepAliveDisconnectThreshold = ClientServicesConfig.MaxKeepAliveCountKey;

        /// <summary>
        /// Key for the flag to trust own certificate.
        /// </summary>
        public const string TrustMyself = SecurityConfig.AddAppCertToTrustedStoreKey;

        /// <summary>
        /// Key for the flat to auto-accept untrusted certificates.
        /// </summary>
        public const string AutoAcceptCerts = SecurityConfig.AutoAcceptUntrustedCertificatesKey;

        /// <summary>
        /// Key for the Application Certificate store type.
        /// </summary>
        public const string OpcOwnCertStoreType = SecurityConfig.ApplicationCertificateStoreTypeKey;

        /// <summary>
        /// Key for the Aplication Certificate Store path.
        /// </summary>
        public const string OpcOwnCertStorePath = SecurityConfig.ApplicationCertificateStorePathKey;

        /// <summary>
        /// Key for app cert subject name.
        /// </summary>
        public const string OpcApplicationCertificateSubjectName = SecurityConfig.ApplicationCertificateSubjectNameKey;

        /// <summary>
        /// Key for app name.
        /// </summary>
        public const string OpcApplicationName = SecurityConfig.ApplicationNameKey;

        /// <summary>
        /// Key for the trusted peer certificates path.
        /// </summary>
        public const string OpcTrustedCertStorePath = SecurityConfig.TrustedPeerCertificatesPathKey;

        /// <summary>
        /// Key for the rejected certificate store path.
        /// </summary>
        public const string OpcRejectedCertStorePath = SecurityConfig.RejectedCertificateStorePathKey;

        /// <summary>
        /// Key for the trusted issuer certificates.
        /// </summary>
        public const string OpcIssuerCertStorePath = SecurityConfig.TrustedIssuerCertificatesPathKey;
    }
}
