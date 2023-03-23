namespace Plc.PluginNodes
{
    using Plc.PluginNodes.Models;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Test;
    using System.Collections.Generic;
    using System.Web;

    /// <summary>
    /// Node with special chars in name and ID.
    /// </summary>
    public class SpecialCharNamePluginNode : IPluginNodes
    {
        public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();
        public TimeService TimeService { get; set; }
        public ILogger Logger { get; set; }

        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<uint> _node;

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;

#pragma warning disable CA2000 // Dispose objects before losing scope
            var folder = _plcNodeManager.CreateFolder(
                telemetryFolder,
                path: "Special",
                name: "Special",
                NamespaceType.PlcApplications);
#pragma warning restore CA2000 // Dispose objects before losing scope

            AddNodes(folder);
        }

        public void StartSimulation()
        {
            _node.Start(value => value + 1, periodMs: 1000);
        }

        public void StopSimulation()
        {
            _node.Stop();
        }

        private void AddNodes(FolderState folder)
        {
            var SpecialChars = HttpUtility.HtmlDecode(@"&quot;!&#167;$%&amp;/()=?`&#180;\+~*&#39;#_-:.;,&lt;&gt;|@^&#176;€&#181;{[]}");

            _node = _plcNodeManager.CreateVariableNode<uint>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: "Special_" + SpecialChars,
                    name: SpecialChars,
                    new NodeId((uint)BuiltInType.UInt32),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentReadOrWrite,
                    "Constantly increasing value",
                    NamespaceType.PlcApplications,
                    defaultValue: (uint)0));

            Nodes = new List<NodeWithIntervals>
            {
                new NodeWithIntervals
                {
                    NodeId = "Special_" + SpecialChars,
                    Namespace = Plc.Namespaces.PlcApplications,
                },
            };
        }
    }
}
