
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

                    Logger.Information("==========================================================================");
                    Logger.Information($"OpcPublisher status @ {System.DateTime.UtcNow} (started @ {PublisherStartTime})");
                    Logger.Information("---------------------------------");
                    Logger.Information($"OPC sessions: {NumberOfOpcSessions}");
                    Logger.Information($"connected OPC sessions: {NumberOfConnectedOpcSessions}");
                    Logger.Information($"connected OPC subscriptions: {NumberOfConnectedOpcSubscriptions}");
                    Logger.Information($"OPC monitored items: {NumberOfMonitoredItems}");
                    Logger.Information("---------------------------------");
                    Logger.Information($"monitored items queue bounded capacity: {MonitoredItemsQueueCapacity}");
                    Logger.Information($"monitored items queue current items: {MonitoredItemsQueueCount}");
                    Logger.Information($"monitored item notifications enqueued: {EnqueueCount}");
                    Logger.Information($"monitored item notifications enqueue failure: {EnqueueFailureCount}");
                    Logger.Information($"monitored item notifications dequeued: {DequeueCount}");
                    Logger.Information("---------------------------------");
                    Logger.Information($"messages sent to IoTHub: {SentMessages}");
                    Logger.Information($"last successful msg sent @: {SentLastTime}");
                    Logger.Information($"bytes sent to IoTHub: {SentBytes}");
                    Logger.Information($"avg msg size: {SentBytes / (SentMessages == 0 ? 1 : SentMessages)}");
                    Logger.Information($"msg send failures: {FailedMessages}");
                    Logger.Information($"messages too large to sent to IoTHub: {TooLargeCount}");
                    Logger.Information($"times we missed send interval: {MissedSendIntervalCount}");
                    Logger.Information("---------------------------------");
                    Logger.Information($"current working set in MB: {Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024)}");
                    Logger.Information($"--si setting: {DefaultSendIntervalSeconds}");
                    Logger.Information($"--ms setting: {HubMessageSize}");
                    Logger.Information($"--ih setting: {HubProtocol}");
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
