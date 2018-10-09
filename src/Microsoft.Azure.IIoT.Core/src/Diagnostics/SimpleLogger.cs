// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Simple CLI console logger which does not log any context.
    /// </summary>
    public class SimpleLogger : ConsoleLogger {

        /// <inheritdoc/>
        protected override void WriteLine(string preamble, string message,
            object[] parameters) {
            if (parameters != null && parameters.Length != 0) {
                Console.WriteLine(
                    $"{message} ({JsonConvertEx.SerializeObject(parameters)})");
            }
            else {
                Console.WriteLine(message);
            }
        }
    }
}
