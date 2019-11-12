// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher
{
    using System;
    public static class IotEdgeIndicator
    {
        public static bool RunsAsIotEdgeModule => runsAsIotEdgeModule;

        static IotEdgeIndicator()
        {
            runsAsIotEdgeModule = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME")) &&
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IOTEDGE_MODULEGENERATIONID")) &&
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IOTEDGE_WORKLOADURI")) &&
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID")) &&
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IOTEDGE_MODULEID"));
        }

        private static readonly bool runsAsIotEdgeModule;
    }
}
