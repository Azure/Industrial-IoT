// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using Serilog;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Helper to log perf // TODO: Replace with Serilog Operation nuget
    /// </summary>
    public sealed class PerfMarker : IDisposable {

        /// <summary>
        /// Create marker
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="name"></param>
        public PerfMarker(ILogger logger, string name) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _name = name ?? throw new ArgumentNullException(name);
            _sw = new Stopwatch();
            _sw.Start();
            _logger.Verbose("Start {name} ...", _name);
        }

        /// <summary>
        /// Writes step to log
        /// </summary>
        /// <param name="step"></param>
        public void StepCompleted(string step) {
            _logger.Information("{name}: {step} took {elapsed}", _name, step, _sw.Elapsed);
            _sw.Restart();
        }

        /// <inheritdoc/>
        public void Dispose() {
            _sw.Stop();
            _logger.Information("{name} took {elapsed}.", _name, _sw.Elapsed);
        }

        private readonly ILogger _logger;
        private readonly string _name;
        private readonly Stopwatch _sw;
    }
}
