// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Diagnostics {
    using System;
    using ITraceLogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// Logging interface
    /// </summary>
    public interface ILogger {

        // The following 4 methods allow to log a message, capturing the context
        // (i.e. the method where the log message is generated)
        void Debug(string message, Action context);
        void Info(string message, Action context);
        void Warn(string message, Action context);
        void Error(string message, Action context);

        // The following 4 methods allow to log a message and some data,
        // capturing the context (i.e. the method where the log message is generated)
        void Debug(string message, Func<object> context);
        void Info(string message, Func<object> context);
        void Warn(string message, Func<object> context);
        void Error(string message, Func<object> context);
    }
}
