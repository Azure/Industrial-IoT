// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Gateway;
using System.Net.Http;
using System.Net.Http.Headers;
using Publisher;
using Opc.Ua.Client;
using IoTHubCredentialTools;
using System.IO;
using System.Net;

namespace Opc.Ua.Publisher
{
    /// <summary>
    /// Gateway module that acts as Opc.Ua Publisher and Server
    /// </summary>
    public class Module : IGatewayModule, IGatewayModuleStart
    {
        public static ApplicationConfiguration m_configuration = null;
        public static List<Session> m_sessions = new List<Session>();
        public static PublishedNodesCollection m_nodesLookups = new PublishedNodesCollection();
        public static List<Uri> m_endpointUrls = new List<Uri>();
        public static string m_deviceName = string.Empty;
        public static string m_accessKey = string.Empty;

        private static Broker m_broker = null;
        private PublisherServer m_server = new PublisherServer();

        private const string m_IoTHubAPIVersion = "?api-version=2016-11-14";

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

        /// <summary>
        /// Create module, throws if configuration is bad
        /// </summary>
        public void Create(Broker broker, byte[] configuration)
        {
            Trace("Opc.Ua.Publisher.Module: Creating...");

            m_broker = broker;

            string configString = Encoding.UTF8.GetString(configuration);

            // Deserialize from configuration string
            ModuleConfiguration moduleConfiguration = null;
            try
            {
                moduleConfiguration = JsonConvert.DeserializeObject<ModuleConfiguration>(configString);
            }
            catch (Exception ex)
            {
                Trace("Opc.Ua.Publisher.Module: Module config string " + configString + " could not be deserialized: " + ex.Message);
                throw;
            }

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

                Trace("Opc.Ua.Publisher.Module: Attemping to load nodes file from: " + publishedNodesFilePath);
                m_nodesLookups = JsonConvert.DeserializeObject<PublishedNodesCollection>(File.ReadAllText(publishedNodesFilePath));
                Trace("Opc.Ua.Publisher.Module: Loaded " + m_nodesLookups.Count.ToString() + " nodes.");
            }
            catch (Exception ex)
            {
                Trace("Opc.Ua.Publisher.Module: Nodes file loading failed with: " + ex.Message);
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
                Trace("Opc.Ua.Publisher.Module: Starting server on endpoint " + m_configuration.ServerConfiguration.BaseAddresses[0].ToString() + "...");
                m_server.Start(m_configuration);
                Trace("Opc.Ua.Publisher.Module: Server started.");
            }
            catch (Exception ex)
            {
                Trace("Opc.Ua.Publisher.Module: Starting server failed with: " + ex.Message);
            }

            // check if we have an environment variable to register ourselves with IoT Hub
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_HUB_CS")))
            {
                string ownerConnectionString = Environment.GetEnvironmentVariable("_HUB_CS");

                if ((m_configuration != null) && (!string.IsNullOrEmpty(m_configuration.ApplicationName)))
                {
                    Trace("Attemping to register ourselves with IoT Hub using owner connection string: " + ownerConnectionString);
                    string deviceConnectionString = IoTHubRegistration.RegisterDeviceWithIoTHub(m_configuration.ApplicationName, ownerConnectionString);
                    if (!string.IsNullOrEmpty(deviceConnectionString))
                    {
                        SecureIoTHubToken.Write(m_configuration.ApplicationName, deviceConnectionString);
                    }
                    else
                    {
                        Trace("Could not register ourselves with IoT Hub using owner connection string: " + ownerConnectionString);
                    }
                }
            }

            // try to configure our publisher component
            TryConfigurePublisherAsync().Wait();

            // connect to servers
            Trace("Opc.Ua.Publisher.Module: Attemping to connect to servers...");
            try
            {
                List<Task> connectionAttempts = new List<Task>();
                foreach (Uri endpointUrl in m_endpointUrls)
                {
                    Trace("Opc.Ua.Publisher.Module: Connecting to server: " + endpointUrl);
                    connectionAttempts.Add(EndpointConnect(endpointUrl));
                }

                // Wait for all sessions to be connected
                Task.WaitAll(connectionAttempts.ToArray());
            }
            catch (Exception ex)
            {
                Trace("Opc.Ua.Publisher.Module: Exception: " + ex.ToString() + "\r\n" + ex.InnerException != null ? ex.InnerException.ToString() : null);
            }

            Trace("Opc.Ua.Publisher.Module: Created.");
        }

        /// <summary>
        /// Disconnect all sessions
        /// </summary>
        public void Destroy()
        {
            foreach (Session session in m_sessions)
            {
                session.Close();
            }

            Trace("Opc.Ua.Publisher.Module: All sessions closed.");
        }

        /// <summary>
        /// Receive message from broker
        /// </summary>
        public void Receive(Message received_message)
        {
            // No-op
        }

