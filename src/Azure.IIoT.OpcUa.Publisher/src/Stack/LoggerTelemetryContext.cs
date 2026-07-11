// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Microsoft.Extensions.Logging;
    using Opc.Ua;

    /// <summary>
    /// Adapts the host's <see cref="ILoggerFactory"/> to the OPC UA 2.0 stack
    /// <see cref="ITelemetryContext"/> abstraction so the stack logs and meters
    /// through the application's configured logging pipeline instead of owning
    /// its own default logger factory.
    /// </summary>
    internal sealed class LoggerTelemetryContext : TelemetryContextBase
    {
        /// <summary>
        /// Create a telemetry context over the provided logger factory.
        /// </summary>
        /// <param name="loggerFactory"></param>
        public LoggerTelemetryContext(ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
        }
    }
}
