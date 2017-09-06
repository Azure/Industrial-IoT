
using IoTHubCredentialTools;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Publisher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Publisher
{
    using System.Threading.Tasks;
    using static Opc.Ua.Utils;
    using static Program;

    public partial class PublisherState
    {
        /// <summary>
        /// Called after the class is created.
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
        /// Method to start monitoring a node and publish the data to IoTHub.
        /// </summary>
        private ServiceResult PublishNodeMethod(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (inputArguments[0] == null || inputArguments[1] == null)
            {
                Trace("PublishNodeMethod: Invalid Arguments!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments!");
            }

            NodeToPublish nodeToPublish;
            string nodeId = inputArguments[0] as string;
            string endpointUrl = inputArguments[1] as string;
            if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(endpointUrl))
            {
                Trace($"PublishNodeMethod: Arguments (0 (nodeId): '{nodeId}', 1 (endpointUrl):'{endpointUrl}') are not valid strings!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments as strings!");
            }

            try
            {
                nodeToPublish = new NodeToPublish(nodeId, endpointUrl);
            }
            catch (UriFormatException)
            {
                Trace($"PublishNodeMethod: The endpointUrl is invalid (0 (nodeId): '{nodeId}', 1 (endpointUrl):'{endpointUrl}')!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA endpoint URL as second argument!");
            }

            // find/create a session to the endpoint URL and start monitoring the node.
            try
            {
                // find the session we need to monitor the node
                OpcSession opcSession = OpcSessions.First(s => s.EndpointUri == nodeToPublish.EndPointUri);

                // add a new session.
                if (opcSession == null)
                {
                    // create new session info.
                    opcSession = new OpcSession(nodeToPublish.EndPointUri, OpcSessionCreationTimeout);
                    OpcSessions.Add(opcSession);
                    Trace($"DoPublish: No matching session found for endpoint '{nodeToPublish.EndPointUri.AbsolutePath}'. Requested to create a new one.");
                }
                else
                {
                    Trace($"DoPublish: Session found for endpoint '{nodeToPublish.EndPointUri.AbsolutePath}'");
                }

                // add the node info to the subscription with the default publishing interval
                opcSession.AddNodeForMonitoring(OpcPublishingInterval, nodeToPublish.NodeId);
                Trace("DoPublish: Requested to monitor item.");

                // start monitoring the node
                Task monitorTask = Task.Run(async () => await opcSession.ConnectAndMonitor());
                monitorTask.Wait();
                Trace("DoPublish: Session processing completed.");

                // update our data
                NodesToPublish.Add(nodeToPublish);

                // persist it to disk
                File.WriteAllText(NodesToPublishAbsFilename, JsonConvert.SerializeObject(NodesToPublish));

                Trace($"DoPublish: Now publishing: {nodeToPublish.ToString()}");
                return ServiceResult.Good;
            }
            catch (Exception e)
            {
                Trace(e, $"DoPublish: Exception while trying to configure publishing node '{nodeToPublish.ToString()}'");
                return ServiceResult.Create(e, StatusCodes.BadUnexpectedError, $"Unexpected error publishing node: {e.Message}");
            }
        }

        /// <summary>
        /// Method to remove the node from the subscription and stop publishing telemetry to IoTHub.
        /// </summary>
        private ServiceResult UnPublishNodeMethod(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (inputArguments[0] == null || inputArguments[1] == null)
            {
                Trace("UnPublishNodeMethod: Invalid arguments!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments!");
            }

            string nodeId = inputArguments[0] as string;
            string endpointUrl = inputArguments[1] as string;
            if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(endpointUrl))
            {
                Trace($"UnPublishNodeMethod: Arguments (0 (nodeId): '{nodeId}', 1 (endpointUrl):'{endpointUrl}') are not valid strings!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments as strings!");
            }

            NodeToPublish nodeToUnpublish = new NodeToPublish();
            try
            {
                nodeToUnpublish = new NodeToPublish(nodeId, endpointUrl);
            }
            catch (UriFormatException)
            {
                Trace($"UnPublishNodeMethod: The endpointUrl is invalid (0 (nodeId): '{nodeId}', 1 (endpointUrl):'{endpointUrl}')!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA endpoint URL as second argument!");
            }

            // find the session and stop monitoring the node.
            try
            {
                // find the session we need to monitor the node
                OpcSession opcSession = OpcSessions.First(s => s.EndpointUri == nodeToUnpublish.EndPointUri);
                if (opcSession == null)
                {
                    // do nothing if there is no session for this endpoint.
                    Trace($"UnPublishNodeMethod: Session for endpoint '{nodeToUnpublish.EndPointUri.AbsolutePath}' not found.");
                    return ServiceResult.Create(StatusCodes.BadSessionIdInvalid, "Session for endpoint of published node not found!");
                }
                else
                {
                    Trace($"UnPublishNodeMethod: Session found for endpoint '{nodeToUnpublish.EndPointUri.AbsolutePath}'");
                }

                // remove the node from the sessions monitored items list.
                opcSession.TagNodeForMonitoringStop(nodeToUnpublish.NodeId);
                Trace("UnPublishNodeMethod: Requested to stop monitoring of node.");

                // stop monitoring the node
                Task monitorTask = Task.Run(async () => await opcSession.ConnectAndMonitor());
                monitorTask.Wait();
                Trace("UnPublishNodeMethod: Session processing completed.");

                // remove node from our persisted data set.
                var itemToRemove = NodesToPublish.Find(l => l.NodeId == nodeToUnpublish.NodeId && l.EndPointUri == nodeToUnpublish.EndPointUri);
                NodesToPublish.Remove(itemToRemove);

                // persist data
                File.WriteAllText(NodesToPublishAbsFilename, JsonConvert.SerializeObject(NodesToPublish));
            }
            catch (Exception e)
            {
                Trace(e, $"DoPublish: Exception while trying to configure publishing node '{nodeToUnpublish.ToString()}'");
                return ServiceResult.Create(e, StatusCodes.BadUnexpectedError, $"Unexpected error publishing node: {e.Message}");
            }
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to get the list of published nodes.
        /// </summary>
        private ServiceResult GetListOfPublishedNodesMethod(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            outputArguments[0] = JsonConvert.SerializeObject(NodesToPublish);
            Trace("GetListOfPublishedNodesMethod: Success!");

            return ServiceResult.Good;
        }

        /// <summary>
        /// Data node in the server which registers ourselves with IoT Hub when this node is written.
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
            string currentConnectionString = SecureIoTHubToken.Read(OpcConfiguration.ApplicationName, IotDeviceCertStoreType, IotDeviceCertStorePath);
            if (string.Equals(connectionString, currentConnectionString, StringComparison.OrdinalIgnoreCase))
            {
                Trace("ConnectionStringWrite: Connection string up to date!");
                return ServiceResult.Create(StatusCodes.Bad, "Connection string already up-to-date!");
            }

            Trace($"ConnectionStringWrite: Attempting to configure publisher with connection string: {connectionString}");
           
            // configure publisher and write connection string
            try
            {
                IotHubMessaging.ConnectionStringWrite(connectionString);
            }
            catch (Exception e)
            {
                statusCode = StatusCodes.Bad;
                Trace(e, $"ConnectionStringWrite: Exception while trying to create IoTHub client and store device connection string in cert store");
                return ServiceResult.Create(StatusCodes.Bad, "Publisher registration failed: " + e.Message);
            }
            
            statusCode = StatusCodes.Good;
            Trace("ConnectionStringWrite: Success!");

            return statusCode;
        }
    }
}
