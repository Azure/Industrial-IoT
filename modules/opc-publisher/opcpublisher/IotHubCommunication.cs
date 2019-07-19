namespace OpcPublisher
{
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Client;
    using Opc.Ua;
    using System;
    using static OpcApplicationConfiguration;
    using static Program;

    /// <summary>
    /// Class to handle all IoTHub communication.
    /// </summary>
    public class IotHubCommunication : HubCommunicationBase
    {
        /// <summary>
        /// Default cert store path of the IoTHub credentials for store type directory.
        /// </summary>
        public static string IotDeviceCertDirectoryStorePathDefault => "CertificateStores/IoTHub";

        /// <summary>
        /// Default cert store path of the IoTHub credentials for store type X509Store.
        /// </summary>
        public static string IotDeviceCertX509StorePathDefault => "My";

        /// <summary>
        /// Cert store type for the IoTHub credentials.
        /// </summary>

        public static string IotDeviceCertStoreType { get; set; } = CertificateStoreType.X509Store;

        /// <summary>
        /// Cert store path for the IoTHub credentials.
        /// </summary>
        public static string IotDeviceCertStorePath { get; set; } = IotDeviceCertX509StorePathDefault;

        /// <summary>
        /// The device connection string to be used to connect to IoTHub.
        /// </summary>
        public static string DeviceConnectionString { get; set; } = null;

        /// <summary>
        /// The IoTHub owner connection string to be used to connect to IoTHub.
        /// </summary>
        public static string IotHubOwnerConnectionString { get; set; } = null;

        /// <summary>
        /// This property is only there to allow mocking of the device client.
        /// </summary>
        public static IHubClient IotHubClient { get; set; }

        /// <summary>
        /// Get the singleton.
        /// </summary>
        public static IotHubCommunication Instance
        {
            get
            {
                lock (_singletonLock)
                {
                    if (_instance == null)
                    {
                        _instance = new IotHubCommunication();
                    }
                    return _instance;
                }
            }
        }

        /// <summary>
        /// Ctor for the singleton class.
        /// </summary>
        private IotHubCommunication()
        {
            // check if we got an IoTHub owner connection string
            if (string.IsNullOrEmpty(IotHubOwnerConnectionString))
            {
                Logger.Information("IoT Hub owner connection string not passed as argument.");

                // check if we have an environment variable to register ourselves with IoT Hub
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_HUB_CS")))
                {
                    IotHubOwnerConnectionString = Environment.GetEnvironmentVariable("_HUB_CS");
                    Logger.Information("IoT Hub owner connection string read from environment.");
                }
            }

            Logger.Information($"IoTHub device cert store type is: {IotDeviceCertStoreType}");
            Logger.Information($"IoTHub device cert path is: {IotDeviceCertStorePath}");
            if (string.IsNullOrEmpty(IotHubOwnerConnectionString))
            {
                Logger.Information("IoT Hub owner connection string not specified. Assume device connection string already in cert store or passed in via command line option.");
            }
            else
            {
                if (string.IsNullOrEmpty(DeviceConnectionString))
                {
                    Logger.Information($"Attempting to register ourselves with IoT Hub using owner connection string.");
                    RegistryManager manager = RegistryManager.CreateFromConnectionString(IotHubOwnerConnectionString);

                    // remove any existing device
                    Device existingDevice = manager.GetDeviceAsync(ApplicationName).Result;
                    if (existingDevice != null)
                    {
                        Logger.Information($"Device '{ApplicationName}' found in IoTHub registry. Remove it.");
                        manager.RemoveDeviceAsync(ApplicationName).Wait();
                    }

                    Logger.Information($"Adding device '{ApplicationName}' to IoTHub registry.");
                    Device newDevice = manager.AddDeviceAsync(new Device(ApplicationName)).Result;
                    if (newDevice != null)
                    {
                        Logger.Information($"Generate device connection string.");
                        string hostname = IotHubOwnerConnectionString.Substring(0, IotHubOwnerConnectionString.IndexOf(";", StringComparison.InvariantCulture));
                        DeviceConnectionString = hostname + ";DeviceId=" + ApplicationName + ";SharedAccessKey=" + newDevice.Authentication.SymmetricKey.PrimaryKey;
                    }
                    else
                    {
                        string errorMessage = $"Can not register ourselves with IoT Hub. Exiting...";
                        Logger.Fatal(errorMessage);
                        throw new Exception(errorMessage);
                    }
                }
                else
                {
                    Logger.Information($"There have been a device connectionstring specified on command line. Skipping device creation in IoTHub. Please ensure you created a device with name '{ApplicationName}' manually.");
                }
            }

            // save the device connectionstring, if we have one
            if (!string.IsNullOrEmpty(DeviceConnectionString))
            {
                Logger.Information($"Adding device connectionstring to secure store.");
                SecureIoTHubToken.WriteAsync(ApplicationName, DeviceConnectionString, IotDeviceCertStoreType, IotDeviceCertStorePath).Wait();
            }

            // try to read connection string from secure store and open IoTHub client
            Logger.Information($"Attempting to read device connection string from cert store using subject name: {ApplicationName}");
            DeviceConnectionString = SecureIoTHubToken.ReadAsync(ApplicationName, IotDeviceCertStoreType, IotDeviceCertStorePath).Result;

            if (string.IsNullOrEmpty(DeviceConnectionString))
            {
                string errorMessage = $"Device connection string not found in secure store. Please pass it in at least once via command line option. Can not connect to IoTHub. Exiting...";
                Logger.Fatal(errorMessage);
                throw new ArgumentException(errorMessage);
            }

            // connect as device client
            Logger.Information($"Create device client using '{HubProtocol}' for communication.");
            IotHubClient = HubClient.CreateDeviceClientFromConnectionString(DeviceConnectionString, HubProtocol);
            if (!InitHubCommunicationAsync(IotHubClient).Result)
            {
                string errorMessage = $"Cannot create IoTHub client. Exiting...";
                Logger.Fatal(errorMessage);
                throw new Exception(errorMessage);
            }
        }

        private static readonly object _singletonLock = new object();
        private static IotHubCommunication _instance;
    }
}
