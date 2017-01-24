// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Azure.IoT.Gateway;
using Opc.Ua.Configuration;
using Newtonsoft.Json;

namespace Opc.Ua.Client
{
    [DataContract(Name = "NodeLookup", Namespace = Namespaces.OpcUaXsd)]
    public partial class NodeLookup
    {
        public NodeLookup()
        {
        }

        [DataMember(Name = "EndpointUrl", IsRequired = true, Order = 0)]
        public Uri EndPointURL;

        [DataMember(Name = "NodeId", IsRequired = true, Order = 1)]
        public NodeId NodeID;
    }

    [CollectionDataContract(Name = "ListOfPublishedNodes", Namespace = Namespaces.OpcUaConfig, ItemName = "NodeLookup")]
    public partial class PublishedNodesCollection : List<NodeLookup>
    {
        public PublishedNodesCollection()
        {
        }

        public static PublishedNodesCollection Load(ApplicationConfiguration configuration)
        {
            return configuration.ParseExtension<PublishedNodesCollection>();
        }
    }

    public class SampleModule : IGatewayModule, IGatewayModuleStart
    {
        private Broker m_broker;

        private ApplicationConfiguration m_configuration = null;
        private List<Session> m_sessions = new List<Session>();

        private string m_DeviceID = string.Empty;
        private string m_SharedAccessKey = string.Empty;

        public async void Create(Broker broker, byte[] configuration)
        {
            m_broker = broker;

            // TODO: Security: The shared access key should be stored in secure storage, e.g. a TPM
            // and the device ID can be used as a lookup
            string configurationString = JsonConvert.DeserializeObject<string>(Encoding.UTF8.GetString(configuration));
            m_DeviceID = configurationString.Substring(0, configurationString.IndexOf(';'));
            m_SharedAccessKey = configurationString.Substring(configurationString.IndexOf(';') + 1);

            // load the application configuration.
            ApplicationInstance application = new ApplicationInstance();
            application.ConfigSectionName = "Opc.Ua.Client.SampleModule";
            m_configuration = await application.LoadApplicationConfiguration(false);

            // check the application certificate.
            await application.CheckApplicationInstanceCertificate(false, 0);
            
            m_configuration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
            
            // get a list of persisted endpoint URLs and create a session for each.
            List<Uri> endpointUrls = new List<Uri>();
            PublishedNodesCollection nodesLookups = PublishedNodesCollection.Load(m_configuration);
            foreach (NodeLookup nodeLookup in nodesLookups)
            {
                if (!endpointUrls.Contains(nodeLookup.EndPointURL))
                {
                    endpointUrls.Add(nodeLookup.EndPointURL);
                }
            }

            try
            {
                List<Task> connectionAttempts = new List<Task>();
                foreach (Uri endpointUrl in endpointUrls)
                {
                    connectionAttempts.Add(EndpointConnect(endpointUrl));
                }

                // Wait for all sessions to be connected
                Task.WaitAll(connectionAttempts.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Opc.Ua.Client.SampleModule: Exception: " + ex.ToString() + "\r\n" + ex.InnerException != null? ex.InnerException.ToString() : null );
            }   
                 
            Console.WriteLine("Opc.Ua.Client.SampleModule: OPC UA Client Sample Module created.");
        }

        private async Task EndpointConnect(Uri endpointUrl)
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
                Console.WriteLine("Opc.Ua.Client.SampleModule: Created session with updated endpoint " + configuredEndpoint.EndpointUrl + " from server!");
                newSession.KeepAlive += new KeepAliveEventHandler(StandardClient_KeepAlive);
                m_sessions.Add(newSession);
            }
        }

        public void Start()
        {
            // publish preconfigured nodes
            PublishedNodesCollection nodesLookups = PublishedNodesCollection.Load(m_configuration);
            foreach (NodeLookup nodeLookup in nodesLookups)
            {
                CreateMonitoredItem(nodeLookup);
            }

            Console.WriteLine("Opc.Ua.Client.SampleModule: OPC UA Client Sample Module started.");
        }

        public void Destroy()
        {
            foreach (Session session in m_sessions)
            {
                // Disconnect and dispose
                session.Dispose();
            }

            m_sessions.Clear();

            Console.WriteLine("Opc.Ua.Client.SampleModule: OPC UA Client Sample Module destroyed.");
        }

        public void Receive(Message received_message)
        {
            // Nothing to do, we only send!
        }

