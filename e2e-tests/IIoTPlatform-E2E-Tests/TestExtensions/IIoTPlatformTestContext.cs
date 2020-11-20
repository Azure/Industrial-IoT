// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable
namespace IIoTPlatform_E2E_Tests.TestExtensions {
    using System;
    using Config;
    using Extensions;
    using Microsoft.Extensions.Configuration;
    using Xunit.Abstractions;

    /// <summary>
    /// Context to pass data between test cases
    /// </summary>
    public class IIoTPlatformTestContext : IDisposable, IDeviceConfig, IIoTHubConfig, IIIoTPlatformConfig, ISshConfig, IOpcPlcConfig, IContainerRegistryConfig {

        /// <summary>
        /// Configuration
        /// </summary>
        private IConfiguration Configuration { get; }

        public IIoTPlatformTestContext() {
            Configuration = GetConfiguration();

            RegistryHelper = new RegistryHelper(IoTHubConfig);
            OutputHelper = null;
        }

        /// <summary>
        /// Save the identifier of OPC server endpoints
        /// </summary>
        public string? OpcUaEndpointId { get; set; }

        /// <summary>
        /// Helper to write output, need to be set from constructor of test class
        /// </summary>
        public ITestOutputHelper OutputHelper { get; set; }

        /// <summary>
        /// IoT Device Configuration
        /// </summary>
        public IDeviceConfig DeviceConfig { get { return this; } }

        /// <summary>
        /// IoT Hub Configuration
        /// </summary>
        public IIoTHubConfig IoTHubConfig { get { return this; } }

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
        /// ContainerRegistry Configuration
        /// </summary>
        public IContainerRegistryConfig ContainerRegistryConfig { get { return this; } }

        /// <summary>
        /// Helper to work with Azure.Devices.RegistryManager
        /// </summary>
        public RegistryHelper RegistryHelper { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            RegistryHelper.RegistryManager.Dispose();
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
        ///     - environment variables from launchSettings.json
        /// </summary>
        /// <returns></returns>
        private static IConfigurationRoot GetConfiguration() {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddAllEnvVarsFromLaunchSettings()
                .Build();

            return configuration;
        }

        string IDeviceConfig.DeviceId => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOT_EDGE_DEVICE_ID,
            () => throw new Exception("IoT Edge device id is not provided."));

        string IIoTHubConfig.IoTHubConnectionString => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_IOTHUB_CONNSTRING,
            () => throw new Exception("IoT Hub connection string is not provided."));

        string IIIoTPlatformConfig.BaseUrl => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_SERVICE_URL,
            () => throw new Exception("BaseUrl of Industrial IoT Platform is not provided."));

        string IIIoTPlatformConfig.AuthTenant => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_AUTH_TENANT,
            () => throw new Exception("Authentication Tenant of Industrial IoT Platform is not provided."));

        string IIIoTPlatformConfig.AuthClientId => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_AUTH_CLIENT_APPID,
            () => throw new Exception("Authentication client id of Industrial IoT Platform is not provided."));

        string IIIoTPlatformConfig.AuthClientSecret => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_AUTH_CLIENT_SECRET,
            () => throw new Exception("Authentication client secret of Industrial IoT Platform is not provided."));

        string IIIoTPlatformConfig.ApplicationName => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.ApplicationName,
            () => throw new Exception("ApplicationName of Industrial IoT Platform is not provided."));

        string ISshConfig.Username => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_SIMULATION_USER,
            () => throw new Exception("Username of iot edge device is not provided."));

        string ISshConfig.Password => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_SIMULATION_PASSWORD,
            () => throw new Exception("Password of iot edge device is not provided."));

        string ISshConfig.Host => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOT_EDGE_DEVICE_DNS_NAME,
            () => throw new Exception("DNS name of iot edge device is not provided."));

        string IOpcPlcConfig.Urls => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PLC_SIMULATION_URLS,
            () => throw new Exception("Semicolon separated list of URLs of OPC-PLCs is not provided."));

        string IContainerRegistryConfig.ContainerRegistryServer => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_CONTAINER_REGISTRY_SERVER,
            () => string.Empty);

        string IContainerRegistryConfig.ContainerRegistryUser => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_CONTAINER_REGISTRY_USER,
            () => string.Empty);

        string IContainerRegistryConfig.ContainerRegistryPassword => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_CONTAINER_REGISTRY_PASSWORD,
            () => string.Empty);

        string IContainerRegistryConfig.ImagesNamespace => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_IMAGES_NAMESPACE,
            () => string.Empty);

        string IContainerRegistryConfig.ImagesTag => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_IMAGES_TAG,
            () => throw new Exception("Images tag not provided"));
    }
}
