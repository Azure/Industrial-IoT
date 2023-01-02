// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Tests {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime;
    using FluentAssertions;
    using Xunit;

    /// <summary>
    /// Class to test Cli options
    /// </summary>
    public class StandaloneCliTests {

        /// <summary>
        /// ValidOptionTest
        /// </summary>
        [Theory]
        [InlineData("testValue", new string[] { "-dc", "testValue" })]
        [InlineData("testValue", new string[] { "--dc", "testValue" })]
        [InlineData("testValue", new string[] { "-ec", "testValue" })]
        [InlineData("testValue", new string[] { "--ec", "testValue" })]
        [InlineData("testValue", new string[] { "-deviceconnectionstring", "testValue" })]
        [InlineData("testValue", new string[] { "--deviceconnectionstring", "testValue" })]
        public void ValidOptionTest(string expected, string[] param) {

            var result = new StandaloneCliOptions(param);

            result.Count
                .Should()
                .Be(1);

            result.Values.Should()
                .Equal(expected);
        }

        /// <summary>
        /// Valid boolean option test
        /// </summary>
        [Theory]
        [InlineData("True", new string[] { "-aa" })]
        [InlineData("True", new string[] { "--aa" })]
        [InlineData("True", new string[] { "-autoaccept" })]
        [InlineData("True", new string[] { "--autoaccept" })]
        [InlineData("True", new string[] { "--autoaccept=True" })]
        [InlineData("False", new string[] { "--autoaccept=False" })]
        [InlineData("False", new string[] { "-aa=false" })]
        [InlineData("True", new string[] { "-aa=true" })]
        [InlineData("True", new string[] { "--AutoAcceptUntrustedCertificates" })]
        [InlineData("True", new string[] { "--AutoAcceptUntrustedCertificates=True" })]
        public void ValidAutoAcceptUntrustedCertificatesOptionTest(string expected, string[] param) {

            var result = new StandaloneCliOptions(param);

            result.Count
                .Should()
                .Be(1);

            result.Keys.Should()
                .Equal("AutoAcceptUntrustedCertificates");
            result.Values.Should()
                .Equal(expected);
        }

        /// <summary>
        /// LegacyOptionTest
        /// </summary>
        [Theory]
        [InlineData("tc|telemetryconfigfile", new string[] { "-tc", "testValue" })]
        [InlineData("tc|telemetryconfigfile", new string[] { "--tc", "testValue" })]
        [InlineData("tc|telemetryconfigfile", new string[] { "-telemetryconfigfile", "testValue" })]
        [InlineData("tc|telemetryconfigfile", new string[] { "--telemetryconfigfile", "testValue" })]
        public void LegacyOptionTest(string cliOption, string[] param) {

            var result = new StandaloneCliOptionsTest(param);

            result.Count.Should().Be(0);

            result.Warnings.Count.Should().Be(1);
            result.Warnings[0].Should().Be(
                "Legacy option {option} not supported, please use -h option to get all the supported options."
                + "::" + cliOption
            );
        }

        /// <summary>
        /// UnsupportedOptionTest
        /// </summary>
        [Theory]
        [InlineData("-xx")]
        [InlineData("--xx")]
        [InlineData("--unknown")]
        [InlineData("-unknown", "testValue")]
        [InlineData("unknown=testValue")]
        public void UnsupportedOptionTest(params string[] param) {

            var result = new StandaloneCliOptionsTest(param);

            result.Count.Should().Be(0);

            result.Warnings.Count.Should().Be(param.Length);

            for (var i = 0; i < result.Warnings.Count; ++i) {
                var warning = result.Warnings[i];
                warning.Should().Be(
                    "Option {option} wrong or not supported, please use -h option to get all the supported options."
                    + "::" + param[i]
                );
            }
        }

        /// <summary>
        /// MissingOptionParameterTest
        /// </summary>
        [Theory]
        [InlineData("-dc")]
        [InlineData("--dc")]
        [InlineData("-deviceconnectionstring")]
        [InlineData("--deviceconnectionstring")]
        public void MissingOptionParameterTest(params string[] param) {

            var result = new StandaloneCliOptionsTest(param);

            result.ExitCode.Should().Be(160);

            result.Warnings.Count.Should().Be(1);
            result.Warnings[0].Should().Be($"Parse args exception: Missing required value for option '{param[0]}'.");
        }

        /// <summary>
        /// HelpOptionParameterTest
        /// </summary>
        [Theory]
        [InlineData(new object[] { new string[] { "-h" } })]
        [InlineData(new object[] { new string[] { "--h" } })]
        [InlineData(new object[] { new string[] { "-help" } })]
        [InlineData(new object[] { new string[] { "--help" } })]
        public void HelpOptionParameterTest(string[] param) {

            var result = new StandaloneCliOptionsTest(param);

            result.ExitCode
                .Should()
                .Be(0);
        }
    }
}
