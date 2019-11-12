// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher
{
    using Opc.Ua;
    using System;
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
            Logger.Information($"IoTHub device cert store type is: {IotDeviceCertStoreType}");
            Logger.Information($"IoTHub device cert path is: {IotDeviceCertStorePath}");

            if (string.IsNullOrEmpty(DeviceConnectionString))
            {
                string errorMessage = $"Device connection string not provided. Please pass it in at least once via command line option. Can not connect to IoTHub. Exiting...";
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
