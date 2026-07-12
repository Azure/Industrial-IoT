// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using FluentAssertions;
    using System.Collections.Generic;
    using System.IO;
    using Xunit;

    public sealed class CommandLineOptionParserTests
    {
        [Theory]
        [InlineData("--publisher=value")]
        [InlineData("-p", "value")]
        public void RequiredValueAliasesAreParsed(params string[] arguments)
        {
            string result = null;
            var parser = new CommandLineOptionParser
            {
                { "p|publisher=", "Publisher identifier.\n", value => result = value }
            };

            parser.Parse(arguments).Should().BeEmpty();

            result.Should().Be("value");
        }

        [Theory]
        [InlineData("--strict", true)]
        [InlineData("--strict=False", false)]
        public void OptionalBooleanValuesAreParsed(string argument, bool expected)
        {
            bool? result = null;
            var parser = new CommandLineOptionParser
            {
                { "s|strict:", "Strict mode.\n", (bool? value) => result = value ?? true }
            };

            parser.Parse([argument]).Should().BeEmpty();

            result.Should().Be(expected);
        }

        [Fact]
        public void MissingRequiredValuePreservesCompatibilityError()
        {
            var parser = new CommandLineOptionParser
            {
                { "d|device=", "Device connection string.\n", _ => { } }
            };

            var action = () => parser.Parse(["--device"]);

            action.Should().Throw<CommandLineOptionException>()
                .WithMessage("Missing required value for option '--device'.");
        }

        [Fact]
        public void OptionExceptionsPreserveLegacyMessage()
        {
            var exception = new CommandLineOptionException("Bad store type", "apt");

            exception.Message.Should().Be("Bad store type");
            exception.OptionName.Should().Be("apt");
        }

        [Fact]
        public void UnknownArgumentsAreReturnedUnchanged()
        {
            var parser = new CommandLineOptionParser
            {
                { "p|publisher=", "Publisher identifier.\n", _ => { } }
            };

            var unsupported = parser.Parse(["--unknown", "value"]);

            unsupported.Should().Equal("--unknown", "value");
        }

        [Fact]
        public void DoubleDashReturnsTheUnprocessedTail()
        {
            var calls = new List<string>();
            var parser = new CommandLineOptionParser
            {
                { "h|help", "Show help.\n", _ => calls.Add("help") },
                { "id=", "Publisher identifier.\n", value => calls.Add($"id:{value}") }
            };

            var unsupported = parser.Parse(["--", "--help", "--id=after", "tail"]);

            calls.Should().BeEmpty();
            unsupported.Should().Equal("--help", "--id=after", "tail");
        }

        [Fact]
        public void DoubleDashDoesNotInvokeCommandLineHandlers()
        {
            var result = new CommandLineTest(["--", "--help"]);

            result.ExitCode.Should().Be(-1);
            result.CommandLine.Should().BeEmpty();
            result.Warnings.Should().Contain(
                "Option {0} wrong or not supported, please use -h option to get all the supported options.::--help");
        }

        [Fact]
        public void GroupedShortOptionsInvokeHandlersInOrder()
        {
            var calls = new List<string>();
            var parser = new CommandLineOptionParser
            {
                { "h", "Show help.\n", _ => calls.Add("h") },
                { "c", "Enable compliance.\n", _ => calls.Add("c") }
            };

            parser.Parse(["-hc"]).Should().BeEmpty();

            calls.Should().Equal("h", "c");
        }

        [Fact]
        public void GroupedRequiredOptionConsumesRemainingArgument()
        {
            var calls = new List<string>();
            var parser = new CommandLineOptionParser
            {
                { "h", "Show help.\n", _ => calls.Add("h") },
                { "i|id=", "Publisher identifier.\n", value => calls.Add($"id:{value}") }
            };

            parser.Parse(["-hi", "-leading-value"]).Should().BeEmpty();

            calls.Should().Equal("h", "id:-leading-value");
        }

        [Fact]
        public void InvalidGroupedShortOptionUsesLegacyFailure()
        {
            var parser = new CommandLineOptionParser
            {
                { "h", "Show help.\n", _ => { } }
            };

            var action = () => parser.Parse(["-hunknown"]);

            action.Should().Throw<CommandLineOptionException>()
                .WithMessage("Cannot use unregistered option 'u' in bundle '-hunknown'.");
        }

        [Fact]
        public void RequiredValueConsumesLeadingDash()
        {
            string value = null;
            var parser = new CommandLineOptionParser
            {
                { "id=", "Publisher identifier.\n", input => value = input }
            };

            parser.Parse(["--id", "-leading-value"]).Should().BeEmpty();

            value.Should().Be("-leading-value");
        }

        [Fact]
        public void HelpAndEnvironmentVariableOutputAreDeterministic()
        {
            var parser = new CommandLineOptionParser
            {
                "",
                "General",
                "-------",
                "",
                { "p|publisher=", "Publisher identifier.\n", _ => { } },
                { "s|strict:", "Strict mode.\n", (bool? _) => { } },
                { "hidden=", "Hidden option.\n", _ => { }, true }
            };

            using var help = new StringWriter();
            parser.WriteOptionDescriptions(help);
            help.ToString().Should().Contain("General")
                .And.Contain("-p, --publisher=VALUE")
                .And.NotContain("Hidden option.");

            using var environment = new StringWriter();
            CommandLineOptionParser.WriteEnvironmentVariableHelp(environment,
                new List<CommandLineOptionDescriptor>(parser)
                    .FindAll(option => !option.Hidden &&
                        option.OptionValueType != CommandLineOptionValueType.None));

            environment.ToString().Should().Be("""
[
  {
    "key": "publisher",
    "description": "Publisher identifier.\n"
  },
  {
    "key": "strict",
    "description": "Strict mode.\n"
  }
]

""");
        }
    }
}
