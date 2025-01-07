// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Plc.PluginNodes
{
    using Plc.PluginNodes.Models;
    using PlcModel;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Test;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Timers;

    /// <summary>
    /// Complex type boiler node.
    /// </summary>
    public sealed class ComplexTypePlcPluginNode : IPluginNodes, IDisposable
    {
        public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();
        public TimeService TimeService { get; set; }
        public ILogger Logger { get; set; }
        public uint ScaleUnits { get; set; }

        private PlcNodeManager _plcNodeManager;
        private PlcState _node;
        private ITimer _nodeGenerator;

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;

            AddNodes(methodsFolder);
        }

        public void Dispose()
        {
            _node.Dispose();
            _nodeGenerator.Dispose();
        }

        public void StartSimulation()
        {
            _nodeGenerator = TimeService.NewTimer(UpdatePlc1, 1000);
        }

        public void StopSimulation()
        {
            if (_nodeGenerator != null)
            {
                _nodeGenerator.Enabled = false;
            }
        }

        private void AddNodes(FolderState methodsFolder)
        {
            // Load complex types from binary uanodes file.
            _plcNodeManager.LoadPredefinedNodes(LoadPredefinedNodes);

            // Find the Plc1 node that was created when the model was loaded.
            var passiveNode = (BaseObjectState)_plcNodeManager.FindPredefinedNode(
                new NodeId(PlcModel.Objects.Plc1, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.PlcSimulation]), typeof(BaseObjectState));

            // Convert to node that can be manipulated within the server.
            _node = new PlcState(null);
            _node.Create(_plcNodeManager.SystemContext, passiveNode);
            _node.PlcStatus.Value = new PlcDataType();

            _plcNodeManager.AddPredefinedNode(_node);

            // Create heater on/off methods.
            var heaterOnMethod = _plcNodeManager.CreateMethod(
                methodsFolder,
                path: "HeaterOn",
                name: "HeaterOn",
                "Turn the heater on",
                NamespaceType.PlcSimulation);
            SetHeaterOnMethodProperties(ref heaterOnMethod);
            var heaterOffMethod = _plcNodeManager.CreateMethod(
                methodsFolder,
                path: "HeaterOff",
                name: "HeaterOff",
                "Turn the heater off",
                NamespaceType.PlcSimulation);
            SetHeaterOffMethodProperties(ref heaterOffMethod);

            Nodes = new List<NodeWithIntervals>
            {
                new() {
                    NodeId = "Plc",
                    Namespace = Plc.Namespaces.PlcSimulation
                }
            };
        }

        /// <summary>
        /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
        /// </summary>
        /// <param name="context"></param>
        private NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            var type = GetType().GetTypeInfo();
            var predefinedNodes = new NodeStateCollection();
            predefinedNodes.LoadFromBinaryResource(context,
                $"{type.Assembly.GetName().Name}.Generated.Plc.Design.PlcModel.PredefinedNodes.uanodes",
                typeof(PlcNodeManager).GetTypeInfo().Assembly,
                updateTables: true);
            return predefinedNodes;
        }

        private void UpdatePlc1(object state, ElapsedEventArgs elapsedEventArgs)
        {
            var newValue = new PlcDataType
            {
                HeaterState = _node.PlcStatus.Value.HeaterState
            };

            var currentTemperatureBottom = _node.PlcStatus.Value.Temperature.Bottom;
            var newTemperature = newValue.Temperature;

            if (_node.PlcStatus.Value.HeaterState == PlcHeaterStateType.On)
            {
                // Heater on, increase by 1.
                newTemperature.Bottom = currentTemperatureBottom + 1;
            }
            else
            {
                // Heater off, decrease down to a minimum of 20.
                newTemperature.Bottom = currentTemperatureBottom > 20
                    ? currentTemperatureBottom - 1
                    : currentTemperatureBottom;
            }

            // Top is always 5 degrees less than bottom, with a minimum value of 20.
            newTemperature.Top = Math.Max(20, newTemperature.Bottom - 5);

            // Pressure is always 100_000 + bottom temperature.
            newValue.Pressure = 100_000 + newTemperature.Bottom;

            // Change complex value in one atomic step.
            _node.PlcStatus.Value = newValue;
            _node.PlcStatus.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: true);
        }

        private void SetHeaterOnMethodProperties(ref MethodState method)
        {
            method.OnCallMethod += OnHeaterOnCall;
        }

        private void SetHeaterOffMethodProperties(ref MethodState method)
        {
            method.OnCallMethod += OnHeaterOffCall;
        }

        /// <summary>
        /// Method to turn the heater on. Executes synchronously.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        private ServiceResult OnHeaterOnCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            _node.PlcStatus.Value.HeaterState = PlcHeaterStateType.On;
            Logger.LogDebug("OnHeaterOnCall method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to turn the heater off. Executes synchronously.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        private ServiceResult OnHeaterOffCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            _node.PlcStatus.Value.HeaterState = PlcHeaterStateType.Off;
            Logger.LogDebug("OnHeaterOffCall method called");
            return ServiceResult.Good;
        }
    }
}
