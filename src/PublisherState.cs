
using IoTHubCredentialTools;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Publisher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Publisher
{
    using static Opc.Ua.Utils;

    public partial class PublisherState
    {
        /// <summary>
        /// Called after the class is created
        /// </summary>
        protected override void OnAfterCreate(ISystemContext context, NodeState node)
        {
            base.OnAfterCreate(context, node);

            PublishNode.OnCallMethod = PublishNodeMethod;
            UnPublishNode.OnCallMethod = UnPublishNodeMethod;
            GetListOfPublishedNodes.OnCallMethod = GetListOfPublishedNodesMethod;
            ConnectionString.OnWriteValue = ConnectionStringWrite;
        }

        /// <summary>
        /// Method exposed as a node in the server to publish a node to IoT Hub that it is connected to
        /// </summary>
        private ServiceResult PublishNodeMethod(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (inputArguments[0] == null || inputArguments[1] == null)
            {
                Trace("PublishNodeMethod: Invalid Arguments!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments!");
            }

            string nodeID = inputArguments[0] as string;
            string uri = inputArguments[1] as string;
            if (string.IsNullOrEmpty(nodeID) || string.IsNullOrEmpty(uri))
            {
                Trace("PublishNodeMethod: Arguments are not valid strings!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments as strings!");
            }

            NodeLookup lookup = new NodeLookup()
            {
                NodeID = new NodeId(nodeID)
            };
            try
            {
                lookup.EndPointURL = new Uri(uri);
            }
            catch (UriFormatException)
            {
                Trace("PublishNodeMethod: Invalid endpoint URL!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA endpoint URL as second argument!");
            }

            // create session, if it doesn't exist already and complete asynchonourly (do to thread dependencies in the UA stack)
            if (!Program.PublishedNodesEndpointUrls.Contains(lookup.EndPointURL))
            {
                try
                {
                    Task.Run(() =>
                    {
                        Trace($"PublishNodeMethod: Session not found, creating one for endpoint '{lookup.EndPointURL}'");
                        Program.EndpointConnect(lookup.EndPointURL).Wait();
                        Trace("PublishNodeMethod: Session created.");

                        return DoPublish(lookup);
                    });

                    return ServiceResult.Create(StatusCodes.GoodCompletesAsynchronously, "Publishing takes a while, please be patient!");
                }
                catch (Exception ex)
                {
                    Trace(ex, "PublishNodeMethod: Exception while trying to setup publishing");
                    return ServiceResult.Create(ex, StatusCodes.BadUnexpectedError, $"Unexpected error publishing node: {ex.Message}");
                }
            }
            else
            {
                // complete synchonoursly
                return DoPublish(lookup);
            }
        }

        /// <summary>
        /// Publishes a single Nodelookup
        /// </summary>
        private ServiceResult DoPublish(NodeLookup lookup)
        {
            try
            {
                // find the right session using our lookup
                Session matchingSession = null;
                foreach (Session session in Program.OpcSessions)
                {
                    char[] trimChars = { '/', ' ' };
                    if (session.Endpoint.EndpointUrl.TrimEnd(trimChars).StartsWith(lookup.EndPointURL.ToString().TrimEnd(trimChars), StringComparison.OrdinalIgnoreCase))
                    {
                        lookup.EndPointURL = new Uri(session.Endpoint.EndpointUrl);
                        matchingSession = session;
                        break;
                    }
                }

                if (matchingSession == null)
                {
                    Trace($"DoPublish: No matching session found for endpoint '{lookup.EndPointURL.ToString()}'");
                    return ServiceResult.Create(StatusCodes.BadSessionIdInvalid, "Session for published node not found!");
                }
                Trace("DoPublish: Session found.");


                // check if the node has already been published
                foreach (MonitoredItem item in matchingSession.DefaultSubscription.MonitoredItems)
                {
                    if (item.StartNodeId == lookup.NodeID)
                    {
                        Trace($"DoPublish: Node ID '{lookup.NodeID.ToString()}' is already published!");
                        return ServiceResult.Create(StatusCodes.BadNodeIdExists, $"Node ID '{lookup.NodeID.ToString()}' is already published!");
                    }
                }

                // subscribe to the node
                Program.CreateMonitoredItem(lookup);
                Trace("DoPublish: Monitored item created.");

                // update our data
                Program.PublishedNodes.Add(lookup);
                if (!Program.PublishedNodesEndpointUrls.Contains(lookup.EndPointURL))
                {
                    Program.PublishedNodesEndpointUrls.Add(lookup.EndPointURL);
                }

                //serialize Program.m_nodesLookups to disk
                File.WriteAllText(Program.PublishedNodesAbsFilename, JsonConvert.SerializeObject(Program.PublishedNodes));

                Trace($"DoPublish: Now publishing: {lookup.ToString()}");
                return ServiceResult.Good;
            }
            catch (Exception ex)
            {
                Trace(ex, $"DoPublish: Exception while trying to configure publishing node '{lookup.ToString()}'");
                return ServiceResult.Create(ex, StatusCodes.BadUnexpectedError, $"Unexpected error publishing node: {ex.Message}");
            }
        }

        /// <summary>
        /// Method exposed as a node in the server to un-publish a node from IoT Hub that it is connected to
        /// </summary>
        private ServiceResult UnPublishNodeMethod(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (inputArguments[0] == null || inputArguments[1] == null)
            {
                Trace("UnPublishNodeMethod: Invalid arguments!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments!");
            }

            string nodeID = inputArguments[0] as string;
            string uri = inputArguments[1] as string;
            if (string.IsNullOrEmpty(nodeID) || string.IsNullOrEmpty(uri))
            {
                Trace("UnPublishNodeMethod: Arguments are not valid strings!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments as strings!");
            }

            NodeLookup lookup = new NodeLookup()
            {
                NodeID = new NodeId(nodeID)
            };
            try
            {
                lookup.EndPointURL = new Uri(uri);
            }
            catch (UriFormatException)
            {
                Trace("UnPublishNodeMethod: Invalid endpoint URL!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA endpoint URL as second argument!");
            }
            
            // find the right session using our lookup
            Session matchingSession = null;
            foreach (Session session in Program.OpcSessions)
            {
                char[] trimChars = { '/', ' ' };
                if (session.Endpoint.EndpointUrl.TrimEnd(trimChars).Equals(lookup.EndPointURL.ToString().TrimEnd(trimChars), StringComparison.OrdinalIgnoreCase))
                {
                    matchingSession = session;
                    break;
                }
            }

            if (matchingSession == null)
            {
                Trace($"UnPublishNodeMethod: Session for published node with NodeId '{lookup.EndPointURL.ToString()}' not found.");
                return ServiceResult.Create(StatusCodes.BadSessionIdInvalid, "Session for published node not found!");
            }

            // find the right monitored item to remove
            foreach (MonitoredItem item in matchingSession.DefaultSubscription.MonitoredItems)
            {
                if (item.StartNodeId == lookup.NodeID)
                {
                    matchingSession.DefaultSubscription.RemoveItem(item);
                    Trace($"UnPublishNodeMethod: Stopping publishing of '{lookup.NodeID.ToString()}'");

                    // update our data on success only
                    // we keep the session to the server, as there may be other nodes still published on it
                    var itemToRemove = Program.PublishedNodes.Find(l => l.NodeID == lookup.NodeID && l.EndPointURL == lookup.EndPointURL);
                    Program.PublishedNodes.Remove(itemToRemove);

                    //serialize Program.m_nodesLookups to disk
                    File.WriteAllText(Program.PublishedNodesAbsFilename, JsonConvert.SerializeObject(Program.PublishedNodes));

                    return ServiceResult.Good;
                }
            }

            Trace($"UnPublishNodeMethod: Monitored item for NodeID '{lookup.NodeID.ToString()}' not found ");
            return ServiceResult.Create(StatusCodes.BadNodeIdInvalid, $"Monitored item for NodeID '{lookup.NodeID.ToString()}' not found!");
        }

        /// <summary>
        /// Method exposed as a node in the server to get a list of published nodes
        /// </summary>
        private ServiceResult GetListOfPublishedNodesMethod(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            outputArguments[0] = JsonConvert.SerializeObject(Program.PublishedNodes);
            Trace("GetListOfPublishedNodesMethod: Success!");

            return ServiceResult.Good;
        }

        /// <summary>
        /// Data node in the server which registers ourselves with IoT Hub when this node is written to
        /// </summary>
        public ServiceResult ConnectionStringWrite(ISystemContext context, NodeState node, NumericRange indexRange, QualifiedName dataEncoding, ref object value, ref StatusCode statusCode, ref DateTime timestamp)
        {
            var connectionString = value as string;
            if (string.IsNullOrEmpty(connectionString))
            {
                Trace("ConnectionStringWrite: Invalid Argument!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments as strings!");
            }

            statusCode = StatusCodes.Bad;
            timestamp = DateTime.Now;

            // read current connection string and compare to the one passed in
            string currentConnectionString = SecureIoTHubToken.Read(Program.OpcConfiguration.ApplicationName, Program.IotDeviceCertStoreType, Program.IotDeviceCertStorePath);
            if (string.Equals(connectionString, currentConnectionString, StringComparison.OrdinalIgnoreCase))
            {
                Trace("ConnectionStringWrite: Connection string up to date!");
                return ServiceResult.Create(StatusCodes.Bad, "Connection string already up-to-date!");
            }

            Trace($"ConnectionStringWrite: Attemping to configure publisher with connection string: {connectionString}");
           
            // configure publisher and write connection string
            try
            {
                DeviceClient newClient = DeviceClient.CreateFromConnectionString(connectionString, Program.IotHubProtocol);
                newClient.RetryPolicy = RetryPolicyType.Exponential_Backoff_With_Jitter;
                newClient.OpenAsync().Wait();
                SecureIoTHubToken.Write(Program.OpcConfiguration.ApplicationName, connectionString, Program.IotDeviceCertStoreType, Program.IotDeviceCertStorePath);
                Program.IotHubClient = newClient;
            }
            catch (Exception ex)
            {
                statusCode = StatusCodes.Bad;
                Trace(ex, $"ConnectionStringWrite: Exception while trying to create IoTHub client and store device connection string in cert store");
                return ServiceResult.Create(StatusCodes.Bad, "Publisher registration failed: " + ex.Message);
            }
            
            statusCode = StatusCodes.Good;
            Trace("ConnectionStringWrite: Success!");

            return statusCode;
        }
    }
}
