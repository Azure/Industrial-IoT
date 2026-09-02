// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A <see cref="CommandLineLogger"/> that turns a fatal command line error
    /// into a failure of the test that caused it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="CommandLine"/> ends the process when it cannot parse its
    /// arguments, which is right for the module and wrong here: the
    /// integration fixture hosts the publisher inside the test host, so the
    /// default logger takes the whole run down with it.
    /// </para>
    /// <para>
    /// The failure mode is deliberately nasty. The run aborts with
    /// "Test host process crashed", the results file records the tests that
    /// had already finished as passed and records no failure at all, and the
    /// test named as running at the time is whichever one the host happened to
    /// be on - which is frequently not the one that passed the bad argument.
    /// It has now cost two separate investigations: once when
    /// MqttConfigurationIntegrationTests still passed the removed
    /// <c>--mm=FullSamples</c>, and again when the Counter soaks did.
    /// </para>
    /// <para>
    /// Throwing instead keeps the diagnosis local: the offending test fails,
    /// names the option, and the rest of the run continues.
    /// </para>
    /// </remarks>
    internal sealed class FailTestNotHostCommandLineLogger : CommandLineLogger
    {
        /// <inheritdoc/>
        public override void ExitProcess(int exitCode)
        {
            if (exitCode == 0)
            {
                //
                // --help and --help-env exit zero after writing their output.
                // No test passes them, and if one ever does it wants the text,
                // not a torn down host.
                //
                return;
            }
            throw new InvalidOperationException(
                $"The publisher rejected its command line and would have exited " +
                $"with code {exitCode}, terminating the test host. " +
                $"Reported: {string.Join("; ", _warnings)}");
        }

        /// <inheritdoc/>
        public override void Warning(string messageTemplate)
        {
            _warnings.Add(messageTemplate);
            base.Warning(messageTemplate);
        }

        /// <inheritdoc/>
        public override void Warning<T0>(string messageTemplate, T0 propertyValue0)
        {
            _warnings.Add(messageTemplate.Replace("{0}",
                propertyValue0?.ToString() ?? "null", StringComparison.Ordinal));
            base.Warning(messageTemplate, propertyValue0);
        }

        private readonly List<string> _warnings = [];
    }
}
