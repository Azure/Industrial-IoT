// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

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
