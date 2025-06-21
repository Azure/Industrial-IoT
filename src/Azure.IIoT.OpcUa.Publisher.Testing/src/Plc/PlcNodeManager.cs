// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Plc
{
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Server;
    using Opc.Ua.Test;
    using Plc.PluginNodes;
    using Plc.PluginNodes.Models;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public class PlcNodeManager : CustomNodeManager2
    {
        public IEnumerable<IPluginNodes> PluginNodes { get; }

        public PlcNodeManager(IServerInternal server,
            ApplicationConfiguration configuration,
            TimeService timeService, ILogger logger, uint scaleunits)
            : base(server, configuration,
            [
                Namespaces.PlcApplications,
                Namespaces.PlcSimulation,
                Namespaces.PlcInstance
            ])
        {
            _timeService = timeService;
            _logger = logger;
            SystemContext.NodeIdFactory = this;

            PluginNodes = new List<IPluginNodes>
            {
                new ComplexTypePlcPluginNode(),
                new DataPluginNodes(),
                new DeterministicGuidPluginNodes(),
                new DipPluginNode(),
                new FastPluginNodes(),
                new FastRandomPluginNodes(),
                new LongIdPluginNode(),
                new LongStringPluginNodes(),
                new NegTrendPluginNode(),
                new PosTrendPluginNode(),
                new SlowPluginNodes(),
                new SlowRandomPluginNodes(),
                new SpecialCharNamePluginNode(),
                new SpikePluginNode()
            };

            foreach (var plugin in PluginNodes)
            {
                plugin.Logger = logger;
                plugin.TimeService = timeService;
                plugin.ScaleUnits = scaleunits;
            }

            _simulation = new PlcSimulation(this, timeService);
        }

        protected override void Dispose(bool disposing)
        {
            _simulation.Stop();
            base.Dispose(disposing);
        }

        public string GetPnJson()
        {
            return _simulation.GetPublisherConfigJson();
        }

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            if (node is BaseInstanceState instance && instance.Parent?.NodeId.Identifier is string id)
            {
                return new NodeId(id + "_" + instance.SymbolicName, instance.Parent.NodeId.NamespaceIndex);
            }

            return node.NodeId;
        }

        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <param name="externalReferences"></param>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out var references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = [];
                }

                _externalReferences = externalReferences;

                var root = CreateFolder(null, "OpcPlc", "OpcPlc", NamespaceType.PlcApplications);
                root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, root.NodeId));
                root.EventNotifier = EventNotifiers.SubscribeToEvents;
                AddRootNotifier(root);

                try
                {
                    var telemetryFolder = CreateFolder(root, "Telemetry", "Telemetry", NamespaceType.PlcApplications);
                    var methodsFolder = CreateFolder(root, "Methods", "Methods", NamespaceType.PlcApplications);

                    // Add nodes to address space from plugin nodes list.
                    foreach (var plugin in PluginNodes)
                    {
                        plugin.AddToAddressSpace(telemetryFolder, methodsFolder, plcNodeManager: this);
                    }
                }
                catch (Exception e)
                {
                    _logger.AddressSpaceError(e);
                }

                AddPredefinedNode(SystemContext, root);
            }
            _simulation.Start();
        }

        public SimulatedVariableNode<T> CreateVariableNode<T>(BaseDataVariableState variable)
        {
            return new SimulatedVariableNode<T>(SystemContext, variable, _timeService);
        }

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="namespaceType"></param>
        public FolderState CreateFolder(NodeState parent, string path, string name, NamespaceType namespaceType)
        {
            var existingFolder = parent?.FindChildBySymbolicName(SystemContext, name);
            if (existingFolder != null)
            {
                return (FolderState)existingFolder;
            }

            var namespaceIndex = NamespaceIndexes[(int)namespaceType];

            var folder = new FolderState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId(path, namespaceIndex),
                BrowseName = new QualifiedName(path, namespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                EventNotifier = EventNotifiers.None
            };

            parent?.AddChild(folder);

            return folder;
        }

        /// <summary>
        /// Creates a new extended variable.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="dataType"></param>
        /// <param name="valueRank"></param>
        /// <param name="accessLevel"></param>
        /// <param name="description"></param>
        /// <param name="namespaceType"></param>
        /// <param name="randomize"></param>
        /// <param name="stepSizeValue"></param>
        /// <param name="minTypeValue"></param>
        /// <param name="maxTypeValue"></param>
        /// <param name="defaultValue"></param>
        public BaseDataVariableState CreateBaseVariable(NodeState parent, dynamic path,
            string name, NodeId dataType, int valueRank, byte accessLevel, string description,
            NamespaceType namespaceType, bool randomize, object stepSizeValue,
            object minTypeValue, object maxTypeValue, object defaultValue = null)
        {
            var baseDataVariableState = new BaseDataVariableStateExtended(parent, randomize,
                stepSizeValue, minTypeValue, maxTypeValue)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType
            };
            return CreateBaseVariable(baseDataVariableState, parent, path, name, dataType,
                valueRank, accessLevel, description, namespaceType, defaultValue);
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="dataType"></param>
        /// <param name="valueRank"></param>
        /// <param name="accessLevel"></param>
        /// <param name="description"></param>
        /// <param name="namespaceType"></param>
        /// <param name="defaultValue"></param>
        public BaseDataVariableState CreateBaseVariable(NodeState parent, dynamic path,
            string name, NodeId dataType, int valueRank, byte accessLevel, string description,
            NamespaceType namespaceType, object defaultValue = null)
        {
            var baseDataVariableState = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType
            };
            return CreateBaseVariable(baseDataVariableState, parent, path, name, dataType,
                valueRank, accessLevel, description, namespaceType, defaultValue);
        }

        /// <summary>
        /// Creates a new method.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="namespaceType"></param>
        public MethodState CreateMethod(NodeState parent, string path, string name,
            string description, NamespaceType namespaceType)
        {
            var namespaceIndex = NamespaceIndexes[(int)namespaceType];

            var method = new MethodState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                NodeId = new NodeId(path, namespaceIndex),
                BrowseName = new QualifiedName(path, namespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                Executable = true,
                UserExecutable = true,
                Description = new LocalizedText(description)
            };

            parent?.AddChild(method);

            return method;
        }

        private BaseDataVariableState CreateBaseVariable(BaseDataVariableState baseDataVariableState,
            NodeState parent, dynamic path, string name, NodeId dataType, int valueRank,
            byte accessLevel, string description, NamespaceType namespaceType, object defaultValue = null)
        {
            var namespaceIndex = NamespaceIndexes[(int)namespaceType];

            if (path is uint || path is long)
            {
                baseDataVariableState.NodeId = new NodeId((uint)path, namespaceIndex);
                baseDataVariableState.BrowseName = new QualifiedName(((uint)path)
                    .ToString(CultureInfo.CurrentCulture), namespaceIndex);
            }
            else if (path is string)
            {
                baseDataVariableState.NodeId = new NodeId(path, namespaceIndex);
                baseDataVariableState.BrowseName = new QualifiedName(path, namespaceIndex);
            }
            else
            {
                _logger.NodeIdType((string)path.GetType().ToString());
                baseDataVariableState.NodeId = new NodeId(path, namespaceIndex);
                baseDataVariableState.BrowseName = new QualifiedName(name, namespaceIndex);
            }

            baseDataVariableState.DisplayName = new LocalizedText("en", name);
            baseDataVariableState.WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            baseDataVariableState.UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            baseDataVariableState.DataType = dataType;
            baseDataVariableState.ValueRank = valueRank;
            baseDataVariableState.AccessLevel = accessLevel;
            baseDataVariableState.UserAccessLevel = accessLevel;
            baseDataVariableState.Historizing = false;
            baseDataVariableState.Value = defaultValue ?? TypeInfo.GetDefaultValue(dataType, valueRank, Server.TypeTree);
            baseDataVariableState.StatusCode = StatusCodes.Good;
            baseDataVariableState.Timestamp = _timeService.UtcNow;
            baseDataVariableState.Description = new LocalizedText(description);

            if (valueRank == ValueRanks.OneDimension)
            {
                baseDataVariableState.ArrayDimensions = new ReadOnlyList<uint>([0]);
            }
            else if (valueRank == ValueRanks.TwoDimensions)
            {
                baseDataVariableState.ArrayDimensions = new ReadOnlyList<uint>([0, 0]);
            }

            parent?.AddChild(baseDataVariableState);

            return baseDataVariableState;
        }

        /// <summary>
        /// Loads a predefined node set by using the specified handler.
        /// </summary>
        /// <param name="loadPredefinedNodeshandler"></param>
        public void LoadPredefinedNodes(Func<ISystemContext, NodeStateCollection> loadPredefinedNodeshandler)
        {
            _loadPredefinedNodeshandler = loadPredefinedNodeshandler;

            base.LoadPredefinedNodes(SystemContext, _externalReferences);
        }

        /// <summary>
        /// Adds a predefined node set.
        /// </summary>
        /// <param name="node"></param>
        public void AddPredefinedNode(NodeState node)
        {
            base.AddPredefinedNode(SystemContext, node);
        }

        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            return _loadPredefinedNodeshandler?.Invoke(context);
        }

        private readonly TimeService _timeService;
        private readonly ILogger _logger;
        private IDictionary<NodeId, IList<IReference>> _externalReferences;
        private Func<ISystemContext, NodeStateCollection> _loadPredefinedNodeshandler;
        private readonly PlcSimulation _simulation;
    }

    /// <summary>
    /// Source-generated logging definitions for PlcNodeManager
    /// </summary>
    internal static partial class PlcNodeManagerLogging
    {
        private const int EventClass = 50;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Error,
            Message = "Error creating address space.")]
        public static partial void AddressSpaceError(this ILogger logger, Exception e);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Debug,
            Message = "NodeId type is {NodeIdType}")]
        public static partial void NodeIdType(this ILogger logger, string nodeIdType);
    }
}
