// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests
{
    using Divergic.Logging.Xunit;
    using Microsoft.Extensions.Logging;

    internal static class Logging
    {
        /// <summary>
        /// Default level
        /// </summary>
        public static LogLevel Level => LogLevel.Information;

        /// <summary>
        /// Configuration
        /// </summary>
        public static LoggingConfig Config => new LoggingConfig { LogLevel = Level };
    }
}
