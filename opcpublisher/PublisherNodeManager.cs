
using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;

namespace OpcPublisher
{
    using Newtonsoft.Json;
    using System.Linq;
    using System.Net;
    using static OpcPublisher.Program;
    using static OpcStackConfiguration;
    using static PublisherNodeConfiguration;

    public class PublisherNodeManager : CustomNodeManager2
    {
        public PublisherNodeManager(Opc.Ua.Server.IServerInternal server, ApplicationConfiguration configuration)
        : base(server, configuration, Namespaces.PublisherApplications)
        {
            SystemContext.NodeIdFactory = this;
        }

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            BaseInstanceState instance = node as BaseInstanceState;

            if (instance != null && instance.Parent != null)
            {
                string id = instance.Parent.NodeId.Identifier as string;

                if (id != null)
                {
                    return new NodeId(id + "_" + instance.SymbolicName, instance.Parent.NodeId.NamespaceIndex);
                }
            }

            return node.NodeId;
        }

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        private FolderState CreateFolder(NodeState parent, string path, string name)
        {
            FolderState folder = new FolderState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                EventNotifier = EventNotifiers.None
            };

            if (parent != null)
            {
                parent.AddChild(folder);
            }

            return folder;
        }


        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.  
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                IList<IReference> references = null;

                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                FolderState root = CreateFolder(null, "OpcPublisher", "OpcPublisher");
                root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, root.NodeId));
                root.EventNotifier = EventNotifiers.SubscribeToEvents;
                AddRootNotifier(root);

                List<BaseDataVariableState> variables = new List<BaseDataVariableState>();

                try
                {
                    FolderState dataFolder = CreateFolder(root, "Data", "Data");

                    const string connectionStringItemName = "ConnectionString";
                    DataItemState item = CreateDataItemVariable(dataFolder, connectionStringItemName, connectionStringItemName, BuiltInType.String, ValueRanks.Scalar, AccessLevels.CurrentWrite);
                    item.Value = String.Empty;

                    FolderState methodsFolder = CreateFolder(root, "Methods", "Methods");

                    MethodState publishNodeMethod = CreateMethod(methodsFolder, "PublishNode", "PublishNode");
                    SetPublishNodeMethodProperties(ref publishNodeMethod);

                    MethodState unpublishNodeMethod = CreateMethod(methodsFolder, "UnpublishNode", "UnpublishNode");
                    SetUnpublishNodeMethodProperties(ref unpublishNodeMethod);

                    MethodState getPublishedNodesLegacyMethod = CreateMethod(methodsFolder, "GetPublishedNodes", "GetPublishedNodes");
                    SetGetPublishedNodesLegacyMethodProperties(ref getPublishedNodesLegacyMethod);

                    MethodState getConfiguredNodesOnEndpointMethod = CreateMethod(methodsFolder, "GetConfiguredNodesOnEndpoint", "GetConfiguredNodesOnEndpoint");
                    SetGetConfiguredNodesOnEndpointMethodProperties(ref getConfiguredNodesOnEndpointMethod);
                }
                catch (Exception e)
                {
                    Utils.Trace(e, "Error creating the address space.");
                }

                AddPredefinedNode(SystemContext, root);
            }
        }

        /// <summary>
        /// Sets properties of the PublishNode method.
        /// </summary>
        private void SetPublishNodeMethodProperties(ref MethodState method)
        {
            // define input arguments
            method.InputArguments = new PropertyState<Argument[]>(method)
            {
                NodeId = new NodeId(method.BrowseName.Name + "InArgs", NamespaceIndex),
                BrowseName = BrowseNames.InputArguments
            };
            method.InputArguments.DisplayName = method.InputArguments.BrowseName.Name;
            method.InputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
            method.InputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
            method.InputArguments.DataType = DataTypeIds.Argument;
            method.InputArguments.ValueRank = ValueRanks.OneDimension;

            method.InputArguments.Value = new Argument[]
            {
                            new Argument() { Name = "NodeId", Description = "NodeId of the node to publish in NodeId format.",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar },
                            new Argument() { Name = "EndpointUrl", Description = "Endpoint URI of the OPC UA server owning the node.",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar }
            };

            method.OnCallMethod = new GenericMethodCalledEventHandler(OnPublishNodeCall);
        }

        /// <summary>
        /// Sets properies of the UnpublishNode method.
        /// </summary>
        private void SetUnpublishNodeMethodProperties(ref MethodState method)
        {
            // define input arguments
            method.InputArguments = new PropertyState<Argument[]>(method)
            {
                NodeId = new NodeId(method.BrowseName.Name + "InArgs", NamespaceIndex),
                BrowseName = BrowseNames.InputArguments
            };
            method.InputArguments.DisplayName = method.InputArguments.BrowseName.Name;
            method.InputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
            method.InputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
            method.InputArguments.DataType = DataTypeIds.Argument;
            method.InputArguments.ValueRank = ValueRanks.OneDimension;

            method.InputArguments.Value = new Argument[]
            {
                            new Argument() { Name = "NodeId", Description = "NodeId of the node to publish in NodeId format.",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar },
                            new Argument() { Name = "EndpointUrl", Description = "Endpoint URI of the OPC UA server owning the node.",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar },
            };

            method.OnCallMethod = new GenericMethodCalledEventHandler(OnUnpublishNodeCall);
        }

        /// <summary>
        /// Sets properies of the GetPublishedNodes method, which is only there for backward compatibility.
        /// This method is acutally returning the configured nodes in NodeId syntax.
        /// </summary>
        private void SetGetPublishedNodesLegacyMethodProperties(ref MethodState method)
        {
            // define input arguments
            method.InputArguments = new PropertyState<Argument[]>(method)
            {
                NodeId = new NodeId(method.BrowseName.Name + "InArgs", NamespaceIndex),
                BrowseName = BrowseNames.InputArguments
            };
            method.InputArguments.DisplayName = method.InputArguments.BrowseName.Name;
            method.InputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
            method.InputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
            method.InputArguments.DataType = DataTypeIds.Argument;
            method.InputArguments.ValueRank = ValueRanks.OneDimension;

            method.InputArguments.Value = new Argument[]
            {
                            new Argument() { Name = "EndpointUrl", Description = "Endpoint URI of the OPC UA server to return the published nodes for.",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar }
            };

            // set output arguments
            method.OutputArguments = new PropertyState<Argument[]>(method)
            {
                NodeId = new NodeId(method.BrowseName.Name + "OutArgs", NamespaceIndex),
                BrowseName = BrowseNames.OutputArguments
            };
            method.OutputArguments.DisplayName = method.OutputArguments.BrowseName.Name;
            method.OutputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
            method.OutputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
            method.OutputArguments.DataType = DataTypeIds.Argument;
            method.OutputArguments.ValueRank = ValueRanks.OneDimension;

            method.OutputArguments.Value = new Argument[]
            {
                        new Argument() { Name = "Published nodes", Description = "List of the nodes configured to publish in OPC Publisher in NodeId format",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar }
            };
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnGetPublishedNodesLegacyCall);
        }

        /// <summary>
        /// Sets properies of the GetConfigruredNodesOnEndpoint method.
        /// </summary>
        private void SetGetConfiguredNodesOnEndpointMethodProperties(ref MethodState method)
        {
            // define input arguments
            method.InputArguments = new PropertyState<Argument[]>(method)
            {
                NodeId = new NodeId(method.BrowseName.Name + "InArgs", NamespaceIndex),
                BrowseName = BrowseNames.InputArguments
            };
            method.InputArguments.DisplayName = method.InputArguments.BrowseName.Name;
            method.InputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
            method.InputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
            method.InputArguments.DataType = DataTypeIds.Argument;
            method.InputArguments.ValueRank = ValueRanks.OneDimension;

            method.InputArguments.Value = new Argument[]
            {
                            new Argument() { Name = "EndpointUrl", Description = "Endpoint URI of the OPC UA server to return the published nodes for.",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar }
            };

            // set output arguments
            method.OutputArguments = new PropertyState<Argument[]>(method)
            {
                NodeId = new NodeId(method.BrowseName.Name + "OutArgs", NamespaceIndex),
                BrowseName = BrowseNames.OutputArguments
            };
            method.OutputArguments.DisplayName = method.OutputArguments.BrowseName.Name;
            method.OutputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
            method.OutputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
            method.OutputArguments.DataType = DataTypeIds.Argument;
            method.OutputArguments.ValueRank = ValueRanks.OneDimension;

            method.OutputArguments.Value = new Argument[]
            {
                        new Argument() { Name = "Configured nodes on endpoint", Description = "List of the nodes configured on the specifcied endpoint to publish in OPC Publisher",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar }
            };
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnGetConfiguredNodesOnEndpointCall);
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private DataItemState CreateDataItemVariable(NodeState parent, string path, string name, BuiltInType dataType, int valueRank, byte accessLevel)
        {
            DataItemState variable = new DataItemState(parent);
            variable.ValuePrecision = new PropertyState<double>(variable);
            variable.Definition = new PropertyState<string>(variable);

            variable.Create(
                SystemContext,
                null,
                variable.BrowseName,
                null,
                true);

            variable.SymbolicName = name;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.NodeId = new NodeId(path, NamespaceIndex);
            variable.BrowseName = new QualifiedName(path, NamespaceIndex);
            variable.DisplayName = new LocalizedText("en", name);
            variable.WriteMask = AttributeWriteMask.None;
            variable.UserWriteMask = AttributeWriteMask.None;
            variable.DataType = (uint)dataType;
            variable.ValueRank = valueRank;
            variable.AccessLevel = accessLevel;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.Value = Opc.Ua.TypeInfo.GetDefaultValue((uint)dataType, valueRank, Server.TypeTree);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            if (valueRank == ValueRanks.OneDimension)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0 });
            }
            else if (valueRank == ValueRanks.TwoDimensions)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0, 0 });
            }

            variable.ValuePrecision.Value = 2;
            variable.ValuePrecision.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.ValuePrecision.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Definition.Value = String.Empty;
            variable.Definition.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Definition.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

            if (parent != null)
            {
                parent.AddChild(variable);
            }

            return variable;
        }

        /// <summary>
        /// Creates a new variable using type Numeric as NodeId.
        /// </summary>
        private DataItemState CreateDataItemVariable(NodeState parent, uint id, string name, BuiltInType dataType, int valueRank, byte accessLevel)
        {
            DataItemState variable = new DataItemState(parent);
            variable.ValuePrecision = new PropertyState<double>(variable);
            variable.Definition = new PropertyState<string>(variable);

            variable.Create(
                SystemContext,
                null,
                variable.BrowseName,
                null,
                true);

            variable.SymbolicName = name;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.NodeId = new NodeId(id, NamespaceIndex);
            variable.BrowseName = new QualifiedName(name, NamespaceIndex);
            variable.DisplayName = new LocalizedText("en", name);
            variable.WriteMask = AttributeWriteMask.None;
            variable.UserWriteMask = AttributeWriteMask.None;
            variable.DataType = (uint)dataType;
            variable.ValueRank = valueRank;
            variable.AccessLevel = accessLevel;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.Value = Opc.Ua.TypeInfo.GetDefaultValue((uint)dataType, valueRank, Server.TypeTree);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            if (valueRank == ValueRanks.OneDimension)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0 });
            }
            else if (valueRank == ValueRanks.TwoDimensions)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0, 0 });
            }

            variable.ValuePrecision.Value = 2;
            variable.ValuePrecision.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.ValuePrecision.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Definition.Value = String.Empty;
            variable.Definition.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Definition.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

            if (parent != null)
            {
                parent.AddChild(variable);
            }

            return variable;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private BaseDataVariableState CreateVariable(NodeState parent, string path, string name, NodeId dataType, int valueRank)
        {
            BaseDataVariableState variable = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
                UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
                DataType = dataType,
                ValueRank = valueRank,
                AccessLevel = AccessLevels.CurrentReadOrWrite,
                UserAccessLevel = AccessLevels.CurrentReadOrWrite,
                Historizing = false,
                StatusCode = StatusCodes.Good,
                Timestamp = DateTime.UtcNow
            };

            if (valueRank == ValueRanks.OneDimension)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0 });
            }
            else if (valueRank == ValueRanks.TwoDimensions)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0, 0 });
            }

            if (parent != null)
            {
                parent.AddChild(variable);
            }

            return variable;
        }

        /// <summary>
        /// Creates a new method.
        /// </summary>
        private MethodState CreateMethod(NodeState parent, string path, string name)
        {
            MethodState method = new MethodState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                Executable = true,
                UserExecutable = true
            };

            if (parent != null)
            {
                parent.AddChild(method);
            }

            return method;
        }

        /// <summary>
        /// Creates a new method using type Numeric for the NodeId.
        /// </summary>
        private MethodState CreateMethod(NodeState parent, uint id, string name)
        {
            MethodState method = new MethodState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                NodeId = new NodeId(id, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                Executable = true,
                UserExecutable = true
            };

            if (parent != null)
            {
                parent.AddChild(method);
            }

            return method;
        }

        /// <summary>
        /// Method to start monitoring a node and publish the data to IoTHub. Executes synchronously.
        /// </summary>
        private ServiceResult OnPublishNodeCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (string.IsNullOrEmpty(inputArguments[0] as string) || string.IsNullOrEmpty(inputArguments[1] as string))
            {
                Logger.Error("PublishNode: Invalid Arguments when trying to publish a node.");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments as strings!");
            }

            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            NodeId nodeId = null;
            ExpandedNodeId expandedNodeId = null;
            Uri endpointUrl = null;
            bool isNodeIdFormat = true;
            try
            {
                string id = inputArguments[0] as string;
                if (id.Contains("nsu="))
                {
                    expandedNodeId = ExpandedNodeId.Parse(id);
                    isNodeIdFormat = false;
                }
                else
                {
                    nodeId = NodeId.Parse(id);
                    isNodeIdFormat = true;
                }
                endpointUrl = new Uri(inputArguments[1] as string);
            }
            catch (UriFormatException)
            {
                Logger.Error($"PublishNode: The EndpointUrl has an invalid format '{inputArguments[1] as string}'!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA endpoint URL as second argument!");
            }
            catch (Exception e)
            {
                Logger.Error(e, $"PublishNode: The NodeId has an invalid format '{inputArguments[0] as string}'!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA NodeId in NodeId or ExpandedNodeId format as first argument!");
            }

            // find/create a session to the endpoint URL and start monitoring the node.
            try
            {
                // lock the publishing configuration till we are done
                OpcSessionsListSemaphore.Wait();

                if (ShutdownTokenSource.IsCancellationRequested)
                {
                    return ServiceResult.Create(StatusCodes.BadUnexpectedError, $"Publisher shutdown in progress.");
                }

                // find the session we need to monitor the node
                OpcSession opcSession = null;
                opcSession = OpcSessions.FirstOrDefault(s => s.EndpointUrl.AbsoluteUri.Equals(endpointUrl.AbsoluteUri, StringComparison.OrdinalIgnoreCase));

                // add a new session.
                if (opcSession == null)
                {
                    // create new session info.
                    opcSession = new OpcSession(endpointUrl, true, OpcSessionCreationTimeout);
                    OpcSessions.Add(opcSession);
                    Logger.Information($"PublishNode: No matching session found for endpoint '{endpointUrl.OriginalString}'. Requested to create a new one.");
                }

                if (isNodeIdFormat)
                {
                    // add the node info to the subscription with the default publishing interval, execute syncronously
                    Logger.Debug($"PublishNode: Request to monitor item with NodeId '{nodeId.ToString()}' (PublishingInterval: {OpcPublishingInterval}, SamplingInterval: {OpcSamplingInterval})");
                    statusCode = opcSession.AddNodeForMonitoringAsync(nodeId, null, OpcPublishingInterval, OpcSamplingInterval, ShutdownTokenSource.Token).Result;
                }
                else
                {
                    // add the node info to the subscription with the default publishing interval, execute syncronously
                    Logger.Debug($"PublishNode: Request to monitor item with ExpandedNodeId '{expandedNodeId.ToString()}' (PublishingInterval: {OpcPublishingInterval}, SamplingInterval: {OpcSamplingInterval})");
                    statusCode = opcSession.AddNodeForMonitoringAsync(null, expandedNodeId, OpcPublishingInterval, OpcSamplingInterval, ShutdownTokenSource.Token).Result;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"PublishNode: Exception while trying to configure publishing node '{(isNodeIdFormat ? nodeId.ToString() : expandedNodeId.ToString())}'");
                return ServiceResult.Create(e, StatusCodes.BadUnexpectedError, $"Unexpected error publishing node: {e.Message}");
            }
            finally
            {
                OpcSessionsListSemaphore.Release();
            }
            return (statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.Accepted ? ServiceResult.Good : ServiceResult.Create(StatusCodes.Bad, "Can not start monitoring node!"));
        }

        /// <summary>
        /// Method to remove the node from the subscription and stop publishing telemetry to IoTHub. Executes synchronously.
        /// </summary>
        private ServiceResult OnUnpublishNodeCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (string.IsNullOrEmpty(inputArguments[0] as string) || string.IsNullOrEmpty(inputArguments[1] as string))
            {
                Logger.Error("UnpublishNode: Invalid arguments!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide all arguments!");
            }

            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            NodeId nodeId = null;
            ExpandedNodeId expandedNodeId = null;
            Uri endpointUrl = null;
            bool isNodeIdFormat = true;
            try
            {
                string id = inputArguments[0] as string;
                if (id.Contains("nsu="))
                {
                    expandedNodeId = ExpandedNodeId.Parse(id);
                    isNodeIdFormat = false;
                }
                else
                {
                    nodeId = NodeId.Parse(id);
                    isNodeIdFormat = true;
                }
                endpointUrl = new Uri(inputArguments[1] as string);
            }
            catch (UriFormatException)
            {
                Logger.Error($"UnpublishNode: The endpointUrl is invalid '{inputArguments[1] as string}'!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA endpoint URL as second argument!");
            }
            catch (Exception e)
            {
                Logger.Error(e, $"UnpublishNode: The NodeId has an invalid format '{inputArguments[0] as string}'!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA NodeId in NodeId or ExpandedNodeId format as first argument!");
            }

            // find the session and stop monitoring the node.
            try
            {
                OpcSessionsListSemaphore.Wait();
                if (ShutdownTokenSource.IsCancellationRequested)
                {
                    return ServiceResult.Create(StatusCodes.BadUnexpectedError, $"Publisher shutdown in progress.");
                }

                // find the session we need to monitor the node
                OpcSession opcSession = null;
                try
                {
                    opcSession = OpcSessions.FirstOrDefault(s => s.EndpointUrl.AbsoluteUri.Equals(endpointUrl.AbsoluteUri, StringComparison.OrdinalIgnoreCase));
                }
                catch
                {
                    opcSession = null;
                }

                if (opcSession == null)
                {
                    // do nothing if there is no session for this endpoint.
                    Logger.Error($"UnpublishNode: Session for endpoint '{endpointUrl.OriginalString}' not found.");
                    return ServiceResult.Create(StatusCodes.BadSessionIdInvalid, "Session for endpoint of node to unpublished not found!");
                }
                else
                {
                    if (isNodeIdFormat)
                    {
                        // stop monitoring the node, execute syncronously
                        Logger.Information($"UnpublishNode: Request to stop monitoring item with NodeId '{nodeId.ToString()}')");
                        statusCode = opcSession.RequestMonitorItemRemovalAsync(nodeId, null, ShutdownTokenSource.Token).Result;
                    }
                    else
                    {
                        // stop monitoring the node, execute syncronously
                        Logger.Information($"UnpublishNode: Request to stop monitoring item with ExpandedNodeId '{expandedNodeId.ToString()}')");
                        statusCode = opcSession.RequestMonitorItemRemovalAsync(null, expandedNodeId, ShutdownTokenSource.Token).Result;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"UnpublishNode: Exception while trying to configure publishing node '{nodeId.ToString()}'");
                return ServiceResult.Create(e, StatusCodes.BadUnexpectedError, $"Unexpected error unpublishing node: {e.Message}");
            }
            finally
            {
                OpcSessionsListSemaphore.Release();
            }
            return (statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.Accepted ? ServiceResult.Good : ServiceResult.Create(StatusCodes.Bad, "Can not stop monitoring node!"));
        }

        /// <summary>
        /// Method to get the list of configured nodes and is only there for backward compatibility. Executes synchronously.
        /// The format of the returned node description is using NodeId format. The assumption
        /// is that the caller is able to access the namespace array of the server
        /// on the endpoint URL(s) themselve and do the correct mapping.
        /// </summary>
        private ServiceResult OnGetPublishedNodesLegacyCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            Uri endpointUrl = null;

            if (string.IsNullOrEmpty(inputArguments[0] as string))
            {
                Logger.Error($"GetPublishedNodesLegacy: endpointUrl is null or empty'!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA endpoint URL as first argument!");
            }
            else
            {
                try
                {
                    endpointUrl = new Uri(inputArguments[0] as string);
                }
                catch (UriFormatException)
                {
                    Logger.Error($"GetPublishedNodesLegacy: The endpointUrl is invalid '{inputArguments[0] as string}'!");
                    return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA endpoint URL as first argument!");
                }
            }

            // get the list of published nodes in NodeId format
            outputArguments[0] = JsonConvert.SerializeObject(GetPublisherConfigurationFileEntriesAsNodeIds(endpointUrl));
            Logger.Information("GetPublishedNodesLegacy: Success!");

            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to get the list of configured nodes on the psecified endpoint, which returns the list in new format. Executes synchronously.
        /// </summary>
        private ServiceResult OnGetConfiguredNodesOnEndpointCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            Uri endpointUrl = null;

            if (string.IsNullOrEmpty(inputArguments[0] as string))
            {
                Logger.Error($"OnGetConfiguredNodesOnEndpointCall: endpointUrl is null or empty'!");
                return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA endpoint URL as first argument!");
            }
            else
            {
                try
                {
                    endpointUrl = new Uri(inputArguments[0] as string);
                }
                catch (UriFormatException)
                {
                    Logger.Error($"OnGetConfiguredNodesOnEndpointCall: The endpointUrl is invalid '{inputArguments[0] as string}'!");
                    return ServiceResult.Create(StatusCodes.BadArgumentsMissing, "Please provide a valid OPC UA endpoint URL as first argument!");
                }
            }

            // get the list of published nodes in NodeId format
            uint nodeConfigVersion = 0;
            outputArguments[0] = JsonConvert.SerializeObject(GetPublisherConfigurationFileEntries(endpointUrl, false, out nodeConfigVersion));
            Logger.Information("OnGetConfiguredNodesOnEndpointCall: Success!");

            return ServiceResult.Good;
        }
    }
}