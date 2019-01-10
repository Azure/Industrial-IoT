
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Core;

namespace OpcPublisher
{
    using Serilog;
    using Serilog.Configuration;
    using Serilog.Events;
    using System;
    using System.Collections.Generic;
    using static HubCommunication;
    using static Program;
    using static PublisherNodeConfiguration;

    /// <summary>
    /// Class to enable output to the console.
    /// </summary>
    public static class Diagnostics
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
        public static DiagnosticInfoMethodResponseModel GetDiagnosticInfo()
        {
            DiagnosticInfoMethodResponseModel diagnosticInfo = new DiagnosticInfoMethodResponseModel();

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
        /// Fetch diagnostic log data.
        /// </summary>
        public static async Task<DiagnosticLogMethodResponseModel> GetDiagnosticLogAsync()
        {
            DiagnosticLogMethodResponseModel diagnosticLogMethodResponseModel = new DiagnosticLogMethodResponseModel();
            diagnosticLogMethodResponseModel.MissedMessageCount = _missedMessageCount;
            diagnosticLogMethodResponseModel.LogMessageCount = _logMessageCount;
            diagnosticLogMethodResponseModel.StartupLogMessageCount = _startupLog.Count;

            if (StartupCompleted)
            {
                List<string> log = new List<string>();

                if (DiagnosticsInterval >= 0)
                {
                    await _logQueueSemaphore.WaitAsync();
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
                }
                else
                {
                    _startupLog.Add("Diagnostic log is disabled in OPC Publisher. Please use --di to enable it.");
                    log.Add("Diagnostic log is disabled in OPC Publisher. Please use --di to enable it.");
                }
                diagnosticLogMethodResponseModel.StartupLog = _startupLog.ToArray();
                diagnosticLogMethodResponseModel.Log = log.ToArray();
            }
            return diagnosticLogMethodResponseModel;
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

                    DiagnosticInfoMethodResponseModel diagnosticInfo = GetDiagnosticInfo();
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
            Diagnostics.WriteLog(message);
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
                    Diagnostics.WriteLog(log);
                }
            }
        }

        private string FormatMessage(LogEvent logEvent)
        {
            return $"[{logEvent.Timestamp:T} {logEvent.Level.ToString().Substring(0, 3).ToUpper()}] {logEvent.RenderMessage()}";
        }

        private List<string> FormatException(LogEvent logEvent)
        {
            List<string> exceptionLog = null;
            if (logEvent.Exception != null)
            {
                exceptionLog = new List<string>();
                exceptionLog.Add(logEvent.Exception.Message);
                exceptionLog.Add(logEvent.Exception.StackTrace.ToString());
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
