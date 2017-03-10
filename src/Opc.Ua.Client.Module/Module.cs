// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Opc.Ua.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Runtime.Serialization;
    using System.Text;
    using Newtonsoft.Json;
    using Microsoft.Azure.Devices.Gateway;
    using Ua;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Sample Gateway module - acts as Opc.Ua publisher and publishing server
    /// </summary>
    public class SampleModule : IGatewayModule, IGatewayModuleStart
    {
        /// <summary>
        /// Create module, throws if configuration is bad
        /// </summary>
        /// <param name="broker"></param>
        /// <param name="configuration"></param>
        public void Create(Broker broker, byte[] configuration)
        {
            _broker = broker;

            string configString = Encoding.UTF8.GetString(configuration);

            // Deserialize from configuration string
            _configuration = JsonConvert.DeserializeObject<SampleConfiguration>(configString);

            foreach (var session in _configuration.Subscriptions)
            {
                session.Module = this;
            }

            Console.WriteLine("Opc.Ua.Client.SampleModule:  created.");
        }

        /// <summary>
        /// Disconnect and dispose all sessions
        /// </summary>
        public void Destroy()
        {
            foreach (var session in _configuration.Subscriptions)
            {
                // Disconnect and dispose
                session.Dispose();
            }
            // Then gc.
            _configuration.Subscriptions.Clear();
            Console.WriteLine("Opc.Ua.Client.SampleModule:  destroyed.");
        }

        /// <summary>
        /// Receive message from broker
        /// </summary>
        /// <param name="received_message"></param>
        public void Receive(Message received_message)
        {
            // No-op
        }

        /// <summary>
        /// Publish message to bus
        /// </summary>
        /// <param name="message"></param>
        public void Publish(Message message)
        {
            if (_broker != null)
            {
                _broker.Publish(message);
            }
        }

        /// <summary>
        /// Called when gateway starts, establishes the connections to endpoints
        /// </summary>
        public void Start()
        {
            Console.WriteLine("Opc.Ua.Client.SampleModule: starting...");

            var connections = new List<Task>();
            foreach (var session in _configuration.Subscriptions)
            {
                connections.Add(session.EndpointConnect());
            }
            try
            {
                Task.WaitAll(connections.ToArray());
            }
            catch (AggregateException ae)
            {
                foreach (var ex in ae.InnerExceptions)
                {
                    Console.WriteLine($"Opc.Ua.Client.SampleModule: Could not connect {ex.ToString()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Opc.Ua.Client.SampleModule: Could not connect {ex.ToString()}");
            }

            // Wait for all sessions to be connected
            Console.WriteLine("Opc.Ua.Client.SampleModule: started.");
        }


        /// <summary>
        /// Allow access to the opc ua configuration
        /// </summary>
        internal ApplicationConfiguration Configuration
        {
            get
            {
                return _configuration.Configuration;
            }
        }

        private Broker _broker;
        private SampleConfiguration _configuration;
    }

    /// <summary>
    /// Module configuration object to deserialize / serialize
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class SampleConfiguration
    {
        /// <summary>
        /// Opc client configuration
        /// </summary>
        [JsonProperty]
        public ApplicationConfiguration Configuration { get; set; }

        /// <summary>
        /// List of sessions to create on startup
        /// </summary>
        [JsonProperty]
        public List<ServerSession> Subscriptions { get; set; }

        /// <summary>
        /// Called when the object is deserialized
        /// </summary>
        /// <param name="context"></param>
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            // Validate configuration and set reasonable defaults

            Configuration.ApplicationUri = Configuration.ApplicationUri.Replace("localhost", Utils.GetHostName());

            if (Configuration.TransportQuotas == null)
                Configuration.TransportQuotas = new TransportQuotas { OperationTimeout = 15000 };

            if (Configuration.ClientConfiguration == null)
                Configuration.ClientConfiguration = new ClientConfiguration();
            if (Configuration.ServerConfiguration == null)
                Configuration.ServerConfiguration = new ServerConfiguration();

            if (Configuration.SecurityConfiguration.TrustedPeerCertificates == null)
                Configuration.SecurityConfiguration.TrustedPeerCertificates = new CertificateTrustList();
            if (Configuration.SecurityConfiguration.TrustedPeerCertificates.StoreType == null)
                Configuration.SecurityConfiguration.TrustedPeerCertificates.StoreType = 
                    "Directory";
            if (Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath == null)
                Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath =
                    "OPC Foundation/CertificateStores/UA Applications";

            if (Configuration.SecurityConfiguration.TrustedIssuerCertificates == null)
                Configuration.SecurityConfiguration.TrustedIssuerCertificates = new CertificateTrustList();
            if (Configuration.SecurityConfiguration.TrustedIssuerCertificates.StoreType == null)
                Configuration.SecurityConfiguration.TrustedIssuerCertificates.StoreType =
                    "Directory";
            if (Configuration.SecurityConfiguration.TrustedIssuerCertificates.StorePath == null)
                Configuration.SecurityConfiguration.TrustedIssuerCertificates.StorePath =
                    "OPC Foundation/CertificateStores/UA Certificate Authorities";

            if (Configuration.SecurityConfiguration.RejectedCertificateStore == null)
                Configuration.SecurityConfiguration.RejectedCertificateStore = new CertificateTrustList();
            if (Configuration.SecurityConfiguration.RejectedCertificateStore.StoreType == null)
                Configuration.SecurityConfiguration.RejectedCertificateStore.StoreType =
                    "Directory";
            if (Configuration.SecurityConfiguration.RejectedCertificateStore.StorePath == null)
                Configuration.SecurityConfiguration.RejectedCertificateStore.StorePath =
                    "OPC Foundation/CertificateStores/RejectedCertificates";

            if (Configuration.SecurityConfiguration.ApplicationCertificate == null)
                Configuration.SecurityConfiguration.ApplicationCertificate = new CertificateIdentifier();
            if (Configuration.SecurityConfiguration.ApplicationCertificate.StoreType == null)
                Configuration.SecurityConfiguration.ApplicationCertificate.StoreType =
                    "X509Store";
            if (Configuration.SecurityConfiguration.ApplicationCertificate.StorePath == null)
                Configuration.SecurityConfiguration.ApplicationCertificate.StorePath =
                    "CurrentUser\\UA_MachineDefault";
            if (Configuration.SecurityConfiguration.ApplicationCertificate.SubjectName == null)
                Configuration.SecurityConfiguration.ApplicationCertificate.SubjectName =
                    Configuration.ApplicationName;

            Configuration.Validate(Configuration.ApplicationType).Wait();

            if (Configuration.SecurityConfiguration.ApplicationCertificate.Certificate == null)
            {
                X509Certificate2 certificate = CertificateFactory.CreateCertificate(
                    Configuration.SecurityConfiguration.ApplicationCertificate.StoreType,
                    Configuration.SecurityConfiguration.ApplicationCertificate.StorePath,
                    Configuration.ApplicationUri,
                    Configuration.ApplicationName,
                    Configuration.SecurityConfiguration.ApplicationCertificate.SubjectName
                    );
                Configuration.SecurityConfiguration.ApplicationCertificate.Certificate = certificate;
            }

            if (Configuration.SecurityConfiguration.ApplicationCertificate.Certificate != null)
            {
                Configuration.ApplicationUri = Utils.GetApplicationUriFromCertificate(
                    Configuration.SecurityConfiguration.ApplicationCertificate.Certificate);
            }
            else
            {
                Console.WriteLine("Opc.Ua.Client.SampleModule: WARNING: missing application certificate, using unsecure connection.");
            }

            if (Configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates) 
                Configuration.CertificateValidator.CertificateValidation +=
                    new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
        }

        /// <summary>
        /// Auto accept certificates
        /// </summary>
        /// <param name="validator"></param>
        /// <param name="e"></param>
        private void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                e.Accept = true;
                Console.WriteLine($"Opc.Ua.Client.SampleModule: WARNING: Auto-accepting certificate: {e.Certificate.Subject}");
            }
        }
    }

    /// <summary>
    /// A server session contains a subscription for a list of monitored items
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ServerSession : IDisposable
    {
        /// <summary>
        /// Url of the Server to connect to
        /// </summary>
        [JsonProperty]
        public Uri ServerUrl { get; set; }

        /// <summary>
        /// SharedAccessKey for device
        ///
        /// TODO: Security: The shared access key should be stored in secure storage, 
        /// and the device ID can be used as a lookup
        /// </summary>
        [JsonProperty]
        public string SharedAccessKey { get; set; }

        /// <summary>
        /// Device id or mappable id for mapper
        /// </summary>
        [JsonProperty]
        public string Id { get; set; }

        /// <summary>
        /// Polling interval
        /// </summary>
        [JsonProperty]
        public int PublishingInterval { get; set; }

        /// <summary>
        /// Minimum desired Security Level 
        /// </summary>
        [JsonProperty]
        public byte MinimumSecurityLevel { get; set; } = 0;

        /// <summary>
        /// Minimum desired Security mode 
        /// </summary>
        [JsonProperty]
        public MessageSecurityMode MinimumSecurityMode { get; set; } = MessageSecurityMode.SignAndEncrypt;

        /// <summary>
        /// Monitored item configuration
        /// </summary>
        [JsonProperty]
        public List<MonitoredItem> MonitoredItems { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerSession()
        {
            MonitoredItems = new List<MonitoredItem>();
            PublishingInterval = 1000;
        }

        /// <summary>
        /// The Module the session is attached to.
        /// </summary>
        internal SampleModule Module { get; set; }

        /// <summary>
        /// Called when the object is deserialized
        /// </summary>
        /// <param name="context"></param>
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (MonitoredItems.Count == 0)
            {
                throw new Exception("Configuration did not specify monitored items!");
            }

            if (ServerUrl == null)
            {
                throw new Exception("Configuration did not contain an endpoint Uri");
            }

            if (string.IsNullOrEmpty(Id))
            {
                throw new Exception("Configuration did not contain a device name!");
            }

            MonitoredItems.ForEach(i => i.Notification += MonitoredItem_Notification);
        }

        public async Task EndpointConnect()
        {
            var endpointCollection = DiscoverEndpoints(Module.Configuration, ServerUrl, 60);
            var selectedEndpoints = new List<EndpointDescription>();

            // Select endpoints
            foreach (EndpointDescription endpoint in endpointCollection)
            {
                if (endpoint.TransportProfileUri == Profiles.UaTcpTransport &&
                    endpoint.SecurityLevel >= MinimumSecurityLevel &&
                    endpoint.SecurityMode >= MinimumSecurityMode)
                {
                    // patch endpoint to set the original host name we want to connect to.
                    var url = new UriBuilder(endpoint.EndpointUrl);
                    url.Host = ServerUrl.Host;
                    endpoint.EndpointUrl = url.ToString();
                    selectedEndpoints.Add(endpoint);
                }
            }

            //
            // Sort, but descending with highest level first i.e. return 
            // < 0 if x is less than y
            // > 0 if x is greater than y
            //   0 if x and y are equal
            //
            selectedEndpoints.Sort((y, x) => x.SecurityLevel - y.SecurityLevel);

            // Do not emit all exceptions as they occur, only throw them all when no connection can be made.
            var exceptions = new List<Exception>(selectedEndpoints.Count);
            foreach (EndpointDescription endpoint in selectedEndpoints)
            {
                ConfiguredEndpoint configuredEndpoint = new ConfiguredEndpoint(
                    endpoint.Server, EndpointConfiguration.Create(Module.Configuration));
                configuredEndpoint.Update(endpoint);
                try 
                {
                    Console.WriteLine($"Opc.Ua.Client.SampleModule: Trying to create session with mode: {endpoint.SecurityMode}, level:{endpoint.SecurityLevel} to {configuredEndpoint.EndpointUrl}...");
                    _session = await Session.Create(
                        Module.Configuration,
                        configuredEndpoint,
                        true,
                        false,
                        Module.Configuration.ApplicationName,
                        60000,
                        // TODO: Make user identity configurable, plus add dedicated security policy
                        new UserIdentity(new AnonymousIdentityToken()),
                        null);

                    if (_session != null) 
                    {
                        var subscription = new Subscription(_session.DefaultSubscription);
                        subscription.PublishingInterval = PublishingInterval;

                        // TODO: Make other subscription settings configurable...
                        subscription.AddItems(MonitoredItems);
                        _session.AddSubscription(subscription);
                        subscription.Create();

                        Console.WriteLine($"Opc.Ua.Client.SampleModule: Session with mode: {endpoint.SecurityMode}, level:{endpoint.SecurityLevel} to {configuredEndpoint.EndpointUrl} established!");
                        _session.KeepAlive += new KeepAliveEventHandler(StandardClient_KeepAlive);

                        // Done
                        return;
                    }
                    exceptions.Add(new Exception($"ERROR: Create session to endpoint {endpoint.ToString()} returned null."));
                }
                catch (AggregateException ae) 
                {
                    exceptions.AddRange(ae.InnerExceptions);
                }
                catch (Exception ex) 
                {
                    exceptions.Add(ex);
                }

                //  ... try another endpoint until we do not have any more...
                Console.WriteLine($"Opc.Ua.Client.SampleModule: WARNING Could not create session to endpoint {endpoint.ToString()}...");
            }
            throw new AggregateException("Failed to find acceptable endpoint to connect to.", exceptions);
        }

        private void MonitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            try
            {
                MonitoredItemNotification notification = e.NotificationValue as MonitoredItemNotification;
                if (notification == null)
                {
                    return;
                }

                DataValue value = notification.Value as DataValue;
                if (value == null)
                {
                    return;
                }

                using (var encoder = new JsonEncoder(monitoredItem.Subscription.Session.MessageContext, false))
                {
                    string applicationURI = monitoredItem.Subscription.Session.Endpoint.Server.ApplicationUri;
                    encoder.WriteString("ApplicationUri", applicationURI);
                    encoder.WriteString("DisplayName", monitoredItem.DisplayName);
                    encoder.WriteNodeId("MonitoredItem", monitoredItem.ResolvedNodeId);
                    // suppress output of server timestamp in json by setting it to minvalue
                    value.ServerTimestamp = DateTime.MinValue;
                    encoder.WriteDataValue("Value", value);

                    string json = encoder.CloseAndReturnText();

                    var properties = new Dictionary<string, string>();
                    properties.Add("content-type", "application/opcua+uajson");
                    properties.Add("deviceName", Id);

                    if (SharedAccessKey != null)
                    {
                        properties.Add("source", "mapping");
                        properties.Add("deviceKey", SharedAccessKey);
                    }

                    try
                    {
                        Module.Publish(new Message(json, properties));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Opc.Ua.Client.SampleModule: Failed to publish message, dropping...");
                        Console.WriteLine(ex.ToString());
                    }
                }

            }
            catch (Exception exception)
            {
                Console.WriteLine("Opc.Ua.Client.SampleModule: Error processing monitored item notification.");
                Console.WriteLine(exception.ToString());
            }
        }

        private void StandardClient_KeepAlive(Session sender, KeepAliveEventArgs e)
        {
            if (e != null && sender != null)
            {
                if (!ServiceResult.IsGood(e.Status))
                {
                    Console.WriteLine($"Opc.Ua.Client.SampleModule: Server {sender.ConfiguredEndpoint.EndpointUrl} Status NOT good: {e.Status} {sender.OutstandingRequestCount}/{sender.DefunctRequestCount}");
                }
            }
        }

        private EndpointDescriptionCollection DiscoverEndpoints(ApplicationConfiguration config, Uri discoveryUrl, int timeout)
        {
            EndpointConfiguration configuration = EndpointConfiguration.Create(config);
            configuration.OperationTimeout = timeout;

            using (DiscoveryClient client = DiscoveryClient.Create(discoveryUrl, EndpointConfiguration.Create(config)))
            {
                try
                {
                    EndpointDescriptionCollection endpoints = client.GetEndpoints(null);
                    ReplaceLocalHostWithRemoteHost(endpoints, discoveryUrl);
                    return endpoints;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Opc.Ua.Client.SampleModule: Could not fetch endpoints from url: {discoveryUrl}");
                    Console.WriteLine($"Opc.Ua.Client.SampleModule: Reason = {e.Message}");
                    throw e;
                }
            }
        }

        private void ReplaceLocalHostWithRemoteHost(EndpointDescriptionCollection endpoints, Uri discoveryUrl)
        {
            foreach (EndpointDescription endpoint in endpoints)
            {
                endpoint.EndpointUrl = Utils.ReplaceLocalhost(endpoint.EndpointUrl, discoveryUrl.DnsSafeHost);
                StringCollection updatedDiscoveryUrls = new StringCollection();

                foreach (string url in endpoint.Server.DiscoveryUrls)
                {
                    updatedDiscoveryUrls.Add(Utils.ReplaceLocalhost(url, discoveryUrl.DnsSafeHost));
                }

                endpoint.Server.DiscoveryUrls = updatedDiscoveryUrls;
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dispose implementation
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                lock (this)
                {
                    if (!_disposedValue)
                    {
                        if (disposing)
                        {
                            if (_session != null)
                            {
                                _session.Dispose();
                                _session = null;
                            }
                        }

                        _disposedValue = true;
                    }
                }
            }
        }

        private Session _session;
        private bool _disposedValue = false; // To detect redundant calls
    }
}
