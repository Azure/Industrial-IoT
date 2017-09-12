
using IoTHubCredentialTools;
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
    using static Program;
    using static Opc.Ua.Workarounds.TraceWorkaround;

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
                Trace("PublishNodeMethod: Invalid Arguments when trying to publish a node.");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments!");
            }

            PublishNodeConfig publishNodeConfig;
            NodeId nodeId = null;
            Uri endpointUri = null;
            try
            {
                nodeId = inputArguments[0] as string;
                endpointUri = inputArguments[1] as Uri;
                if (string.IsNullOrEmpty(inputArguments[0] as string) || string.IsNullOrEmpty(inputArguments[1] as string))
                {
                    Trace($"PublishNodeMethod: Arguments (0 (nodeId), 1 (endpointUrl)) are not valid strings!");
                    return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments as strings!");
                }
                publishNodeConfig = new PublishNodeConfig(nodeId, endpointUri, OpcSamplingInterval, OpcPublishingInterval);
            }
            catch (UriFormatException)
            {
                Trace($"PublishNodeMethod: The endpointUri is invalid '{inputArguments[1] as string}'!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA endpoint URL as second argument!");
            }

            // find/create a session to the endpoint URL and start monitoring the node.
            try
            {
                // find the session we need to monitor the node
                OpcSession opcSession = null;
                try
                {
                    OpcSessionsSemaphore.Wait();
                    try
                    {
                        opcSession = OpcSessions.FirstOrDefault(s => s.EndpointUri == publishNodeConfig.EndpointUri);
                    }
                    catch
                    {
                        opcSession = null;
                    }

                    // add a new session.
                    if (opcSession == null)
                    {
                        // create new session info.
                        opcSession = new OpcSession(publishNodeConfig.EndpointUri, OpcSessionCreationTimeout);
                        OpcSessions.Add(opcSession);
                        Trace($"PublishNodeMethod: No matching session found for endpoint '{publishNodeConfig.EndpointUri.AbsolutePath}'. Requested to create a new one.");
                    }
                    else
                    {
                        Trace($"PublishNodeMethod: Session found for endpoint '{publishNodeConfig.EndpointUri.AbsolutePath}'");
                    }
                }
                finally
                {
                    OpcSessionsSemaphore.Release();
                }

                // add the node info to the subscription with the default publishing interval
                opcSession.AddNodeForMonitoring(OpcPublishingInterval, publishNodeConfig.NodeId);
                Trace("PublishNodeMethod: Requested to monitor item.");

                // start monitoring the node
                Task monitorTask = Task.Run(async () => await opcSession.ConnectAndMonitor());
                monitorTask.Wait();
                Trace("PublishNodeMethod: Session processing completed.");

                // update our data
                PublishConfig.Add(publishNodeConfig);

                // add it also to the publish file 
                var publishConfigFileEntry = new PublishConfigFileEntry()
                {
                    EndpointUri = endpointUri,
                    NodeId = nodeId
                };
                PublishConfigFileEntries.Add(publishConfigFileEntry);
                File.WriteAllText(NodesToPublishAbsFilename, JsonConvert.SerializeObject(PublishConfigFileEntries));

                Trace($"PublishNodeMethod: Now publishing: {publishNodeConfig.NodeId.ToString()}");
                return ServiceResult.Good;
            }
            catch (Exception e)
            {
                Trace(e, $"PublishNodeMethod: Exception while trying to configure publishing node '{publishNodeConfig.NodeId.ToString()}'");
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

            NodeId nodeId = null;
            Uri endpointUri = null;
            try
            {
                nodeId = inputArguments[0] as string;
                endpointUri = inputArguments[1] as Uri;
                if (string.IsNullOrEmpty(inputArguments[0] as string) || string.IsNullOrEmpty(inputArguments[1] as string))
                {
                    Trace($"UnPublishNodeMethod: Arguments (0 (nodeId), 1 (endpointUrl)) are not valid strings!");
                    return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments as strings!");
                }
            }
            catch (UriFormatException)
            {
                Trace($"UnPublishNodeMethod: The endpointUrl is invalid '{inputArguments[1] as string}'!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA endpoint URL as second argument!");
            }

            // find the session and stop monitoring the node.
            try
            {
                // find the session we need to monitor the node
                OpcSession opcSession = null;
                try
                {
                    OpcSessionsSemaphore.Wait();
                    opcSession = OpcSessions.FirstOrDefault(s => s.EndpointUri == endpointUri);
                }
                catch
                {
                    opcSession = null;
                }
                finally
                {
                    OpcSessionsSemaphore.Release();

                }
                if (opcSession == null)
                {
                    // do nothing if there is no session for this endpoint.
                    Trace($"UnPublishNodeMethod: Session for endpoint '{endpointUri.AbsolutePath}' not found.");
                    return ServiceResult.Create(StatusCodes.BadSessionIdInvalid, "Session for endpoint of published node not found!");
                }
                else
                {
                    Trace($"UnPublishNodeMethod: Session found for endpoint '{endpointUri.AbsolutePath}'");
                }

                // remove the node from the sessions monitored items list.
                opcSession.TagNodeForMonitoringStop(nodeId);
                Trace("UnPublishNodeMethod: Requested to stop monitoring of node.");

                // stop monitoring the node
                Task monitorTask = Task.Run(async () => await opcSession.ConnectAndMonitor());
                monitorTask.Wait();
                Trace("UnPublishNodeMethod: Session processing completed.");

                // remove node from persisted config file
                var entryToRemove = PublishConfigFileEntries.Find(l => l.NodeId == nodeId && l.EndpointUri == endpointUri);
                PublishConfigFileEntries.Remove(entryToRemove);
                File.WriteAllText(NodesToPublishAbsFilename, JsonConvert.SerializeObject(PublishConfigFileEntries));
            }
            catch (Exception e)
            {
                Trace(e, $"DoPublish: Exception while trying to configure publishing node '{nodeId.ToString()}'");
                return ServiceResult.Create(e, StatusCodes.BadUnexpectedError, $"Unexpected error publishing node: {e.Message}");
            }
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to get the list of published nodes.
        /// </summary>
        private ServiceResult GetListOfPublishedNodesMethod(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            outputArguments[0] = JsonConvert.SerializeObject(PublishConfigFileEntries);
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
