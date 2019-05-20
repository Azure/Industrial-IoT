using System;

namespace OpcPublisher
{
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
