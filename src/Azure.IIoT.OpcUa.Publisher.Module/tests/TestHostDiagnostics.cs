// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Temporary test-host crash diagnostics. The full Module.Tests suite
    /// intermittently aborts with "Test host process crashed" on the
    /// resource-constrained CI build containers, late in the run, with no
    /// captured dump and no managed stack surfaced by vstest. This installs
    /// process-wide handlers (loaded via a module initializer, so they are
    /// active before any test runs) that print the managed crash stack and a
    /// periodic resource sample to stderr, which vstest captures into the test
    /// log. That makes the failure analyzable without relying on a post-mortem
    /// dump. Remove once the crash is root-caused.
    /// </summary>
    internal static class TestHostDiagnostics
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AppDomain.CurrentDomain.UnhandledException += static (_, e) =>
                Log($"UNHANDLED EXCEPTION (terminating={e.IsTerminating}): {e.ExceptionObject}");

            TaskScheduler.UnobservedTaskException += static (_, e) =>
            {
                Log($"UNOBSERVED TASK EXCEPTION: {e.Exception}");
                // Prevent an unobserved faulted task from tearing down the
                // process; if this is the crash cause the run will go green and
                // the logged stack identifies the offending task.
                e.SetObserved();
            };

            AppDomain.CurrentDomain.FirstChanceException += static (_, e) =>
            {
                // Only the rarely-seen, hard-to-diagnose fatal kinds, to avoid
                // flooding the log with ordinary handled exceptions.
                if (e.Exception is OutOfMemoryException or InsufficientExecutionStackException or
                    AccessViolationException)
                {
                    Log($"FIRST-CHANCE FATAL: {e.Exception}");
                }
            };

            AppDomain.CurrentDomain.ProcessExit += static (_, _) =>
                Log($"PROCESS EXIT code={Environment.ExitCode} {Snapshot()}");

            // Periodic resource sampling so a leak/exhaustion trend is visible
            // in the log right up to the crash.
            var timer = new Timer(static _ => Log($"RESOURCES {Snapshot()}"),
                null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
            // Keep the timer alive for the life of the process.
            AppDomain.CurrentDomain.ProcessExit += (_, _) => timer.Dispose();
            GC.KeepAlive(timer);
        }

        private static string Snapshot()
        {
            try
            {
                using var p = Process.GetCurrentProcess();
                return string.Format(CultureInfo.InvariantCulture,
                    "handles={0} threads={1} ws={2}MB priv={3}MB gc={4}MB gen0={5} gen1={6} gen2={7}",
                    p.HandleCount, p.Threads.Count, p.WorkingSet64 / (1024 * 1024),
                    p.PrivateMemorySize64 / (1024 * 1024), GC.GetTotalMemory(false) / (1024 * 1024),
                    GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));
            }
            catch (Exception ex)
            {
                return "snapshot-failed: " + ex.Message;
            }
        }

        private static readonly string kLogPath = BuildLogPath();

        private static string BuildLogPath()
        {
            try
            {
                // The pipeline points TESTHOST_DIAG_DIR at the same folder it
                // harvests dumps from; fall back to the temp directory locally.
                var dir = Environment.GetEnvironmentVariable("TESTHOST_DIAG_DIR");
                if (string.IsNullOrEmpty(dir))
                {
                    dir = System.IO.Path.GetTempPath();
                }
                System.IO.Directory.CreateDirectory(dir);
                return System.IO.Path.Combine(dir,
                    string.Format(CultureInfo.InvariantCulture,
                        "testhost-diag-{0}.log", Environment.ProcessId));
            }
            catch
            {
                return null;
            }
        }

        private static void Log(string message)
        {
            var line = string.Format(CultureInfo.InvariantCulture,
                "[TESTHOST-DIAG {0:HH:mm:ss.fff}] {1}", DateTime.UtcNow, message);
            try
            {
                Console.Error.WriteLine(line);
                Console.Error.Flush();
            }
            catch
            {
                // Diagnostics must never throw.
            }
            // Also write to a file so the trail survives a hard crash and is
            // harvested by the pipeline's dump-collection step (it is written
            // next to the test results under the agent temp directory).
            if (kLogPath != null)
            {
                try
                {
                    System.IO.File.AppendAllText(kLogPath, line + Environment.NewLine);
                }
                catch
                {
                    // Diagnostics must never throw.
                }
            }
        }
    }
}
