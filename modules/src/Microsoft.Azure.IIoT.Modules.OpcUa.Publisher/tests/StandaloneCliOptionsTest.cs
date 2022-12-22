// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Tests {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime;
    using System.Collections.Generic;

    /// <summary>
    /// Test class to override Exit method
    /// </summary>
    public class StandaloneCliOptionsTest : StandaloneCliOptions {

        /// <summary>
        /// Exit code
        /// </summary>
        public int ExitCode { get; set; } = -1;

        /// <summary>
        /// Warnings reported by StandaloneCliOptions.
        /// </summary>
        public List<string> Warnings = new List<string>();

        public StandaloneCliOptionsTest(string[] args) : base(args) {
        }

        /// <summary>
        /// Set exit code
        /// </summary>
        public override void Exit(int exitCode) {
            ExitCode = exitCode;
        }

        /// <inheritdoc/>
        public override void Warning(string messageTemplate) {
            Warnings.Add(messageTemplate);
        }

        /// <inheritdoc/>
        public override void Warning<T>(string messageTemplate, T propertyValue) {
            Warnings.Add(messageTemplate + "::" + propertyValue.ToString());
        }
    }
}
