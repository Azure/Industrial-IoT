// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Serilog.Core;
    using Serilog.Events;

    /// <summary>
    /// Log control
    /// </summary>
    public static class LogControl {

        /// <summary>
        /// Level switcher
        /// </summary>
        public static LoggingLevelSwitch Level { get; } = new LoggingLevelSwitch(LogEventLevel.Information);
    }
}
