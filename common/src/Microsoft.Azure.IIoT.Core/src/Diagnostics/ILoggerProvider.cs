// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Diagnostics {
    using Serilog;

    /// <summary>
    /// Logger provider
    /// </summary>
    public interface ILoggerProvider {

        /// <summary>
        /// Root logger
        /// </summary>
        /// <returns></returns>
        ILogger Logger { get; }
    }
}
