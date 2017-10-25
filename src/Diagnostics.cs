
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using static OpcPublisher.Workarounds.TraceWorkaround;
    using static IotHubMessaging;
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
        private static uint _diagnosticsInterval = 0;

        public static int IotHubMessagingMessagesSentCount
        {
            get => _messagesSentCount;
        }
        private static int _messagesSentCount = 0;

        private static CancellationTokenSource _shutdownTokenSource;
        private static Task _showDiagnosticsInfoTask;

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
        public async static Task Shutdown()
        {
            // wait for diagnostic task completion if it is enabled
            if (_showDiagnosticsInfoTask != null)
            {
                _shutdownTokenSource.Cancel();
                await _showDiagnosticsInfoTask;
            }

        }

        /// <summary>
        /// Kicks of the task to show diagnostic information each 30 seconds.
        /// </summary>
        public static async Task ShowDiagnosticsInfoAsync(CancellationToken cancellationtoken)
        {
            while (true)
            {
                try
                {

                    if (cancellationtoken.IsCancellationRequested)
                    {
                        return;
                    }
                    await Task.Delay((int)_diagnosticsInterval * 1000);

                    Trace("======================================================================");
                    Trace($"OpcPublisher status @ {System.DateTime.UtcNow}");
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
                    Trace($"bytes sent to IoTHub: {SentBytes}");
                    Trace($"avg msg size: {SentBytes / (SentMessages == 0 ? 1 : SentMessages)}");
                    Trace($"time in ms for sent msgs: {SentTime}");
                    Trace($"min time in ms for msg: {MinSentTime}");
                    Trace($"max time in ms for msg: {MaxSentTime}");
                    Trace($"avg time in ms for msg: {SentTime / (SentMessages == 0 ? 1 : SentMessages)}");
                    Trace($"msg send failures: {FailedMessages}");
                    Trace($"time in ms for failed msgs: {FailedTime}");
                    Trace($"avg time in ms for failed msg: {FailedTime / (FailedMessages == 0 ? 1 : FailedMessages)}");
                    Trace($"messages too large to sent to IoTHub: {TooLargeCount}");
                    Trace($"times we missed send interval: {MissedSendIntervalCount}");
                    Trace("---------------------------------");
                    Trace($"current working set in MB: {Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024)}");
                    Trace("======================================================================");
                }
                catch
                {
                }
            }
        }

    }
}
