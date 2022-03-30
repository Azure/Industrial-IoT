// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.IIoT.Modules.Publisher.Tests {

    /// <summary>
    /// Class to test Cli options
    /// </summary>
    public class CliTests {

        /// <summary>
        /// ValidOptionTest
        /// </summary>
        [Theory]
        [InlineData("testValue", new string[] { "-dc", "testValue" })]
        [InlineData("testValue", new string[] { "--dc", "testValue" })]
        [InlineData("testValue", new string[] { "-deviceconnectionstring", "testValue" })]
        [InlineData("testValue", new string[] { "--deviceconnectionstring", "testValue" })]
        public void ValidOptionTest(string expected, string[] param) {

            var result = new StandaloneCliOptions(param);

            result.Count()
                .Should()
                .Be(1);

            result.Values.Should()
                .Equal(expected);
        }

        /// <summary>
        /// LegacyOptionTest
        /// </summary>
        [Theory]
        [InlineData("testValue", new string[] { "-tc", "testValue" })]
        [InlineData("testValue", new string[] { "--tc", "testValue" })]
        [InlineData("testValue", new string[] { "-telemetryconfigfile", "testValue" })]
        [InlineData("testValue", new string[] { "--telemetryconfigfile", "testValue" })]
        public void LegacyOptionTest(string expected, string[] param) {

            var result = new StandaloneCliOptions(param);

            result.Count()
                .Should()
                .Be(0);
        }

        /// <summary>
        /// UnsupportedOptionTest
        /// </summary>
        [Theory]
        [InlineData("testValue", new string[] { "-xx" })]
        [InlineData("testValue", new string[] { "--xx" })]
        public void UnsupportedOptionTest(string expected, string[] param) {

            var result = new StandaloneCliOptions(param);

            result.Count()
                .Should()
                .Be(0);
        }

        /// <summary>
        /// MissingOptionParameterTest
        /// </summary>
        [Theory]
        [InlineData("testValue", new string[] { "-dc" })]
        [InlineData("testValue", new string[] { "--dc" })]
        [InlineData("testValue", new string[] { "-deviceconnectionstring" })]
        [InlineData("testValue", new string[] { "--deviceconnectionstring" })]
        public void MissingOptionParameterTest(string expected, string[] param) {

            var result = new StandaloneCliOptionsTest(param);

            result.ExitCode
                .Should()
                .Be(160);
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
