// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.TestExtensions
{
    using Azure.ResourceManager.Resources;
    using Config;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Neovolve.Logging.Xunit;
    using System;
    using System.Collections.Generic;
    using Xunit.Abstractions;

    /// <summary>
    /// Context to pass data between test cases
    /// </summary>
    public class IIoTPlatformTestContext : IDisposable, IDeviceConfig, IIoTHubConfig,
        IIoTEdgeConfig, ISshConfig, IOpcPlcConfig, IContainerRegistryConfig
    {
        private ITestOutputHelper _outputHelper = new DummyOutput();

        /// <summary>
        /// Configuration
        /// </summary>
        private IConfiguration Configuration { get; }

        public IIoTPlatformTestContext()
        {
            Configuration = GetConfiguration();
            RegistryHelper = new RegistryHelper(this);
        }

        /// <summary>
        /// Helper to write output, need to be set from constructor of test class
        /// </summary>
        public ITestOutputHelper OutputHelper => _outputHelper;

        /// <summary>
        /// Helper to write output, need to be set from constructor of test class
        /// </summary>
        /// <param name="output"></param>
        public void SetOutputHelper(ITestOutputHelper output)
        {
            ArgumentNullException.ThrowIfNull(output);
            LogEnvironment(output);
            _outputHelper = output;

            _logFactory?.Dispose();
            _logFactory = LogFactory.Create(_outputHelper);
        }

        public ILogger<T> CreateLogger<T>()
        {
            return _logFactory?.CreateLogger<T>();
        }

        private sealed class DummyOutput : ITestOutputHelper
        {
            public void WriteLine(string message) { Console.WriteLine(message); }
            public void WriteLine(string format, params object[] args) { Console.WriteLine(format, args); }
        }

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

        /// <summary>
        /// Resource group object
        /// </summary>
        public ResourceGroupResource ResourceGroup { get; set; }

        /// <summary>
        /// Urls for the dynamic ACI containers
        /// </summary>
        public IReadOnlyList<string> PlcAciDynamicUrls { get; set; }

        /// <summary>
        /// Azure Storage Name
        /// </summary>
        public string AzureStorageName { get; set; }

        /// <summary>
        /// Azure Storage Key
        /// </summary>
        public string AzureStorageKey { get; set; }

        /// <summary>
        /// Image that are used for PLC ACI
        /// </summary>
        public string PLCImage { get; set; }

        /// <summary>
        /// Testing suffix for this environment
        /// </summary>
        public string TestingSuffix { get; set; }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Override for disposing
        /// </summary>
        /// <param name="disposing">Indicates if called from <see cref="Dispose"/></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                RegistryHelper.Dispose();
                _logFactory?.Dispose();
            }
        }

        /// <summary>
        /// Read configuration variable
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private string GetStringOrDefault(string key, Func<string> defaultValue)
        {
            var value = Configuration.GetValue<string>(key);
            if (string.IsNullOrEmpty(value))
            {
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
        private static IConfigurationRoot GetConfiguration()
        {
            return new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddFromDotEnvFile()
                .Build();
        }

        public string DeviceId => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOT_EDGE_DEVICE_ID,
            () => throw new InvalidOperationException("IoT Edge device id is not provided."));

        public string IoTHubConnectionString => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_IOTHUB_CONNSTRING,
            () => throw new InvalidOperationException("IoT Hub connection string is not provided."));

        public string IoTHubEventHubConnectionString => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOTHUB_EVENTHUB_CONNECTIONSTRING,
            () => throw new InvalidOperationException("IoT Hub EventHub connection string is not provided."));

        public string EdgeVersion => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOT_EDGE_VERSION,
            () => "1.4");

        public string Username => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOT_EDGE_VM_USERNAME,
            () => throw new InvalidOperationException("Username of iot edge device is not provided."));

        public string PublicKey => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOT_EDGE_VM_PUBLICKEY,
            () => throw new InvalidOperationException("Public key of iot edge device is not provided."));

        public string PrivateKey => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOT_EDGE_VM_PRIVATEKEY,
            () => throw new InvalidOperationException("Private key of iot edge device is not provided."));

        public string Host => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.IOT_EDGE_DEVICE_DNSNAME,
            () => throw new InvalidOperationException("DNS name of iot edge device is not provided."));

        public string Urls => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PLC_SIMULATION_URLS,
            () => throw new InvalidOperationException("Semicolon separated list of URLs of OPC-PLCs is not provided."));

        public string Ips => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PLC_SIMULATION_IPS,
            () => throw new InvalidOperationException("Semicolon separated list of ip addresses of OPC-PLCs is not provided."));

        public string TenantId => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_AUTH_TENANT,
            () => GetStringOrDefault("AZURE_TENANT_ID", () => throw new InvalidOperationException("Tenant Id is not provided.")));

        public string ResourceGroupName => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_RESOURCE_GROUP,
            () => throw new InvalidOperationException("Resource Group Name is not provided."));

        public string SubscriptionId => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_SUBSCRIPTION_ID, () => string.Empty);

        public string ContainerRegistryServer => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_DOCKER_SERVER,
            () => string.Empty);

        public string ContainerRegistryUser => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_DOCKER_USER,
            () => string.Empty);

        public string ContainerRegistryPassword => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_DOCKER_PASSWORD,
            () => string.Empty);

        public string ImagesNamespace => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_IMAGES_NAMESPACE,
            () => string.Empty);

        public string ImagesTag => GetStringOrDefault(TestConstants.EnvironmentVariablesNames.PCS_IMAGES_TAG,
            () => "latest");

        public void LogEnvironment(ITestOutputHelper output)
        {
            if (output == null || _logged || output is DummyOutput)
            {
                return;
            }
            _logged = true;
            Log("ApplicationName");
            Log("PCS_IMAGES_TAG");
            Log("PCS_DOCKER_SERVER");
            Log("PCS_DOCKER_USER");
            Log("PCS_DOCKER_PASSWORD");
            Log("PCS_IMAGES_NAMESPACE");
            Log("PCS_SUBSCRIPTION_ID");
            Log("PCS_RESOURCE_GROUP");
            Log("PCS_SERVICE_URL");
            Log("PLC_SIMULATION_URLS");
            Log("IOT_EDGE_VERSION");
            Log("IOT_EDGE_DEVICE_ID");
            Log("IOT_EDGE_DEVICE_DNSNAME");
            Log("IOT_EDGE_VM_USERNAME");
            Log("PCS_IOTHUB_CONNSTRING");
            Log("IOTHUB_EVENTHUB_CONNECTIONSTRING");
            void Log(string envVar) => output.WriteLine($"{envVar}: '{Environment.GetEnvironmentVariable(envVar)}'");
        }
        private bool _logged;
        private ILoggerFactory _logFactory;
    }
}
