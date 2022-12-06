// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestExtensions {
    using Config;
    using Microsoft.Extensions.Configuration;
    using System;
    using Xunit.Abstractions;

    /// <summary>
    /// Context to pass data between test cases
    /// </summary>
    public class IIoTPlatformTestContext : IDisposable, IDeviceConfig, IIoTHubConfig,
		IIoTEdgeConfig, IIIoTPlatformConfig, ISshConfig, IOpcPlcConfig, ITestEventProcessorConfig, IContainerRegistryConfig {

        /// <summary>
        /// Configuration
        /// </summary>
        private IConfiguration Configuration { get; }

        public IIoTPlatformTestContext() {
            Configuration = GetConfiguration();
            RegistryHelper = new RegistryHelper(this);
            OutputHelper = null;
        }

        /// <summary>
        /// Save the identifier of OPC server endpoints
        /// </summary>
        public string OpcUaEndpointId { get; set; }

        /// <summary>
        /// Save the identfier of the opc ua application
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// Folder path where PublishedNodes file is saved during the test
        /// </summary>
        public string PublishedNodesFileInternalFolder { get; set; }

        /// <summary>
        /// Helper to write output, need to be set from constructor of test class
        /// </summary>
        public ITestOutputHelper OutputHelper { get; set; }

        /// <summary>
        /// Gets or sets the OPC server url
        /// </summary>
        public string OpcServerUrl { get; set; }

        /// <summary>
        /// IoT Device Configuration
        /// </summary>
        public IDeviceConfig DeviceConfig { get { return this; } }

        /// <summary>
        /// IoT Hub Configuration
        /// </summary>
        public IIoTHubConfig IoTHubConfig { get { return this; } }

        /// <summary>
        /// IoT Edge Configuration
        /// </summary>
        public IIoTEdgeConfig IoTEdgeConfig { get { return this; } }

        /// <summary>
        /// IoT Hub Configuration
        /// </summary>
        public IIIoTPlatformConfig IIoTPlatformConfigHubConfig { get { return this; } }

        /// <summary>
        /// SSH Configuration
        /// </summary>
        public ISshConfig SshConfig { get { return this; } }

        /// <summary>
        /// OpcPlc Configuration
        /// </summary>
        public IOpcPlcConfig OpcPlcConfig { get { return this; } }

        /// <summary>
        /// TestEventProcessor configuration
        /// </summary>
        public ITestEventProcessorConfig TestEventProcessorConfig { get { return this; } }

        /// <summary>
        /// ContainerRegistry Configuration
        /// </summary>
        public IContainerRegistryConfig ContainerRegistryConfig { get { return this; } }

        /// <summary>
        /// Helper to work with Azure.Devices.RegistryManager
        /// </summary>
        public RegistryHelper RegistryHelper { get; }

        /// <inheritdoc />
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Override for disposing
        /// </summary>
        /// <param name="disposing">Indicates if called from <see cref="Dispose"/></param>
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                RegistryHelper.Dispose();
            }
        }

        /// <summary>
        /// Read configuration variable
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private string GetStringOrDefault(string key, Func<string> defaultValue) {
            var value = Configuration.GetValue<string>(key);
            if (string.IsNullOrEmpty(value)) {
                return defaultValue?.Invoke() ?? string.Empty;
            }
            return value.Trim();
        }

        /// <summary>
        /// Get configuration that reads from:
        ///     - environment variables
        ///     - environment variables from user target
        ///     - environment variables from .env file
        /// </summary>
        /// <returns></returns>
        private static IConfigurationRoot GetConfiguration() {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromDotEnvFile()
                .Build();

            return configuration;
        }

        string IDeviceConfig.DeviceId => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOT_EDGE_DEVICE_ID,
            () => throw new Exception("IoT Edge device id is not provided."));

        string IIoTHubConfig.IoTHubConnectionString => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_IOTHUB_CONNSTRING,
            () => throw new Exception("IoT Hub connection string is not provided."));

        string IIoTHubConfig.IoTHubEventHubConnectionString => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOTHUB_EVENTHUB_CONNECTIONSTRING,
            () => throw new Exception("IoT Hub EventHub connection string is not provided."));

        string IIoTHubConfig.CheckpointStorageConnectionString => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.STORAGEACCOUNT_IOTHUBCHECKPOINT_CONNECTIONSTRING,
        () => throw new Exception("IoT Hub Checkpoint Storage connection string is not provided."));

        string IIoTEdgeConfig.EdgeVersion => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOT_EDGE_VERSION,
            () => "1.4");

        string IIoTEdgeConfig.NestedEdgeFlag => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.NESTED_EDGE_FLAG,
            () => "Disable");

        string[] IIoTEdgeConfig.NestedEdgeSshConnections => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.NESTED_EDGE_SSH_CONNECTIONS,
            () => "").Split(",");

        string IIIoTPlatformConfig.BaseUrl => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_SERVICE_URL,
            () => { return string.Empty; });

        string IIIoTPlatformConfig.AuthTenant => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_AUTH_TENANT,
            () => { return string.Empty; });

        string IIIoTPlatformConfig.AuthClientId => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_AUTH_CLIENT_APPID,
            () => { return string.Empty; });

        string IIIoTPlatformConfig.AuthClientSecret => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_AUTH_CLIENT_SECRET,
            () => { return string.Empty; });

        string IIIoTPlatformConfig.ApplicationName => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.ApplicationName,
            () => throw new Exception("ApplicationName is not provided."));

        string ISshConfig.Username => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOT_EDGE_VM_USERNAME,
            () => throw new Exception("Username of iot edge device is not provided."));

        string ISshConfig.PublicKey => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOT_EDGE_VM_PUBLICKEY,
            () => throw new Exception("Public key of iot edge device is not provided."));

        string ISshConfig.PrivateKey => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOT_EDGE_VM_PRIVATEKEY,
            () => throw new Exception("Private key of iot edge device is not provided."));

        string ISshConfig.Host => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOT_EDGE_DEVICE_DNSNAME,
            () => throw new Exception("DNS name of iot edge device is not provided."));

        string IOpcPlcConfig.Urls => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PLC_SIMULATION_URLS,
            () => throw new Exception("Semicolon separated list of URLs of OPC-PLCs is not provided."));

        string ITestEventProcessorConfig.TestEventProcessorBaseUrl => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.TESTEVENTPROCESSOR_BASEURL,
            () => throw new Exception("Test Event Processor BaseUrl is not provided."));

        string ITestEventProcessorConfig.TestEventProcessorUsername => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.TESTEVENTPROCESSOR_USERNAME,
            () => throw new Exception("Test Event Processor Username is not provided."));

        string ITestEventProcessorConfig.TestEventProcessorPassword => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.TESTEVENTPROCESSOR_PASSWORD,
            () => throw new Exception("Test Event Processor Password is not provided."));

        string IContainerRegistryConfig.ContainerRegistryServer => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_DOCKER_SERVER,
            () => string.Empty);

        string IContainerRegistryConfig.ContainerRegistryUser => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_DOCKER_USER,
            () => string.Empty);

        string IContainerRegistryConfig.ContainerRegistryPassword => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_DOCKER_PASSWORD,
            () => string.Empty);

        string IContainerRegistryConfig.ImagesNamespace => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_IMAGES_NAMESPACE,
            () => string.Empty);

        string IContainerRegistryConfig.ImagesTag => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_IMAGES_TAG,
            () => "latest");
    }
}
