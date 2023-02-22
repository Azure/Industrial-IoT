// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services {
    using Autofac;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;

    /// <summary>
    /// Injectable service that registers logger with stack
    /// </summary>
    public class StackLogger : IStartable {

        /// <summary>
        /// Wrapped logger
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Create stack logger
        /// </summary>
        /// <param name="logger"></param>
        public StackLogger(ILogger logger) {
            Logger = logger;
        }

        /// <inheritdoc/>
        public void Start() {
            Utils.SetLogger(Logger);
        }

        /// <summary>
        /// Helper to use when not using autofac di.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static StackLogger Create(ILogger logger) {
            var stackLogger = new StackLogger(logger);
            stackLogger.Start();
            return stackLogger;
        }
    }
}
