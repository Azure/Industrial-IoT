using System.Threading;

namespace OpcPublisher
{
    using System;
    using static Program;

    /// <summary>
    /// Class with unit test helper methods.
    /// </summary>
    public static class UnitTestHelper
    {
        public static string GetMethodName([System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            return memberName;
        }

        public static int WaitTilItemsAreMonitored()
        {
            // wait till monitoring starts
            int iter = 0;
            int startNum = NodeConfiguration.NumberOfOpcMonitoredItemsMonitored;
            while (NodeConfiguration.NumberOfOpcMonitoredItemsMonitored  == 0 && iter < _maxIterations)
            {
                Thread.Sleep(_sleepMilliseconds);
                iter++;
            }
            return iter < _maxIterations ? (iter * _sleepMilliseconds) / 1000 : -1;
        }
        public static int WaitTilItemsAreMonitoredAndFirstEventReceived()
        {
            // wait till monitoring starts
            int iter = 0;
            long numberOfEventsStart = HubCommunicationBase.NumberOfEvents;
            while ((NodeConfiguration.NumberOfOpcMonitoredItemsMonitored == 0 || (HubCommunicationBase.NumberOfEvents - numberOfEventsStart) == 0) && iter < _maxIterations)
            {
                Thread.Sleep(_sleepMilliseconds);
                iter++;
            }
            return iter < _maxIterations ? (iter * _sleepMilliseconds) / 1000 : -1;
        }

        public static void SetPublisherDefaults()
        {
            OpcApplicationConfiguration.OpcSamplingInterval = 1000;
            OpcApplicationConfiguration.OpcPublishingInterval = 0;
            HubCommunicationBase.DefaultSendIntervalSeconds = 0;
            HubCommunicationBase.HubMessageSize = 0;
            OpcMonitoredItem.SkipFirstDefault = false;
            OpcMonitoredItem.HeartbeatIntervalDefault = 0;
        }

        private const int _maxTimeSeconds = 30;
        private const int _sleepMilliseconds = 100;
        private const int _maxIterations = (_maxTimeSeconds * 1000) / _sleepMilliseconds;
    }
}
