// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using System.Collections.Generic;

    /// <summary>
    /// Test class to override Exit method
    /// </summary>
    public class CommandLineTest : CommandLineLogger
    {
        /// <summary>
        /// Exit code
        /// </summary>
        public int ExitCode { get; set; } = -1;

        /// <summary>
        /// Warnings reported by StandaloneCliOptions.
        /// </summary>
        public IList<string> Warnings { get; } = new List<string>();

        public CommandLine CommandLine { get; }

        public CommandLineTest(string[] args)
        {
            CommandLine = new CommandLine(args, this);
        }

        /// <summary>
        /// Set exit code
        /// </summary>
        /// <param name="exitCode"></param>
        public override void ExitProcess(int exitCode)
        {
            ExitCode = exitCode;
        }

        /// <inheritdoc/>
        public override void Warning(string messageTemplate)
        {
            Warnings.Add(messageTemplate);
        }

        /// <inheritdoc/>
        public override void Warning<T>(string messageTemplate, T propertyValue0)
        {
            Warnings.Add(messageTemplate + "::" + propertyValue0);
        }
    }
}
