namespace Plc.PluginNodes.Models
{
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Test;
    using System.Collections.Generic;

    public interface IPluginNodes
    {
        ILogger Logger { get; set; }
        TimeService TimeService { get; set; }
        IReadOnlyCollection<NodeWithIntervals> Nodes { get; }
        void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager);
        void StartSimulation();
        void StopSimulation();
    }
}
