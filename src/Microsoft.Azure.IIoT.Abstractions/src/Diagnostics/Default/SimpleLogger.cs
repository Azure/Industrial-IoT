// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using System.Reflection;

    /// <summary>
    /// Simple console logger
    /// </summary>
    public class SimpleLogger : ConsoleLogger {

        /// <inheritdoc/>
        protected override string Write(MethodInfo context,
            string text) => text;
    }
}