        public void CreateMonitoredItem(NodeLookup nodeLookup)
        {
            // find the right session using our lookup
            Session matchingSession = null;
            foreach(Session session in m_sessions)
            {
                if (session.Endpoint.EndpointUrl.ToLowerInvariant().TrimEnd('/') == Utils.ReplaceLocalhost(nodeLookup.EndPointURL.ToString()).ToLowerInvariant().TrimEnd('/'))
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

                // add the new monitored item.
                MonitoredItem monitoredItem = new MonitoredItem(subscription.DefaultItem);

                monitoredItem.StartNodeId = nodeLookup.NodeID;
                monitoredItem.AttributeId = Attributes.Value;
                monitoredItem.DisplayName = nodeLookup.NodeID.Identifier.ToString();
                monitoredItem.MonitoringMode = MonitoringMode.Reporting;
                monitoredItem.SamplingInterval = 0;
                monitoredItem.QueueSize = 0;
                monitoredItem.DiscardOldest = true;

                monitoredItem.Notification += new MonitoredItemNotificationEventHandler(MonitoredItem_Notification);
                subscription.AddItem(monitoredItem);
                subscription.ApplyChanges();
            }
            else
            {
                Console.WriteLine("Opc.Ua.Client.SampleModule: ERROR: Could not find endpoint URL " + nodeLookup.EndPointURL.ToString() + " in active server sessions, NodeID " + nodeLookup.NodeID.Identifier.ToString() + " NOT published!");
                Console.WriteLine("Opc.Ua.Client.SampleModule: To fix this, please update your config.xml with the updated enpoint URL!");
            }
        }

        private void MonitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            try
            {
                if (e.NotificationValue == null || monitoredItem.Subscription.Session == null)
                {
                    return;
                }

                JsonEncoder encoder = new JsonEncoder(monitoredItem.Subscription.Session.MessageContext, false);
                string hostname = monitoredItem.Subscription.Session.ConfiguredEndpoint.EndpointUrl.DnsSafeHost;
                if (hostname == "localhost")
                {
                    hostname = Utils.GetHostName();
                }
                encoder.WriteString("HostName", hostname);
                encoder.WriteNodeId("MonitoredItem", monitoredItem.ResolvedNodeId);
                e.NotificationValue.Encode(encoder);

                string json = encoder.Close();

                var properties = new Dictionary<string, string>();
                properties.Add("source", "mapping");
                properties.Add("content-type", "application/opcua+uajson");
                properties.Add("deviceName", m_DeviceID);
                properties.Add("deviceKey", m_SharedAccessKey);

                try
                {
                    m_broker.Publish(new Message(json, properties));
                }
                catch (Exception ex)
                {
                    Utils.Trace(ex, "Opc.Ua.Client.SampleModule: Failed to publish message, dropping....");
                }

            }
            catch (Exception exception)
            {
                Utils.Trace(exception, "Opc.Ua.Client.SampleModule: Error processing monitored item notification.");
            }
        }
            
        private void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                e.Accept = true;
                Console.WriteLine("Opc.Ua.Client.SampleModule: WARNING: Auto-accepting certificate: {0}", e.Certificate.Subject);
            }
        }

        private void StandardClient_KeepAlive(Session sender, KeepAliveEventArgs e)
        {
            if (e != null && sender != null)
            {
                if (!ServiceResult.IsGood(e.Status))
                {
                    Console.WriteLine(String.Format(
                        "Opc.Ua.Client.SampleModule: Server Status NOT good: {0} {1}/{2}", e.Status,
                        sender.OutstandingRequestCount,
                        sender.DefunctRequestCount));
                }
            }
        }

        private EndpointDescriptionCollection DiscoverEndpoints(ApplicationConfiguration config, Uri discoveryUrl, int timeout)
        {
            // use a short timeout.
            EndpointConfiguration configuration = EndpointConfiguration.Create(config);
            configuration.OperationTimeout = timeout;

            using (DiscoveryClient client = DiscoveryClient.Create(
                discoveryUrl,
                EndpointConfiguration.Create(config)))
            {
                try
                {
                    EndpointDescriptionCollection endpoints = client.GetEndpoints(null);
                    ReplaceLocalHostWithRemoteHost(endpoints, discoveryUrl);
                    return endpoints;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Opc.Ua.Client.SampleModule: Could not fetch endpoints from url: {0}", discoveryUrl);
                    Console.WriteLine("Opc.Ua.Client.SampleModule: Reason = {0}", e.Message);
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

        private EndpointDescription SelectUaTcpEndpoint(EndpointDescriptionCollection endpointCollection)
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
    }
}
