
using IoTHubCredentialTools;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Opc.Ua.Client;
using Publisher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Opc.Ua.Publisher
{
    public class Program
    {
        public static ApplicationConfiguration m_configuration = null;
        public static List<Session> m_sessions = new List<Session>();
        public static PublishedNodesCollection m_nodesLookups = new PublishedNodesCollection();
        public static List<Uri> m_endpointUrls = new List<Uri>();
        public static string m_applicationName = string.Empty;
        public static DeviceClient m_deviceClient = null;

        private static PublisherServer m_server = new PublisherServer();

        /// <summary>
        /// Trace message helper
        /// </summary>
        public static void Trace(string message, params object[] args)
        {
            Utils.Trace(message, args);
            Console.WriteLine(message, args);
        }

        public static void Trace(int traceMask, string format, params object[] args)
        {
            Utils.Trace(traceMask, format, args);
            Console.WriteLine(format, args);
        }

        public static void Trace(Exception e, string format, params object[] args)
        {
            Utils.Trace(e, format, args);
            Console.WriteLine(e.ToString());
            Console.WriteLine(format, args);
        }

        public static void Main(string[] args)
        {
            if ((args.Length == 0) || string.IsNullOrEmpty(args[0]))
            {
                Console.WriteLine("Please specify an application name as argument!");
                return;
            }

            m_applicationName = args[0];
            string ownerConnectionString = string.Empty;

            // check if we also received an owner connection string
            if ((args.Length > 1) && !string.IsNullOrEmpty(args[1]))
            {
                ownerConnectionString = args[1];
            }
            else
            {
                Trace("IoT Hub owner connection string not passed as argument.");

                // check if we have an environment variable to register ourselves with IoT Hub
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_HUB_CS")))
                {
                    ownerConnectionString = Environment.GetEnvironmentVariable("_HUB_CS");
                }
            }

            // register ourselves with IoT Hub
            if (ownerConnectionString != string.Empty)
            {
                Trace("Attemping to register ourselves with IoT Hub using owner connection string: " + ownerConnectionString);
                RegistryManager manager = RegistryManager.CreateFromConnectionString(ownerConnectionString);

                // remove any existing device
                Device existingDevice = manager.GetDeviceAsync(m_applicationName).Result;
                if (existingDevice != null)
                {
                    manager.RemoveDeviceAsync(m_applicationName).Wait();
                }

                Device newDevice = manager.AddDeviceAsync(new Device(m_applicationName)).Result;
                if (newDevice != null)
                {
                    string hostname = ownerConnectionString.Substring(0, ownerConnectionString.IndexOf(";"));
                    string deviceConnectionString = hostname + ";DeviceId=" + m_applicationName + ";SharedAccessKey=" + newDevice.Authentication.SymmetricKey.PrimaryKey;
                    SecureIoTHubToken.Write(m_applicationName, deviceConnectionString);
                }
                else
                {
                    Trace("Could not register ourselves with IoT Hub using owner connection string: " + ownerConnectionString);
                }
            }
            else
            {
                Trace("IoT Hub owner connection string not found, registration with IoT Hub abandoned.");
            }

            // try to read connection string from secure store and open IoTHub client
            Trace("Attemping to read connection string from secure store with certificate name: " + m_applicationName);
            string connectionString = SecureIoTHubToken.Read(m_applicationName);
            if (!string.IsNullOrEmpty(connectionString))
            {
                Trace("Attemping to configure publisher with connection string: " + connectionString);
                m_deviceClient = DeviceClient.CreateFromConnectionString(connectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
                m_deviceClient.RetryPolicy = RetryPolicyType.Exponential_Backoff_With_Jitter;
                m_deviceClient.OpenAsync().Wait();
            }
            else
            {
                Trace("Device connection string not found in secure store.");
            }
                       
            ModuleConfiguration moduleConfiguration = new ModuleConfiguration(m_applicationName);
            m_configuration = moduleConfiguration.Configuration;
            m_configuration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);

            // update log configuration, if available
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_LOGP")))
            {
                m_configuration.TraceConfiguration.OutputFilePath = Environment.GetEnvironmentVariable("_GW_LOGP");
                m_configuration.TraceConfiguration.ApplySettings();
            }

            // get a list of persisted endpoint URLs and create a session for each.
            try
            {
                // check if we have an env variable specifying the published nodes path, otherwise use current directory
                string publishedNodesFilePath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "publishednodes.json";
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_PNFP")))
                {
                    publishedNodesFilePath = Environment.GetEnvironmentVariable("_GW_PNFP");
                }

                Trace("Attemping to load nodes file from: " + publishedNodesFilePath);
                m_nodesLookups = JsonConvert.DeserializeObject<PublishedNodesCollection>(File.ReadAllText(publishedNodesFilePath));
                Trace("Loaded " + m_nodesLookups.Count.ToString() + " nodes.");
            }
            catch (Exception ex)
            {
                Trace("Nodes file loading failed with: " + ex.Message);
            }

            foreach (NodeLookup nodeLookup in m_nodesLookups)
            {
                if (!m_endpointUrls.Contains(nodeLookup.EndPointURL))
                {
                    m_endpointUrls.Add(nodeLookup.EndPointURL);
                }
            }

            // start the server
            try
            {
                Trace("Starting server on endpoint " + m_configuration.ServerConfiguration.BaseAddresses[0].ToString() + "...");
                m_server.Start(m_configuration);
                Trace("Server started.");
            }
            catch (Exception ex)
            {
                Trace("Starting server failed with: " + ex.Message);
            }

            // connect to servers
            Trace("Attemping to connect to servers...");
            try
            {
                List<Task> connectionAttempts = new List<Task>();
                foreach (Uri endpointUrl in m_endpointUrls)
                {
                    Trace("Connecting to server: " + endpointUrl);
                    connectionAttempts.Add(EndpointConnect(endpointUrl));
                }

                // Wait for all sessions to be connected
                Task.WaitAll(connectionAttempts.ToArray());
            }
            catch (Exception ex)
            {
                Trace("Exception: " + ex.ToString() + "\r\n" + ex.InnerException != null ? ex.InnerException.ToString() : null);
            }

            // subscribe to preconfigured nodes
            Trace("Attemping to subscribe to published nodes...");
            if (m_nodesLookups != null)
            {
                foreach (NodeLookup nodeLookup in m_nodesLookups)
                {
                    try
                    {
                        CreateMonitoredItem(nodeLookup);
                    }
                    catch (Exception ex)
                    {
                        Trace("Unexpected error publishing node: " + ex.Message + "\r\nIgnoring node: " + nodeLookup.EndPointURL.AbsoluteUri + ", " + nodeLookup.NodeID.ToString());
                    }
                }
            }

            Console.WriteLine("Publisher is running. Press enter to quit.");
            Console.ReadLine();

            foreach (Session session in m_sessions)
            {
                session.Close();
            }

            m_deviceClient.CloseAsync().Wait();
        }

        /// <summary>
        /// Connects to a single OPC UA Server's endpoint
        /// </summary>
        public static async Task EndpointConnect(Uri endpointUrl)
        {
            EndpointDescription selectedEndpoint = SelectUaTcpEndpoint(DiscoverEndpoints(m_configuration, endpointUrl, 10));
            ConfiguredEndpoint configuredEndpoint = new ConfiguredEndpoint(selectedEndpoint.Server, EndpointConfiguration.Create(m_configuration));
            configuredEndpoint.Update(selectedEndpoint);

            Session newSession = await Session.Create(
                m_configuration,
                configuredEndpoint,
                true,
                false,
                m_configuration.ApplicationName,
                60000,
                new UserIdentity(new AnonymousIdentityToken()),
                null);

            if (newSession != null)
            {
                Trace("Created session with updated endpoint " + selectedEndpoint.EndpointUrl + " from server!");
                newSession.KeepAlive += new KeepAliveEventHandler((sender, e) => StandardClient_KeepAlive(sender, e, newSession));
                m_sessions.Add(newSession);
            }
        }

        /// <summary>
        /// Creates a subscription to a monitored item on an OPC UA server
        /// </summary>
        public static void CreateMonitoredItem(NodeLookup nodeLookup)
        {
            // find the right session using our lookup
            Session matchingSession = null;
            foreach (Session session in m_sessions)
            {
                char[] trimChars = { '/', ' ' };
                if (session.Endpoint.EndpointUrl.TrimEnd(trimChars).Equals(nodeLookup.EndPointURL.ToString().TrimEnd(trimChars), StringComparison.OrdinalIgnoreCase))
                {
                    matchingSession = session;
                    break;
                }
            }

            if (matchingSession != null)
            {
                Subscription subscription = matchingSession.DefaultSubscription;
                if (matchingSession.AddSubscription(subscription))
                {
                    subscription.Create();
                }

                // get the DisplayName for the node.
                Node node = matchingSession.ReadNode(nodeLookup.NodeID);
                string nodeDisplayName = node.DisplayName.Text;
                if (String.IsNullOrEmpty(nodeDisplayName))
                {
                    nodeDisplayName = nodeLookup.NodeID.Identifier.ToString();
                }

                // add the new monitored item.
                MonitoredItem monitoredItem = new MonitoredItem(subscription.DefaultItem);

                monitoredItem.StartNodeId = nodeLookup.NodeID;
                monitoredItem.AttributeId = Attributes.Value;
                monitoredItem.DisplayName = nodeDisplayName;
                monitoredItem.MonitoringMode = MonitoringMode.Reporting;
                monitoredItem.SamplingInterval = 1000;
                monitoredItem.QueueSize = 0;
                monitoredItem.DiscardOldest = true;

                monitoredItem.Notification += new MonitoredItemNotificationEventHandler(MonitoredItem_Notification);
                subscription.AddItem(monitoredItem);
                subscription.ApplyChanges();
            }
            else
            {
                Trace("ERROR: Could not find endpoint URL " + nodeLookup.EndPointURL.ToString() + " in active server sessions, NodeID " + nodeLookup.NodeID.Identifier.ToString() + " NOT published!");
                Trace("To fix this, please update your publishednodes.json file with the updated endpoint URL!");
            }
        }

        /// <summary>
        /// The notification that the data for a monitored item has changed on an OPC UA server
        /// </summary>
        public static void MonitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            try
            {
                if (e.NotificationValue == null || monitoredItem.Subscription.Session == null)
                {
                    return;
                }

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

                JsonEncoder encoder = new JsonEncoder(monitoredItem.Subscription.Session.MessageContext, false);
                string applicationURI = monitoredItem.Subscription.Session.Endpoint.Server.ApplicationUri;
                encoder.WriteString("ApplicationUri", applicationURI);
                encoder.WriteString("DisplayName", monitoredItem.DisplayName);

                // write NodeId as ns=x;i=y
                NodeId nodeId = monitoredItem.ResolvedNodeId;
                encoder.WriteString("NodeId", new NodeId(nodeId.Identifier, nodeId.NamespaceIndex).ToString());

                // suppress output of server timestamp in json by setting it to minvalue
                value.ServerTimestamp = DateTime.MinValue;
                encoder.WriteDataValue("Value", value);

                string json = encoder.CloseAndReturnText();
                var eventMessage = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(json));

                // publish
                eventMessage.Properties.Add("content-type", "application/opcua+uajson");
                eventMessage.Properties.Add("deviceName", m_applicationName);

                try
                {
                    m_deviceClient.SendEventAsync(eventMessage).Wait();
                }
                catch (Exception ex)
                {
                    Trace("Failed to publish message, dropping...");
                    Trace(ex.ToString());
                }
            }
            catch (Exception exception)
            {
                Trace("Error processing monitored item notification: " + exception.ToString());
            }
        }

        /// <summary>
        /// Handler for the standard "keep alive" event sent by all OPC UA servers
        /// </summary>
        private static void StandardClient_KeepAlive(Session sender, KeepAliveEventArgs e, Session session)
        {
            if (e != null && session != null)
            {
                if (!ServiceResult.IsGood(e.Status))
                {
                    Trace(String.Format(
                        "Status: {0}/t/tOutstanding requests: {1}/t/tDefunct requests: {2}",
                        e.Status,
                        session.OutstandingRequestCount,
                        session.DefunctRequestCount));
                }
            }
        }

        /// <summary>
        /// Discovers all endpoints provided by an OPC UA server using a discovery client
        /// </summary>
        private static EndpointDescriptionCollection DiscoverEndpoints(ApplicationConfiguration config, Uri discoveryUrl, int timeout)
        {
            EndpointConfiguration configuration = EndpointConfiguration.Create(config);
            configuration.OperationTimeout = timeout;

            using (DiscoveryClient client = DiscoveryClient.Create(
                discoveryUrl,
                EndpointConfiguration.Create(config)))
            {
                try
                {
                    EndpointDescriptionCollection endpoints = client.GetEndpoints(null);
                    return ReplaceLocalHostWithRemoteHost(endpoints, discoveryUrl);
                }
                catch (Exception e)
                {
                    Trace("Could not fetch endpoints from url: " + discoveryUrl.ToString());
                    Trace("Reason = " + e.Message);
                    throw e;
                }
            }
        }

        /// <summary>
        /// Replaces all instances of "LocalHost" in a collection of endpoint description with the real host name
        /// </summary>
        private static EndpointDescriptionCollection ReplaceLocalHostWithRemoteHost(EndpointDescriptionCollection endpoints, Uri discoveryUrl)
        {
            EndpointDescriptionCollection updatedEndpoints = endpoints;

            foreach (EndpointDescription endpoint in updatedEndpoints)
            {
                endpoint.EndpointUrl = Utils.ReplaceLocalhost(endpoint.EndpointUrl, discoveryUrl.DnsSafeHost);

                StringCollection updatedDiscoveryUrls = new StringCollection();
                foreach (string url in endpoint.Server.DiscoveryUrls)
                {
                    updatedDiscoveryUrls.Add(Utils.ReplaceLocalhost(url, discoveryUrl.DnsSafeHost));
                }

                endpoint.Server.DiscoveryUrls = updatedDiscoveryUrls;
            }

            return updatedEndpoints;
        }

        /// <summary>
        /// Selects the UA TCP endpoint from an endpoint collection with the highest security settings offered
        /// </summary>
        private static EndpointDescription SelectUaTcpEndpoint(EndpointDescriptionCollection endpointCollection)
        {
            EndpointDescription bestEndpoint = null;
            foreach (EndpointDescription endpoint in endpointCollection)
            {
                if (endpoint.TransportProfileUri == Profiles.UaTcpTransport)
                {
                    if ((bestEndpoint == null) ||
                        (endpoint.SecurityLevel > bestEndpoint.SecurityLevel))
                    {
                        bestEndpoint = endpoint;
                    }
                }
            }

            return bestEndpoint;
        }

        /// <summary>
        /// Standard certificate validation callback
        /// </summary>
        private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                Trace("Certificate \""
                    + e.Certificate.Subject
                    + "\" not trusted. If you want to trust this certificate, please copy it from the \""
                    + m_configuration.SecurityConfiguration.RejectedCertificateStore.StorePath + "/certs"
                    + "\" to the \""
                    + m_configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath + "/certs"
                    + "\" folder. A restart of the gateway is NOT required.");
            }
        }

    }
}
