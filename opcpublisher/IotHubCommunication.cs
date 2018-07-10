
using System.Threading;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Client;
    using Opc.Ua;
    using System;
    using static OpcStackConfiguration;
    using static Program;

    /// <summary>
    /// Class to handle all IoTHub communication.
    /// </summary>
    public class IotHubCommunication : HubCommunication
    {
        public static string IotDeviceCertDirectoryStorePathDefault => "CertificateStores/IoTHub";

        public static string IotDeviceCertX509StorePathDefault => "My";

        public static string IotHubOwnerConnectionString { get; set; } = null;
        public static Microsoft.Azure.Devices.Client.TransportType IotHubProtocol
        {
            get => HubProtocol;
            set => HubProtocol = value;
        }

        public static string IotDeviceCertStoreType { get; set; } = CertificateStoreType.X509Store;
        public static string IotDeviceCertStorePath { get; set; } = IotDeviceCertX509StorePathDefault;
        public static string DeviceConnectionString { get; set; } = null;


        /// <summary>
        /// Ctor for the class.
        /// </summary>
        public IotHubCommunication(CancellationToken ct) : base(ct)
        {
        }

        /// <summary>
        /// Initializes the IoTHub communication.
        /// </summary>
        public async Task<bool> InitAsync()
        {
            try
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
                        Device existingDevice = await manager.GetDeviceAsync(ApplicationName);
                        if (existingDevice != null)
                        {
                            Logger.Information($"Device '{ApplicationName}' found in IoTHub registry. Remove it.");
                            await manager.RemoveDeviceAsync(ApplicationName);
                        }

                        Logger.Information($"Adding device '{ApplicationName}' to IoTHub registry.");
                        Device newDevice = await manager.AddDeviceAsync(new Device(ApplicationName));
                        if (newDevice != null)
                        {
                            Logger.Information($"Generate device connection string.");
                            string hostname = IotHubOwnerConnectionString.Substring(0, IotHubOwnerConnectionString.IndexOf(";"));
                            DeviceConnectionString = hostname + ";DeviceId=" + ApplicationName + ";SharedAccessKey=" + newDevice.Authentication.SymmetricKey.PrimaryKey;
                        }
                        else
                        {
                            Logger.Fatal($"Can not register ourselves with IoT Hub. Exiting...");
                            return false;
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
                    await SecureIoTHubToken.WriteAsync(ApplicationName, DeviceConnectionString, IotDeviceCertStoreType, IotDeviceCertStorePath);
                }

                // try to read connection string from secure store and open IoTHub client
                Logger.Information($"Attempting to read device connection string from cert store using subject name: {ApplicationName}");
                DeviceConnectionString = await SecureIoTHubToken.ReadAsync(ApplicationName, IotDeviceCertStoreType, IotDeviceCertStorePath);

                if (string.IsNullOrEmpty(DeviceConnectionString))
                {
                    Logger.Fatal("Device connection string not found in secure store. Please pass it in at least once via command line option. Can not connect to IoTHub. Exiting...");
                    return false;
                }

                // connect to IoTHub
                DeviceClient hubClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, IotHubProtocol);
                return (await InitHubCommunicationAsync(hubClient, IotHubProtocol));
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Error in IoTHub initialization.");
                return false;
            }
        }
    }
}
