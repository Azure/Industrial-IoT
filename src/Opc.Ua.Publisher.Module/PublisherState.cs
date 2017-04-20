
using IoTHubCredentialTools;
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
                Module.Trace("PublishNodeMethod: Invalid Arguments!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments!");
            }

            string nodeID = inputArguments[0] as string;
            string uri = inputArguments[1] as string;
            if (string.IsNullOrEmpty(nodeID) || string.IsNullOrEmpty(uri))
            {
                Module.Trace("PublishNodeMethod: Arguments are not valid strings!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments as strings!");
            }

            NodeLookup lookup = new NodeLookup();
            lookup.NodeID = new NodeId(nodeID);
            try
            {
                lookup.EndPointURL = new Uri(uri);
            }
            catch (UriFormatException)
            {
                Module.Trace("PublishNodeMethod: Invalid endpoint URL!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA endpoint URL as second argument!");
            }

            // create session, if it doesn't exist already and complete asynchonourly (do to thread dependencies in the UA stack)
            if (!Module.m_endpointUrls.Contains(lookup.EndPointURL))
            {
                try
                {
                    Task.Run(() =>
                    {
                        Module.Trace("PublishNodeMethod: Session not found, creating one for " + lookup.EndPointURL);
                        Module.EndpointConnect(lookup.EndPointURL).Wait();
                        Module.Trace("PublishNodeMethod: Session created.");

                        return DoPublish(lookup);
                    });

                    return ServiceResult.Create(StatusCodes.GoodCompletesAsynchronously, "Publishing takes a while, please be patient!");
                }
                catch (Exception ex)
                {
                    Module.Trace("PublishNodeMethod: Exception: " + ex.ToString());
                    return ServiceResult.Create(ex, StatusCodes.BadUnexpectedError, "Unexpected error publishing node: " + ex.Message);
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
                foreach (Session session in Module.m_sessions)
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
                    Module.Trace("PublishNodeMethod: No matching session found for " + lookup.EndPointURL.ToString());
                    return ServiceResult.Create(StatusCodes.BadSessionIdInvalid, "Session for published node not found!");
                }
                Module.Trace("PublishNodeMethod: Session found.");


                // check if the node has already been published
                foreach (MonitoredItem item in matchingSession.DefaultSubscription.MonitoredItems)
                {
                    if (item.StartNodeId == lookup.NodeID)
                    {
                        Module.Trace("PublishNodeMethod: Node ID has already been published " + lookup.NodeID.ToString());
                        return ServiceResult.Create(StatusCodes.BadNodeIdExists, "Node has already been published!");
                    }
                }

                // subscribe to the node
                Module.CreateMonitoredItem(lookup);
                Module.Trace("PublishNodeMethod: Monitored item created.");

                // update our data
                Module.m_nodesLookups.Add(lookup);
                if (!Module.m_endpointUrls.Contains(lookup.EndPointURL))
                {
                    Module.m_endpointUrls.Add(lookup.EndPointURL);
                }

                //serialize Program.m_nodesLookups to disk
                string publishedNodesFilePath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "publishednodes.json";
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_PNFP")))
                {
                    publishedNodesFilePath = Environment.GetEnvironmentVariable("_GW_PNFP");
                }
                File.WriteAllText(publishedNodesFilePath, JsonConvert.SerializeObject(Module.m_nodesLookups));

                Module.Trace("PublishNodeMethod: Successful publish: " + lookup.ToString());
                return ServiceResult.Good;
            }
            catch (Exception ex)
            {
                Module.Trace("PublishNodeMethod: Exception: " + ex.ToString());
                return ServiceResult.Create(ex, StatusCodes.BadUnexpectedError, "Unexpected error publishing node: " + ex.Message);
            }
        }

        /// <summary>
        /// Method exposed as a node in the server to un-publish a node from IoT Hub that it is connected to
        /// </summary>
        private ServiceResult UnPublishNodeMethod(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (inputArguments[0] == null || inputArguments[1] == null)
            {
                Module.Trace("UnPublishNodeMethod: Invalid arguments!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments!");
            }

            string nodeID = inputArguments[0] as string;
            string uri = inputArguments[1] as string;
            if (string.IsNullOrEmpty(nodeID) || string.IsNullOrEmpty(uri))
            {
                Module.Trace("UnPublishNodeMethod: Arguments are not valid strings!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments as strings!");
            }

            NodeLookup lookup = new NodeLookup();
            lookup.NodeID = new NodeId(nodeID);
            try
            {
                lookup.EndPointURL = new Uri(uri);
            }
            catch (UriFormatException)
            {
                Module.Trace("UnPublishNodeMethod: Invalid endpoint URL!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA endpoint URL as second argument!");
            }
            
            // find the right session using our lookup
            Session matchingSession = null;
            foreach (Session session in Module.m_sessions)
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
                Module.Trace("UnPublishNodeMethod: Session for published node not found: " + lookup.EndPointURL.ToString());
                return ServiceResult.Create(StatusCodes.BadSessionIdInvalid, "Session for published node not found!");
            }

            // find the right monitored item to remove
            foreach (MonitoredItem item in matchingSession.DefaultSubscription.MonitoredItems)
            {
                if (item.StartNodeId == lookup.NodeID)
                {
                    matchingSession.DefaultSubscription.RemoveItem(item);
                    Module.Trace("UnPublishNodeMethod: Successful unpublish: " + lookup.NodeID.ToString());

                    // update our data on success only
                    // we keep the session to the server, as there may be other nodes still published on it
                    Module.m_nodesLookups.Remove(lookup);

                    //serialize Program.m_nodesLookups to disk
                    string publishedNodesFilePath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "publishednodes.json";
                    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_PNFP")))
                    {
                        publishedNodesFilePath = Environment.GetEnvironmentVariable("_GW_PNFP");
                    }
                    File.WriteAllText(publishedNodesFilePath, JsonConvert.SerializeObject(Module.m_nodesLookups));

                    return ServiceResult.Good;
                }
            }
            
            Module.Trace("UnPublishNodeMethod: Monitored item for node ID not found " + lookup.NodeID.ToString());
            return ServiceResult.Create(StatusCodes.BadNodeIdInvalid, "Monitored item for node ID not found!");
        }

        /// <summary>
        /// Method exposed as a node in the server to get a list of published nodes
        /// </summary>
        private ServiceResult GetListOfPublishedNodesMethod(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            outputArguments[0] = JsonConvert.SerializeObject(Module.m_nodesLookups);
            Module.Trace("GetListOfPublishedNodesMethod: Success!");

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
                Module.Trace("ConnectionStringWrite: Invalid Argument!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments as strings!");
            }

            statusCode = StatusCodes.Bad;
            timestamp = DateTime.Now;

            // read current connection string and compare to the one passed in
            string currentConnectionString = SecureIoTHubToken.Read(Module.m_configuration.ApplicationName);
            if (string.Equals(connectionString, currentConnectionString, StringComparison.OrdinalIgnoreCase))
            {
                Module.Trace("ConnectionStringWrite: Connection string up to date!");
                return ServiceResult.Create(StatusCodes.Bad, "Connection string already up-to-date!");
            }

            // try to parse connection string
            string[] parsedConnectionString = IoTHubRegistration.ParseConnectionString(connectionString, true);
            if ((parsedConnectionString == null) || (parsedConnectionString.Length != 3))
            {
                Module.Trace("ConnectionStringWrite: Connection string parsing error: " + connectionString);
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Could not parse connection string!");
            }

            // write connection string
            try
            {
                SecureIoTHubToken.Write(Module.m_configuration.ApplicationName, connectionString);
            }
            catch (Exception ex)
            {
                statusCode = StatusCodes.Bad;
                Module.Trace("ConnectionStringWrite: Exception: " + ex.ToString());
                return ServiceResult.Create(StatusCodes.Bad, "Publisher registration failed: " + ex.Message);
            }
            
            try
            {
                // try to configure our publisher component
                Module.TryConfigurePublisherAsync().Wait();
            }
            catch (Exception ex)
            {
                statusCode = StatusCodes.Bad;
                Module.Trace("ConnectionStringWrite: Exception: " + ex.ToString());
                return ServiceResult.Create(StatusCodes.Bad, "Publisher configuration failed: " + ex.Message);
            }

            statusCode = StatusCodes.Good;
            Module.Trace("ConnectionStringWrite: Success!");

            return statusCode;
        }
    }
}
