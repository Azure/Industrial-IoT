
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using static HubCommunication;
    using static Program;
    using static PublisherNodeConfiguration;

    /// <summary>
    /// Class to enable output to the console.
    /// </summary>
    public static class Diagnostics
    {
        public static uint DiagnosticsInterval { get; set; } = 0;

        public static void Init()
        {
            // init data
            _showDiagnosticsInfoTask = null;
            _shutdownTokenSource = new CancellationTokenSource();

            // kick off the task to show diagnostic info
            if (DiagnosticsInterval > 0)
            {
                _showDiagnosticsInfoTask = Task.Run(async () => await ShowDiagnosticsInfoAsync(_shutdownTokenSource.Token));
            }


        }
        public async static Task ShutdownAsync()
        {
            // wait for diagnostic task completion if it is enabled
            if (_showDiagnosticsInfoTask != null)
            {
                _shutdownTokenSource.Cancel();
                await _showDiagnosticsInfoTask;
            }

            _shutdownTokenSource = null;
            _showDiagnosticsInfoTask = null;
        }

        /// <summary>
        /// Fetch diagnostic data.
        /// </summary>
        public static DiagnosticInfoModel GetDiagnosticInfo()
        {
            DiagnosticInfoModel diagnosticInfo = new DiagnosticInfoModel();

            try
            {
                diagnosticInfo.PublisherStartTime = PublisherStartTime;
                diagnosticInfo.NumberOfOpcSessions = NumberOfOpcSessions;
                diagnosticInfo.NumberOfConnectedOpcSessions = NumberOfConnectedOpcSessions;
                diagnosticInfo.NumberOfConnectedOpcSubscriptions = NumberOfConnectedOpcSubscriptions;
                diagnosticInfo.NumberOfMonitoredItems = NumberOfMonitoredItems;
                diagnosticInfo.MonitoredItemsQueueCapacity = MonitoredItemsQueueCapacity;
                diagnosticInfo.MonitoredItemsQueueCount = MonitoredItemsQueueCount;
                diagnosticInfo.EnqueueCount = EnqueueCount;
                diagnosticInfo.EnqueueFailureCount = EnqueueFailureCount;
                diagnosticInfo.NumberOfEvents = NumberOfEvents;
                diagnosticInfo.SentMessages = SentMessages;
                diagnosticInfo.SentLastTime = SentLastTime;
                diagnosticInfo.SentBytes = SentBytes;
                diagnosticInfo.FailedMessages = FailedMessages;
                diagnosticInfo.TooLargeCount = TooLargeCount;
                diagnosticInfo.MissedSendIntervalCount = MissedSendIntervalCount;
                diagnosticInfo.WorkingSetMB = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
                diagnosticInfo.DefaultSendIntervalSeconds = DefaultSendIntervalSeconds;
                diagnosticInfo.HubMessageSize = HubMessageSize;
                diagnosticInfo.HubProtocol = HubProtocol;
            }
            catch
            {
            }
            return diagnosticInfo;
        }

        /// <summary>
        /// Kicks of the task to show diagnostic information each 30 seconds.
        /// </summary>
        public static async Task ShowDiagnosticsInfoAsync(CancellationToken ct)
        {
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    await Task.Delay((int)DiagnosticsInterval * 1000, ct);

                    DiagnosticInfoModel diagnosticInfo = GetDiagnosticInfo();
                    Logger.Information("==========================================================================");
                    Logger.Information($"OpcPublisher status @ {System.DateTime.UtcNow} (started @ {diagnosticInfo.PublisherStartTime})");
                    Logger.Information("---------------------------------");
                    Logger.Information($"OPC sessions: {diagnosticInfo.NumberOfOpcSessions}");
                    Logger.Information($"connected OPC sessions: {diagnosticInfo.NumberOfConnectedOpcSessions}");
                    Logger.Information($"connected OPC subscriptions: {diagnosticInfo.NumberOfConnectedOpcSubscriptions}");
                    Logger.Information($"OPC monitored items: {diagnosticInfo.NumberOfMonitoredItems}");
                    Logger.Information("---------------------------------");
                    Logger.Information($"monitored items queue bounded capacity: {diagnosticInfo.MonitoredItemsQueueCapacity}");
                    Logger.Information($"monitored items queue current items: {diagnosticInfo.MonitoredItemsQueueCount}");
                    Logger.Information($"monitored item notifications enqueued: {diagnosticInfo.EnqueueCount}");
                    Logger.Information($"monitored item notifications enqueue failure: {diagnosticInfo.EnqueueFailureCount}");
                    Logger.Information("---------------------------------");
                    Logger.Information($"messages sent to IoTHub: {diagnosticInfo.SentMessages}");
                    Logger.Information($"last successful msg sent @: {diagnosticInfo.SentLastTime}");
                    Logger.Information($"bytes sent to IoTHub: {diagnosticInfo.SentBytes}");
                    Logger.Information($"avg msg size: {diagnosticInfo.SentBytes / (diagnosticInfo.SentMessages == 0 ? 1 : diagnosticInfo.SentMessages)}");
                    Logger.Information($"msg send failures: {diagnosticInfo.FailedMessages}");
                    Logger.Information($"messages too large to sent to IoTHub: {diagnosticInfo.TooLargeCount}");
                    Logger.Information($"times we missed send interval: {diagnosticInfo.MissedSendIntervalCount}");
                    Logger.Information($"number of events: {diagnosticInfo.NumberOfEvents}");
                    Logger.Information("---------------------------------");
                    Logger.Information($"current working set in MB: {diagnosticInfo.WorkingSetMB}");
                    Logger.Information($"--si setting: {diagnosticInfo.DefaultSendIntervalSeconds}");
                    Logger.Information($"--ms setting: {diagnosticInfo.HubMessageSize}");
                    Logger.Information($"--ih setting: {diagnosticInfo.HubProtocol}");
                    Logger.Information("==========================================================================");
                }
                catch
                {
                }
            }
        }

        private static CancellationTokenSource _shutdownTokenSource;
        private static Task _showDiagnosticsInfoTask;
    }
}
