// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Plc {
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Node manager
    /// </summary>
    public partial class PlcNodeManager : CustomNodeManager2 {

        public string ProgramName { get; set; } = "Plc";

        public uint RandomUnsignedInt32 {
            get => (uint)_randomUnsignedInt32.Value;
            set {
                _randomUnsignedInt32.Value = value;
                _randomUnsignedInt32.Timestamp = DateTime.Now;
                _randomUnsignedInt32.ClearChangeMasks(SystemContext, false);
            }
        }

        public int RandomSignedInt32 {
            get => (int)_randomSignedInt32.Value;
            set {
                _randomSignedInt32.Value = value;
                _randomSignedInt32.Timestamp = DateTime.Now;
                _randomSignedInt32.ClearChangeMasks(SystemContext, false);
            }
        }

        public double SpikeData {
            get => (double)_spikeData.Value;
            set {
                _spikeData.Value = value;
                _spikeData.Timestamp = DateTime.Now;
                _spikeData.ClearChangeMasks(SystemContext, false);
            }
        }

        public double DipData {
            get => (double)_dipData.Value;
            set {
                _dipData.Value = value;
                _dipData.Timestamp = DateTime.Now;
                _dipData.ClearChangeMasks(SystemContext, false);
            }
        }

        public double PosTrendData {
            get => (double)_posTrendData.Value;
            set {
                _posTrendData.Value = value;
                _posTrendData.Timestamp = DateTime.Now;
                _posTrendData.ClearChangeMasks(SystemContext, false);
            }
        }

        public double NegTrendData {
            get => (double)_negTrendData.Value;
            set {
                _negTrendData.Value = value;
                _negTrendData.Timestamp = DateTime.Now;
                _negTrendData.ClearChangeMasks(SystemContext, false);
            }
        }

        public bool AlternatingBoolean {
            get => (bool)_alternatingBoolean.Value;
            set {
                _alternatingBoolean.Value = value;
                _alternatingBoolean.Timestamp = DateTime.Now;
                _alternatingBoolean.ClearChangeMasks(SystemContext, false);
            }
        }

        public uint StepUp {
            get => (uint)_step.Value;
            set {
                _step.Value = value;
                _step.Timestamp = DateTime.Now;
                _step.ClearChangeMasks(SystemContext, false);
            }
        }

        public PlcNodeManager(IServerInternal server, ApplicationConfiguration configuration)
            : base(server, configuration, Namespaces.OpcPlcApplications) {
            SystemContext.NodeIdFactory = this;
            Initialize();
        }

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        public override NodeId New(ISystemContext context, NodeState node) {
            if (node is BaseInstanceState instance &&
                instance.Parent?.NodeId.Identifier is string id) {
                return new NodeId(id + "_" + instance.SymbolicName,
                    instance.Parent.NodeId.NamespaceIndex);
            }
            return node.NodeId;
        }

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        private FolderState CreateFolder(NodeState parent, string path, string name) {
            var folder = new FolderState(parent) {
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

            parent?.AddChild(folder);

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
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences) {
            lock (Lock) {
                IList<IReference> references = null;

                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references)) {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                var root = CreateFolder(null, ProgramName, ProgramName);
                root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, root.NodeId));
                root.EventNotifier = EventNotifiers.SubscribeToEvents;
                AddRootNotifier(root);

                try {
                    var dataFolder = CreateFolder(root, "Telemetry", "Telemetry");

                    _step = CreateBaseVariable(dataFolder, "StepUp", "StepUp", BuiltInType.UInt32,
                        ValueRanks.Scalar, AccessLevels.CurrentReadOrWrite, "Constantly increasing value");
                    _alternatingBoolean = CreateBaseVariable(dataFolder, "AlternatingBoolean",
                        "AlternatingBoolean", BuiltInType.Boolean, ValueRanks.Scalar,
                        AccessLevels.CurrentRead, "Alternating boolean value");
                    _randomSignedInt32 = CreateBaseVariable(dataFolder, "RandomSignedInt32",
                        "RandomSignedInt32", BuiltInType.Int32, ValueRanks.Scalar,
                        AccessLevels.CurrentRead, "Random signed 32 bit integer value");
                    _randomUnsignedInt32 = CreateBaseVariable(dataFolder, "RandomUnsignedInt32",
                        "RandomUnsignedInt32", BuiltInType.UInt32, ValueRanks.Scalar,
                        AccessLevels.CurrentRead, "Random unsigned 32 bit integer value");
                    _spikeData = CreateBaseVariable(dataFolder, "SpikeData", "SpikeData",
                        BuiltInType.Double, ValueRanks.Scalar,
                        AccessLevels.CurrentRead, "Value which generates randomly spikes");
                    _dipData = CreateBaseVariable(dataFolder, "DipData", "DipData",
                        BuiltInType.Double, ValueRanks.Scalar,
                        AccessLevels.CurrentRead, "Value which generates randomly dips");
                    _posTrendData = CreateBaseVariable(dataFolder, "PositiveTrendData",
                        "PositiveTrendData", BuiltInType.Float, ValueRanks.Scalar,
                        AccessLevels.CurrentRead, "Value with a slow positive trend");
                    _negTrendData = CreateBaseVariable(dataFolder, "NegativeTrendData",
                        "NegativeTrendData", BuiltInType.Float, ValueRanks.Scalar,
                        AccessLevels.CurrentRead, "Value with a slow negative trend");

                    var methodsFolder = CreateFolder(root, "Methods", "Methods");
                    var resetTrendMethod = CreateMethod(methodsFolder, "ResetTrend",
                        "ResetTrend", "Reset the trend values to their baseline value");
                    SetResetTrendMethodProperties(ref resetTrendMethod);
                    var resetStepUpMethod = CreateMethod(methodsFolder, "ResetStepUp",
                        "ResetStepUp", "Resets the StepUp counter to 0");
                    SetResetStepUpMethodProperties(ref resetStepUpMethod);
                    var startStepUpMethod = CreateMethod(methodsFolder, "StartStepUp",
                        "StartStepUp", "Starts the StepUp counter");
                    SetStartStepUpMethodProperties(ref startStepUpMethod);
                    var stopStepUpMethod = CreateMethod(methodsFolder, "StopStepUp",
                        "StopStepUp", "Stops the StepUp counter");
                    SetStopStepUpMethodProperties(ref stopStepUpMethod);
                }
                catch (Exception e) {
                    Utils.Trace(e, "Error creating the address space.");
                }

                AddPredefinedNode(SystemContext, root);
            }
        }

        /// <summary>
        /// Sets properies of the ResetTrend method.
        /// </summary>
        private void SetResetTrendMethodProperties(ref MethodState method) {
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnResetTrendCall);
        }

        /// <summary>
        /// Sets properies of the ResetStepUp method.
        /// </summary>
        private void SetResetStepUpMethodProperties(ref MethodState method) {
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnResetStepUpCall);
        }

        /// <summary>
        /// Sets properies of the StartStepUp method.
        /// </summary>
        private void SetStartStepUpMethodProperties(ref MethodState method) {
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnStartStepUpCall);
        }

        /// <summary>
        /// Sets properies of the StopStepUp method.
        /// </summary>
        private void SetStopStepUpMethodProperties(ref MethodState method) {
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnStopStepUpCall);
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private BaseDataVariableState CreateBaseVariable(NodeState parent, string path,
            string name, BuiltInType dataType, int valueRank, byte accessLevel, string description) {
            return CreateBaseVariable(parent, path, name, (uint)dataType, valueRank, accessLevel, description);
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private BaseDataVariableState CreateBaseVariable(NodeState parent, string path,
            string name, NodeId dataType, int valueRank, byte accessLevel, string description) {
            var variable = new BaseDataVariableState(parent) {
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
                AccessLevel = accessLevel,
                UserAccessLevel = accessLevel,
                Historizing = false,
                Value = TypeInfo.GetDefaultValue(dataType, valueRank, Server.TypeTree),
                StatusCode = StatusCodes.Good,
                Timestamp = DateTime.UtcNow,
                Description = new LocalizedText(description)
            };

            if (valueRank == ValueRanks.OneDimension) {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0 });
            }
            else if (valueRank == ValueRanks.TwoDimensions) {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0, 0 });
            }

            parent?.AddChild(variable);

            return variable;
        }

        /// <summary>
        /// Creates a new method.
        /// </summary>
        private MethodState CreateMethod(NodeState parent, string path, string name,
            string description) {
            var method = new MethodState(parent) {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
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

        /// <summary>
        /// Method to reset the trend values. Executes synchronously.
        /// </summary>
        private ServiceResult OnResetTrendCall(ISystemContext context, MethodState method,
            IList<object> inputArguments, IList<object> outputArguments) {
            ResetTrendData();
            Utils.Trace($"ResetTrend method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to reset the stepup value. Executes synchronously.
        /// </summary>
        private ServiceResult OnResetStepUpCall(ISystemContext context, MethodState method,
            IList<object> inputArguments, IList<object> outputArguments) {
            ResetStepUpData();
            Utils.Trace($"ResetStepUp method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to start the stepup value. Executes synchronously.
        /// </summary>
        private ServiceResult OnStartStepUpCall(ISystemContext context, MethodState method,
            IList<object> inputArguments, IList<object> outputArguments) {
            StartStepUp();
            Utils.Trace($"StartStepUp method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to stop the stepup value. Executes synchronously.
        /// </summary>
        private ServiceResult OnStopStepUpCall(ISystemContext context, MethodState method,
            IList<object> inputArguments, IList<object> outputArguments) {
            StopStepUp();
            Utils.Trace($"StopStepUp method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Following variables listed here are simulated.
        /// </summary>
        protected BaseDataVariableState _step = null;
        protected BaseDataVariableState _alternatingBoolean = null;
        protected BaseDataVariableState _randomUnsignedInt32 = null;
        protected BaseDataVariableState _randomSignedInt32 = null;
        protected BaseDataVariableState _spikeData = null;
        protected BaseDataVariableState _dipData = null;
        protected BaseDataVariableState _posTrendData = null;
        protected BaseDataVariableState _negTrendData = null;

        /// <summary>
        /// This variable can be changed by OPCUA client.
        /// </summary>
        protected BaseDataVariableState _daeTemp = null;
    }
}