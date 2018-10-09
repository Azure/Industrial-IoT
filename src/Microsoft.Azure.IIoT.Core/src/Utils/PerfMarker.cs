// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Helper to log perf
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
            _logger.Info($"Start {_name} ...");
        }

        /// <summary>
        /// Writes step to log
        /// </summary>
        /// <param name="step"></param>
        public void StepCompleted(string step) {
            _logger.Info($"    {_name}: {step} took {_sw.Elapsed}");
            _sw.Restart();
        }

        /// <inheritdoc/>
        public void Dispose() {
            _sw.Stop();
            _logger.Info($"... {_name} completed.");
        }

        private readonly ILogger _logger;
        private readonly string _name;
        private readonly Stopwatch _sw;
    }
}
