// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Plc.PluginNodes
{
    using Plc.PluginNodes.Models;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Test;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Nodes with values: Cycling step-up, alternating boolean, random signed 32-bit integer and random unsigend 32-bit integer.
    /// </summary>
    public class DataPluginNodes : IPluginNodes
    {
        public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();
        public TimeService TimeService { get; set; }
        public ILogger Logger { get; set; }
        public uint ScaleUnits { get; set; }

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;
            var folder = _plcNodeManager.CreateFolder(
                telemetryFolder,
                path: "Basic",
                name: "Basic",
                NamespaceType.PlcApplications);
            AddNodes(folder);
            AddMethods(methodsFolder);
        }

        public void StartSimulation()
        {
            _stepUpCycleInPhase = PlcSimulation.SimulationCycleCount;
            _stepUpStarted = true;
            _alternatingBooleanCycleInPhase = PlcSimulation.SimulationCycleCount;

            _stepUpNode.Start(StepUpGenerator, PlcSimulation.SimulationCycleLength);
            _alternatingBooleanNode.Start(AlternatingBooleanGenerator, PlcSimulation.SimulationCycleLength);
#pragma warning disable CA5394 // Do not use insecure randomness
            _randomSignedInt32.Start(_ => _random.Next(int.MinValue, int.MaxValue), PlcSimulation.SimulationCycleLength);
#pragma warning restore CA5394 // Do not use insecure randomness
#pragma warning disable CA5394 // Do not use insecure randomness
            _randomUnsignedInt32.Start(_ => (uint)_random.Next(), PlcSimulation.SimulationCycleLength);
#pragma warning restore CA5394 // Do not use insecure randomness
        }

        public void StopSimulation()
        {
            _stepUpNode.Stop();
            _alternatingBooleanNode.Stop();
            _randomSignedInt32.Stop();
            _randomUnsignedInt32.Stop();
        }

        private void AddNodes(FolderState folder)
        {
            _stepUpNode = _plcNodeManager.CreateVariableNode<uint>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: "StepUp",
                    name: "StepUp",
                    new NodeId((uint)BuiltInType.UInt32),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentReadOrWrite,
                    "Constantly increasing value",
                    NamespaceType.PlcApplications));

            _alternatingBooleanNode = _plcNodeManager.CreateVariableNode<bool>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: "AlternatingBoolean",
                    name: "AlternatingBoolean",
                    new NodeId((uint)BuiltInType.Boolean),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentRead,
                    "Alternating boolean value",
                    NamespaceType.PlcApplications));

            _randomSignedInt32 = _plcNodeManager.CreateVariableNode<int>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: "RandomSignedInt32",
                    name: "RandomSignedInt32",
                    new NodeId((uint)BuiltInType.Int32),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentRead,
                    "Random signed 32 bit integer value",
                    NamespaceType.PlcApplications));

            _randomUnsignedInt32 = _plcNodeManager.CreateVariableNode<uint>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: "RandomUnsignedInt32",
                    "RandomUnsignedInt32",
                    new NodeId((uint)BuiltInType.UInt32),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentRead,
                    "Random unsigned 32 bit integer value",
                    NamespaceType.PlcApplications));

            Nodes = new List<NodeWithIntervals>
            {
                new() {
                    NodeId = "StepUp",
                    Namespace = Plc.Namespaces.PlcApplications
                },
                new() {
                    NodeId = "AlternatingBoolean",
                    Namespace = Plc.Namespaces.PlcApplications
                },
                new() {
                    NodeId = "RandomSignedInt32",
                    Namespace = Plc.Namespaces.PlcApplications
                },
                new() {
                    NodeId = "RandomUnsignedInt32",
                    Namespace = Plc.Namespaces.PlcApplications
                }
            };
        }

        private void AddMethods(FolderState parentFolder)
        {
            var resetStepUpMethod = _plcNodeManager.CreateMethod(parentFolder,
                "ResetStepUp", "ResetStepUp", "Resets the StepUp counter to 0", NamespaceType.PlcApplications);
            SetResetStepUpMethodProperties(ref resetStepUpMethod);
            var startStepUpMethod = _plcNodeManager.CreateMethod(parentFolder,
                "StartStepUp", "StartStepUp", "Starts the StepUp counter", NamespaceType.PlcApplications);
            SetStartStepUpMethodProperties(ref startStepUpMethod);
            var stopStepUpMethod = _plcNodeManager.CreateMethod(parentFolder,
                "StopStepUp", "StopStepUp", "Stops the StepUp counter", NamespaceType.PlcApplications);
            SetStopStepUpMethodProperties(ref stopStepUpMethod);
        }

        private void SetResetStepUpMethodProperties(ref MethodState method)
        {
            method.OnCallMethod += OnResetStepUpCall;
        }

        private void SetStartStepUpMethodProperties(ref MethodState method)
        {
            method.OnCallMethod += OnStartStepUpCall;
        }

        private void SetStopStepUpMethodProperties(ref MethodState method)
        {
            method.OnCallMethod += OnStopStepUpCall;
        }

        /// <summary>
        /// Method to reset the stepup value. Executes synchronously.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        private ServiceResult OnResetStepUpCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            ResetStepUpData();
            Logger.LogDebug("ResetStepUp method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to start the stepup value. Executes synchronously.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        private ServiceResult OnStartStepUpCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            StartStepUp();
            Logger.LogDebug("StartStepUp method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to stop the stepup value. Executes synchronously.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        private ServiceResult OnStopStepUpCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            StopStepUp();
            Logger.LogDebug("StopStepUp method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Updates simulation values. Called each SimulationCycleLength msec.
        /// Using SimulationCycleCount cycles per simulation phase.
        /// </summary>
        /// <param name="value"></param>
        private uint StepUpGenerator(uint value)
        {
            // increase step up value
            if (_stepUpStarted && (_stepUpCycleInPhase % (PlcSimulation.SimulationCycleCount / 50) == 0))
            {
                value++;
            }

            // end of cycle: reset cycle count
            if (--_stepUpCycleInPhase == 0)
            {
                _stepUpCycleInPhase = PlcSimulation.SimulationCycleCount;
            }

            return value;
        }

        /// <summary>
        /// Updates simulation values. Called each SimulationCycleLength msec.
        /// Using SimulationCycleCount cycles per simulation phase.
        /// </summary>
        /// <param name="value"></param>
        private bool AlternatingBooleanGenerator(bool value)
        {
            // calculate next boolean value
            var nextAlternatingBoolean = _alternatingBooleanCycleInPhase % PlcSimulation.SimulationCycleCount == 0 ? !value : value;
            if (value != nextAlternatingBoolean)
            {
                Logger.LogTrace("Data change to: {NextAlternatingBoolean}", nextAlternatingBoolean);
            }

            // end of cycle: reset cycle count
            if (--_alternatingBooleanCycleInPhase == 0)
            {
                _alternatingBooleanCycleInPhase = PlcSimulation.SimulationCycleCount;
            }

            return nextAlternatingBoolean;
        }

        /// <summary>
        /// Method implementation to reset the StepUp data.
        /// </summary>
        public void ResetStepUpData()
        {
            _stepUpNode.Value = 0;
        }

        /// <summary>
        /// Method implementation to start the StepUp.
        /// </summary>
        public void StartStepUp()
        {
            _stepUpStarted = true;
        }

        /// <summary>
        /// Method implementation to stop the StepUp.
        /// </summary>
        public void StopStepUp()
        {
            _stepUpStarted = false;
        }

        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<uint> _stepUpNode;
        private SimulatedVariableNode<bool> _alternatingBooleanNode;
        private SimulatedVariableNode<int> _randomSignedInt32;
        private SimulatedVariableNode<uint> _randomUnsignedInt32;
        private readonly Random _random = new();
        private bool _stepUpStarted;
        private int _stepUpCycleInPhase;
        private int _alternatingBooleanCycleInPhase;
    }
}
