
using Serilog.Core;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using Serilog;
    using Serilog.Configuration;
    using Serilog.Events;
    using System.Collections.Generic;
    using System.Globalization;
    using static HubCommunication;
    using static Program;
    using static PublisherNodeConfiguration;

    /// <summary>
    /// Class to enable output to the console.
    /// </summary>
    public static class PublisherDiagnostics
    {
        public static int DiagnosticsInterval { get; set; } = 0;

        public static void Init()
        {
            // init data
            _showDiagnosticsInfoTask = null;
            _shutdownTokenSource = new CancellationTokenSource();

            // kick off the task to show diagnostic info
            if (DiagnosticsInterval > 0)
            {
                _showDiagnosticsInfoTask = Task.Run(async () => await ShowDiagnosticsInfoAsync(_shutdownTokenSource.Token).ConfigureAwait(false));
            }


        }
        public async static Task ShutdownAsync()
        {
            // wait for diagnostic task completion if it is enabled
            if (_showDiagnosticsInfoTask != null)
            {
                _shutdownTokenSource.Cancel();
                await _showDiagnosticsInfoTask.ConfigureAwait(false);
            }

            _shutdownTokenSource = null;
            _showDiagnosticsInfoTask = null;
        }

        /// <summary>
        /// Fetch diagnostic data.
        /// </summary>
        public static DiagnosticInfoMethodResponseModel GetDiagnosticInfo()
        {
            DiagnosticInfoMethodResponseModel diagnosticInfo = new DiagnosticInfoMethodResponseModel();

            try
            {
                diagnosticInfo.PublisherStartTime = PublisherStartTime;
                diagnosticInfo.NumberOfOpcSessionsConfigured = NumberOfOpcSessionsConfigured;
                diagnosticInfo.NumberOfOpcSessionsConnected = NumberOfOpcSessionsConnected;
                diagnosticInfo.NumberOfOpcSubscriptionsConfigured = NumberOfOpcSubscriptionsConfigured;
                diagnosticInfo.NumberOfOpcSubscriptionsConnected = NumberOfOpcSubscriptionsConnected;
                diagnosticInfo.NumberOfOpcMonitoredItemsConfigured = NumberOfOpcMonitoredItemsConfigured;
                diagnosticInfo.NumberOfOpcMonitoredItemsMonitored = NumberOfOpcMonitoredItemsMonitored;
                diagnosticInfo.NumberOfOpcMonitoredItemsToRemove = NumberOfOpcMonitoredItemsToRemove;
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
        /// Fetch diagnostic log data.
        /// </summary>
        public static async Task<DiagnosticLogMethodResponseModel> GetDiagnosticLogAsync()
        {
            DiagnosticLogMethodResponseModel diagnosticLogMethodResponseModel = new DiagnosticLogMethodResponseModel();
            diagnosticLogMethodResponseModel.MissedMessageCount = _missedMessageCount;
            diagnosticLogMethodResponseModel.LogMessageCount = _logMessageCount;

            if (DiagnosticsInterval >= 0)
            {
                if (StartupCompleted)
                {
                    List<string> log = new List<string>();
                    await _logQueueSemaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        string message;
                        while ((message = ReadLog()) != null)
                        {
                            log.Add(message);
                        }
                    }
                    finally
                    {
                        diagnosticLogMethodResponseModel.MissedMessageCount = _missedMessageCount;
                        _missedMessageCount = 0;
                        _logQueueSemaphore.Release();
                    }
                    diagnosticLogMethodResponseModel.Log.AddRange(log);
                }
                else
                {
                    diagnosticLogMethodResponseModel.Log.Add("Startup is not yet completed. Please try later.");
                }
            }
            else
            {
                diagnosticLogMethodResponseModel.Log.Add("Diagnostic log is disabled. Please use --di to enable it.");
            }

            return diagnosticLogMethodResponseModel;
        }

        /// <summary>
        /// Fetch diagnostic startup log data.
        /// </summary>
        public static Task<DiagnosticLogMethodResponseModel> GetDiagnosticStartupLogAsync()
        {
            DiagnosticLogMethodResponseModel diagnosticLogMethodResponseModel = new DiagnosticLogMethodResponseModel();
            diagnosticLogMethodResponseModel.MissedMessageCount = 0;
            diagnosticLogMethodResponseModel.LogMessageCount = _startupLog.Count;

            if (DiagnosticsInterval >= 0)
            {
                if (StartupCompleted)
                {
                    diagnosticLogMethodResponseModel.Log.AddRange(_startupLog);
                }
                else
                {
                    diagnosticLogMethodResponseModel.Log.Add("Startup is not yet completed. Please try later.");
                }
            }
            else
            {
                diagnosticLogMethodResponseModel.Log.Add("Diagnostic log is disabled. Please use --di to enable it.");
            }

            return Task.FromResult(diagnosticLogMethodResponseModel);
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
                    await Task.Delay(DiagnosticsInterval * 1000, ct).ConfigureAwait(false);

                    DiagnosticInfoMethodResponseModel diagnosticInfo = GetDiagnosticInfo();
                    Logger.Information("==========================================================================");
                    Logger.Information($"OpcPublisher status @ {System.DateTime.UtcNow} (started @ {diagnosticInfo.PublisherStartTime})");
                    Logger.Information("---------------------------------");
                    Logger.Information($"OPC sessions (configured/connected): {diagnosticInfo.NumberOfOpcSessionsConfigured}/{diagnosticInfo.NumberOfOpcSessionsConnected}");
                    Logger.Information($"OPC subscriptions (configured/connected): {diagnosticInfo.NumberOfOpcSubscriptionsConfigured}/{diagnosticInfo.NumberOfOpcSubscriptionsConnected}");
                    Logger.Information($"OPC monitored items (configured/monitored/to remove): {diagnosticInfo.NumberOfOpcMonitoredItemsConfigured}/{diagnosticInfo.NumberOfOpcMonitoredItemsMonitored}/{diagnosticInfo.NumberOfOpcMonitoredItemsToRemove}");
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

        /// <summary>
        /// Reads a line from the diagnostic log.
        /// Note: caller must take semaphore
        /// </summary>
        private static string ReadLog()
        {
            string message = null;
            try
            {
                message =_logQueue.Dequeue();
            }
            catch
            {
            }
            return message;
        }


        /// <summary>
        /// Writes a line to the diagnostic log.
        /// </summary>
        public static void WriteLog(string message)
        {
            if (StartupCompleted == false)
            {
                _startupLog.Add(message);
                return;
            }

            _logQueueSemaphore.Wait();
            try
            {
                while (_logQueue.Count > _logMessageCount)
                {
                    _logQueue.Dequeue();
                    _missedMessageCount++;
                }
                _logQueue.Enqueue(message);
            }
            finally
            {
                _logQueueSemaphore.Release();
            }
        }

        private static SemaphoreSlim _logQueueSemaphore = new SemaphoreSlim(1);
        private static int _logMessageCount = 100;
        private static int _missedMessageCount;
        private static Queue<string> _logQueue = new Queue<string>();
        private static CancellationTokenSource _shutdownTokenSource;
        private static Task _showDiagnosticsInfoTask;
        private static List<string> _startupLog = new List<string>();
    }

    public class DiagnosticLogSink:ILogEventSink
    {
        public DiagnosticLogSink()
        {
        }

        public void Emit(LogEvent logEvent)
        {
            string message = FormatMessage(logEvent);
            PublisherDiagnostics.WriteLog(message);
            // enable below for testing
            //Console.ForegroundColor = ConsoleColor.Red;
            //Console.WriteLine(message);
            //Console.ResetColor();

            // also dump exception message and stack
            if (logEvent.Exception != null)
            {
                List<string> exceptionLog = FormatException(logEvent);
                foreach (var log in exceptionLog)
                {
                    PublisherDiagnostics.WriteLog(log);
                }
            }
        }

        private static string FormatMessage(LogEvent logEvent)
        {
            return $"[{logEvent.Timestamp:T} {logEvent.Level.ToString().Substring(0, 3).ToUpper(CultureInfo.InvariantCulture)}] {logEvent.RenderMessage()}";
        }

        private static List<string> FormatException(LogEvent logEvent)
        {
            List<string> exceptionLog = null;
            if (logEvent.Exception != null)
            {
                exceptionLog = new List<string>();
                exceptionLog.Add(logEvent.Exception.Message);
                exceptionLog.Add(logEvent.Exception.StackTrace.ToString(CultureInfo.InvariantCulture));
            }
            return exceptionLog;
        }
    }

    public static class DiagnosticLogSinkExtensions
    {
        public static LoggerConfiguration DiagnosticLogSink(
                  this LoggerSinkConfiguration loggerConfiguration)
        {
            return loggerConfiguration.Sink(new DiagnosticLogSink());
        }
    }
}
