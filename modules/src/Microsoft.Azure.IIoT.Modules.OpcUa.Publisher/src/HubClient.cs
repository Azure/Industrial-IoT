using System.Threading.Tasks;

namespace OpcPublisher
{
    using Microsoft.Azure.Devices.Client;
    using System;


    /// <summary>
    /// Class to encapsulate the IoTHub device/module client interface.
    /// </summary>
    public class HubClient : IHubClient, IDisposable
    {
        /// <summary>
        /// Stores custom product information that will be appended to the user agent string that is sent to IoT Hub.
        /// </summary>
        public string ProductInfo
        {
            get
            {
                if (_iotHubClient == null)
                {
                    return _edgeHubClient.ProductInfo;
                }
                return _iotHubClient.ProductInfo;
            }
            set
            {
                if (_iotHubClient == null)
                {
                    _edgeHubClient.ProductInfo = value;
                    return;
                }
                _iotHubClient.ProductInfo = value;
            }
        }

        /// <summary>
        /// Ctor for the class.
        /// </summary>
        public HubClient()
        {
        }

        /// <summary>
        /// Ctor for the class.
        /// </summary>
        public HubClient(DeviceClient iotHubClient)
        {
            _iotHubClient = iotHubClient;
        }

        /// <summary>
        /// Ctor for the class.
        /// </summary>
        public HubClient(ModuleClient edgeHubClient)
        {
            _edgeHubClient = edgeHubClient;
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_iotHubClient == null)
                {
                    _edgeHubClient.Dispose();
                    return;
                }
                _iotHubClient.Dispose();
            }
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            // do cleanup
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Create DeviceClient from the specified connection string using the specified transport type
        /// </summary>
        public static IHubClient CreateDeviceClientFromConnectionString(string connectionString, TransportType transportType)
        {
            return new HubClient(DeviceClient.CreateFromConnectionString(connectionString, transportType));
        }

        /// <summary>
        /// Create ModuleClient from the specified connection string using the specified transport type
        /// </summary>
        public static IHubClient CreateModuleClientFromEnvironment(TransportType transportType)
        {
            return new HubClient(ModuleClient.CreateFromEnvironmentAsync(transportType).Result);
        }

        /// <summary>
        /// Close the client instance
        /// </summary>
        public Task CloseAsync()
        {
            if (_iotHubClient == null)
            {
                return _edgeHubClient.CloseAsync();
            }
            return _iotHubClient.CloseAsync();
        }

        /// <summary>
        /// Sets the retry policy used in the operation retries.
        /// </summary>
        public void SetRetryPolicy(IRetryPolicy retryPolicy)
        {
            if (_iotHubClient == null)
            {
                _edgeHubClient.SetRetryPolicy(retryPolicy);
                return;
            }
            _iotHubClient.SetRetryPolicy(retryPolicy);
        }

        /// <summary>
        /// Registers a new delegate for the connection status changed callback. If a delegate is already associated, 
        /// it will be replaced with the new delegate.
        /// </summary>
        public void SetConnectionStatusChangesHandler(ConnectionStatusChangesHandler statusChangesHandler)
        {
            if (_iotHubClient == null)
            {
                _edgeHubClient.SetConnectionStatusChangesHandler(statusChangesHandler);
                return;
            }
            _iotHubClient.SetConnectionStatusChangesHandler(statusChangesHandler);
        }

        /// <summary>
        /// Explicitly open the DeviceClient instance.
        /// </summary>
        public Task OpenAsync()
        {
            if (_iotHubClient == null)
            {
                return _edgeHubClient.OpenAsync();
            }
            return _iotHubClient.OpenAsync();
        }

        /// <summary>
        /// Registers a new delegate for the named method. If a delegate is already associated with
        /// the named method, it will be replaced with the new delegate.
        /// </summary>
        public Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler)
        {
            if (_iotHubClient == null)
            {
                return _edgeHubClient.SetMethodHandlerAsync(methodName, methodHandler, _edgeHubClient);
            }
            return _iotHubClient.SetMethodHandlerAsync(methodName, methodHandler, _iotHubClient);
        }

        /// <summary>
        /// Registers a new delegate that is called for a method that doesn't have a delegate registered for its name. 
        /// If a default delegate is already registered it will replace with the new delegate.
        /// </summary>
        public Task SetMethodDefaultHandlerAsync(MethodCallback methodHandler)
        {
            if (_iotHubClient == null)
            {
                return _edgeHubClient.SetMethodDefaultHandlerAsync(methodHandler, _edgeHubClient);
            }
            return _iotHubClient.SetMethodDefaultHandlerAsync(methodHandler, _iotHubClient);
        }

        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        public Task SendEventAsync(Message message)
        {
            if (_iotHubClient == null)
            {
                return _edgeHubClient.SendEventAsync(message);
            }
            return _iotHubClient.SendEventAsync(message);
        }

        private static DeviceClient _iotHubClient;
        private static ModuleClient _edgeHubClient;
    }
}
