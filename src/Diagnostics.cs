
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using static IotHubMessaging;
    using static OpcPublisher.Workarounds.TraceWorkaround;
    using static Program;
    using static PublisherNodeConfiguration;

    /// <summary>
    /// Class to enable output to the console.
    /// </summary>
    public static class Diagnostics
    {
        public static uint DiagnosticsInterval
        {
            get => _diagnosticsInterval;
            set => _diagnosticsInterval = value;
        }

        public static int IotHubMessagingMessagesSentCount => _messagesSentCount;

        public static void Init()
        {
            // init data
            _showDiagnosticsInfoTask = null;
            _shutdownTokenSource = new CancellationTokenSource();

            // kick off the task to show diagnostic info
            if (_diagnosticsInterval > 0)
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
                    await Task.Delay((int)_diagnosticsInterval * 1000, ct);

                    Trace("==========================================================================");
                    Trace($"OpcPublisher status @ {System.DateTime.UtcNow} (started @ {PublisherStartTime})");
                    Trace("---------------------------------");
                    Trace($"OPC sessions: {NumberOfOpcSessions}");
                    Trace($"connected OPC sessions: {NumberOfConnectedOpcSessions}");
                    Trace($"connected OPC subscriptions: {NumberOfConnectedOpcSubscriptions}");
                    Trace($"OPC monitored items: {NumberOfMonitoredItems}");
                    Trace("---------------------------------");
                    Trace($"monitored items queue bounded capacity: {MonitoredItemsQueueCapacity}");
                    Trace($"monitored items queue current items: {MonitoredItemsQueueCount}");
                    Trace($"monitored item notifications enqueued: {EnqueueCount}");
                    Trace($"monitored item notifications enqueue failure: {EnqueueFailureCount}");
                    Trace($"monitored item notifications dequeued: {DequeueCount}");
                    Trace("---------------------------------");
                    Trace($"messages sent to IoTHub: {SentMessages}");
                    Trace($"last successful msg sent @: {SentLastTime}");
                    Trace($"bytes sent to IoTHub: {SentBytes}");
                    Trace($"avg msg size: {SentBytes / (SentMessages == 0 ? 1 : SentMessages)}");
                    Trace($"msg send failures: {FailedMessages}");
                    Trace($"messages too large to sent to IoTHub: {TooLargeCount}");
                    Trace($"times we missed send interval: {MissedSendIntervalCount}");
                    Trace("---------------------------------");
                    Trace($"current working set in MB: {Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024)}");
                    Trace($"--si setting: {DefaultSendIntervalSeconds}");
                    Trace($"--ms setting: {IotHubMessageSize}");
                    Trace($"--ih setting: {IotHubProtocol}");
                    Trace("==========================================================================");
                }
                catch
                {
                }
            }
        }

        private static uint _diagnosticsInterval = 0;
        private static int _messagesSentCount = 0;
        private static CancellationTokenSource _shutdownTokenSource;
        private static Task _showDiagnosticsInfoTask;
    }
}