        /// <summary>
        /// Try to configure our Publisher settings
        /// </summary>
        public static async Task TryConfigurePublisherAsync()
        {
            // read connection string from secure store and configure publisher, if possible
            if ((m_configuration != null) && (!string.IsNullOrEmpty(m_configuration.ApplicationName)))
            {
                Trace("Opc.Ua.Publisher.Module: Attemping to read connection string from secure store with certificate name: " + m_configuration.ApplicationName);
                string connectionString = SecureIoTHubToken.Read(m_configuration.ApplicationName);
                if (!string.IsNullOrEmpty(connectionString))
                {
                    Trace("Opc.Ua.Publisher.Module: Attemping to configure publisher with connection string: " + connectionString);
                    string[] parsedConnectionString = IoTHubRegistration.ParseConnectionString(connectionString, true);
                    if ((parsedConnectionString != null) && (parsedConnectionString.Length == 3))
                    {
                        // note: IoTHub name can't be changed during runtime in the GW IoTHub module
                        string _IoTHubName = parsedConnectionString[0];
                        m_deviceName = parsedConnectionString[1];
                        m_accessKey = parsedConnectionString[2];

                        Trace("Opc.Ua.Publisher.Module: Publisher configured for device: " + m_deviceName);

                        // try to connect to IoT Hub
                        using (HttpClient httpClient = new HttpClient())
                        {
                            httpClient.BaseAddress = new UriBuilder { Scheme = "https", Host = _IoTHubName }.Uri;

                            string sharedAccessToken = IoTHubRegistration.GenerateSharedAccessToken(string.Empty, Convert.FromBase64String(m_accessKey), _IoTHubName, 60000);
                            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SharedAccessSignature", sharedAccessToken);

                            // send an empty d2c message
                            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/devices/" + m_deviceName + "/messages/events" + IoTHubRegistration._IoTHubAPIVersion);
                            HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
                            if (response.StatusCode != HttpStatusCode.NoContent)
                            {
                                throw new Exception("Opc.Ua.Publisher.Module: Could not connect to IoT Hub. Response: " + response.ToString());
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Opc.Ua.Publisher.Module: Publisher configuration failed!");
                    }
                }
                else
                {
                    Trace("Opc.Ua.Publisher.Module: Connection string not found in secure store.");
                }
            }
        }

        /// <summary>
        /// Publish message to bus
        /// </summary>
        public static void Publish(Message message)
        {
            if (m_broker != null)
            {
                m_broker.Publish(message);
            }
        }

        /// <summary>
        /// Called when gateway starts, establishes the connections to endpoints
        /// </summary>
        public void Start()
        {
            Trace("Opc.Ua.Publisher.Module: Starting...");

            // subscribe to preconfigured nodes
            Trace("Opc.Ua.Publisher.Module: Attemping to subscribe to published nodes...");
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
                        Trace("Opc.Ua.Publisher.Module: Unexpected error publishing node: " + ex.Message + "\r\nIgnoring node: " + nodeLookup.EndPointURL.AbsoluteUri + ", " + nodeLookup.NodeID.ToString());
                    }
                }
            }
            Trace("Opc.Ua.Publisher.Module: Started.");
        }

        /// <summary>
        /// Registers ourselves with IoTHub so we can send messages to it 
        /// </summary>
        private void SelfRegisterWithIoTHub(string ownerConnectionString)
        {
            string[] parsedConnectionString = IoTHubRegistration.ParseConnectionString(ownerConnectionString, false);
            string deviceConnectionString = string.Empty;
            if ((parsedConnectionString != null) && (parsedConnectionString.Length == 3))
            {
                string IoTHubName = parsedConnectionString[0];
                string name = parsedConnectionString[1];
                string accessToken = parsedConnectionString[2];

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new UriBuilder { Scheme = "https", Host = IoTHubName }.Uri;

                    string sharedAccessSignature = IoTHubRegistration.GenerateSharedAccessToken(name, Convert.FromBase64String(accessToken), IoTHubName, 60000);
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SharedAccessSignature", sharedAccessSignature);
                    deviceConnectionString = IoTHubRegistration.CreateDeviceInIoTHubDeviceRegistry(httpClient, m_configuration.ApplicationName.Replace(" ", "")).Result;

                    // prepend the rest of the connection string
                    deviceConnectionString = "HostName=" + IoTHubName + ";DeviceId=" + m_configuration.ApplicationName.Replace(" ", "") + ";SharedAccessKey=" + deviceConnectionString;
                    SecureIoTHubToken.Write(m_configuration.ApplicationName, deviceConnectionString);
                }
            }
            else
            {
                Trace("Opc.Ua.Publisher.Module: Could not parse IoT Hub owner connection string: " + ownerConnectionString);
            }
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
                Trace("Opc.Ua.Publisher.Module: Created session with updated endpoint " + selectedEndpoint.EndpointUrl + " from server!");
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

                monitoredItem.Notification += new MonitoredItemNotificationEventHandler(Module.MonitoredItem_Notification);
                subscription.AddItem(monitoredItem);
                subscription.ApplyChanges();
            }
            else
            {
                Trace("Opc.Ua.Publisher.Module: ERROR: Could not find endpoint URL " + nodeLookup.EndPointURL.ToString() + " in active server sessions, NodeID " + nodeLookup.NodeID.Identifier.ToString() + " NOT published!");
                Trace("Opc.Ua.Publisher.Module: To fix this, please update your publishednodes.json file with the updated endpoint URL!");
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
                
                // publish
                var properties = new Dictionary<string, string>();
                properties.Add("content-type", "application/opcua+uajson");
                properties.Add("deviceName", m_deviceName);

                if (m_accessKey != null)
                {
                    properties.Add("source", "mapping");
                    properties.Add("deviceKey", m_accessKey);
                }

                try
                {
                    Publish(new Message(json, properties));
                }
                catch (Exception ex)
                {
                    Trace("Opc.Ua.Publisher.Module: Failed to publish message, dropping...");
                    Trace(ex.ToString());
                }
            }
            catch (Exception exception)
            {
                Trace("Opc.Ua.Publisher.Module: Error processing monitored item notification: " + exception.ToString());
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
                    Trace("Opc.Ua.Publisher.Module: Could not fetch endpoints from url: " + discoveryUrl.ToString());
                    Trace("Opc.Ua.Publisher.Module: Reason = " + e.Message);
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
        private void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                Trace("Opc.Ua.Publisher.Module: Certificate \""
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
