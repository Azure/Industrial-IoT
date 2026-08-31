// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using System;
    using Xunit;

    /// <summary>
    /// Locks the contract that keeps a bad command line from taking the whole
    /// test run down with it.
    /// </summary>
    public sealed class FailTestNotHostCommandLineLoggerTests
    {
        [Theory]
        [InlineData("--mm=FullSamples")]
        [InlineData("--mm=Samples")]
        [InlineData("--bs=not-a-number")]
        public void RejectedOptionThrowsInsteadOfEndingTheProcess(string argument)
        {
            // The default logger calls Runtime.Exit here, which inside the
            // integration fixture kills the test host: the run aborts, the
            // finished tests are recorded as passed, no failure is recorded,
            // and the test blamed is whichever one happened to be running.
            var logger = new FailTestNotHostCommandLineLogger();

            var thrown = Assert.Throws<InvalidOperationException>(
                () => new CommandLine([argument], logger));

            // The message has to carry the reason, otherwise it just moves the
            // mystery rather than solving it.
            Assert.Contains("terminating the test host", thrown.Message,
                StringComparison.Ordinal);
            Assert.Contains("Reported:", thrown.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void AcceptedCommandLineDoesNotThrow()
        {
            var logger = new FailTestNotHostCommandLineLogger();

            var commandLine = new CommandLine(
                ["--mm=FullNetworkMessages", "--me=Json"], logger);

            Assert.NotEmpty(commandLine);
        }

        // Every messaging mode the Counter soaks and jitter diagnostics drive
        // the publisher with must still be a mode the publisher accepts. These
        // are passed as strings, so the compiler cannot catch a removal the way
        // it caught the MessagingMode.FullSamples uses, and a rejected value
        // aborts the whole run rather than failing one test.
        [Theory]
        [InlineData("FullNetworkMessages")]
        [InlineData("PubSub")]
        public void MessagingModesUsedByTheSoaksAreStillAccepted(string mode)
        {
            var logger = new FailTestNotHostCommandLineLogger();

            var commandLine = new CommandLine([$"--mm={mode}"], logger);

            Assert.NotEmpty(commandLine);
        }
    }
}
