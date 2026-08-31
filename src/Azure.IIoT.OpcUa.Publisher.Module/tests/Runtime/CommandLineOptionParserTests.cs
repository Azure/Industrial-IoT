// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using FluentAssertions;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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
        public void BundlesUseSingleCharacterAliasesBeforeOverlappingAliases()
        {
            var calls = new List<string>();
            var parser = new CommandLineOptionParser
            {
                { "h|help", "Show help.\n", value => calls.Add($"h:{value}") },
                { "t|transport=", "Transport.\n", value => calls.Add($"t:{value}") },
                { "ht=", "Direct transport.\n", value => calls.Add($"ht:{value}") }
            };

            parser.Parse(["-htMqtt"]).Should().BeEmpty();

            calls.Should().Equal("h:htMqtt", "t:Mqtt");

            calls.Clear();
            parser.Parse(["-ht=value"]).Should().BeEmpty();

            calls.Should().Equal("ht:value");
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

        [Fact]
        public void RequiredPrototypeTakesPrecedenceOverOptionalAliases()
        {
            var parser = new CommandLineOptionParser
            {
                { "short:|required=", "Required wins.\n", _ => { } }
            };

            var descriptor = Assert.Single(parser);

            Assert.Equal(CommandLineOptionValueType.Required, descriptor.OptionValueType);
            Assert.Equal(["short", "required"], descriptor.GetNames());
        }

        [Theory]
        [InlineData("-o", null)]
        [InlineData("-ovalue", "value")]
        public void OptionalShortBundleValueIsOptional(string argument, string expected)
        {
            string result = "unchanged";
            var parser = new CommandLineOptionParser
            {
                { "o|option:", "Optional value.\n", value => result = value }
            };

            parser.Parse([argument]).Should().BeEmpty();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void MissingRequiredBundleValueReportsShortOptionName()
        {
            var parser = new CommandLineOptionParser
            {
                { "i|id=", "Identifier.\n", _ => { } }
            };

            var action = () => parser.Parse(["-i"]);

            action.Should().Throw<CommandLineOptionException>()
                .WithMessage("Missing required value for option '-i'.");
        }

        [Fact]
        public void LongSwitchIgnoresProvidedValueForNoValueOption()
        {
            string result = null;
            var parser = new CommandLineOptionParser
            {
                { "h|help", "Show help.\n", value => result = value }
            };

            parser.Parse(["--help=ignored"]).Should().BeEmpty();

            Assert.Equal("help", result);
        }

        [Fact]
        public void ParseValueSupportsPublisherOptionTypes()
        {
            uint? uintValue = null;
            ushort? ushortValue = null;
            ushort? nullableUshortValue = 12;
            TimeSpan? timeSpanValue = null;
            DayOfWeek? enumValue = null;
            double? doubleValue = null;
            var parser = new CommandLineOptionParser
            {
                { "uint=", "Unsigned int.\n", (uint value) => uintValue = value },
                { "ushort=", "Unsigned short.\n", (ushort value) => ushortValue = value },
                { "nullable:", "Optional unsigned short.\n", (ushort? value) => nullableUshortValue = value },
                { "timespan=", "Time span.\n", (TimeSpan value) => timeSpanValue = value },
                { "day=", "Enum.\n", (DayOfWeek value) => enumValue = value },
                { "double=", "Fallback conversion.\n", (double value) => doubleValue = value }
            };

            parser.Parse([
                "--uint=4294967295",
                "--ushort=65535",
                "--nullable",
                "--timespan=00:00:03",
                "--day=friday",
                "--double=1.5"
            ]).Should().BeEmpty();

            Assert.Equal(4294967295u, uintValue);
            Assert.Equal((ushort)65535, ushortValue);
            Assert.Null(nullableUshortValue);
            Assert.Equal(TimeSpan.FromSeconds(3), timeSpanValue);
            Assert.Equal(DayOfWeek.Friday, enumValue);
            Assert.Equal(1.5d, doubleValue);
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("de-DE")]
        [InlineData("fr-FR")]
        public void ValuesParseIdenticallyUnderAnyHostCulture(string culture)
        {
            // Command line arguments are machine facing configuration, so a
            // host whose decimal separator is ',' must still read "1.5" as one
            // and a half. Parsing under CultureInfo.CurrentCulture read it as
            // fifteen, silently multiplying the configured value by ten.
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

                double? doubleValue = null;
                TimeSpan? timeSpanValue = null;
                uint? uintValue = null;
                var parser = new CommandLineOptionParser
                {
                    { "double=", "Fallback conversion.\n", (double value) => doubleValue = value },
                    { "timespan=", "Time span.\n", (TimeSpan value) => timeSpanValue = value },
                    { "uint=", "Unsigned int.\n", (uint value) => uintValue = value }
                };

                parser.Parse([
                    "--double=1.5",
                    "--timespan=00:00:03",
                    "--uint=4294967295"
                ]).Should().BeEmpty();

                Assert.Equal(1.5d, doubleValue);
                Assert.Equal(TimeSpan.FromSeconds(3), timeSpanValue);
                Assert.Equal(4294967295u, uintValue);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void InvalidConvertedValueIsReportedWithPrototype()
        {
            var parser = new CommandLineOptionParser
            {
                { "port=", "Port.\n", (ushort _) => { } }
            };

            var action = () => parser.Parse(["--port=not-a-port"]);

            action.Should().Throw<CommandLineOptionException>()
                .WithMessage("Could not convert value 'not-a-port' for option 'port='.")
                .WithInnerException<FormatException>();
        }

        [Fact]
        public void LongHelpDescriptionsWrapWithContinuationIndent()
        {
            var parser = new CommandLineOptionParser
            {
                { "very-long-option-name=", "one two three four five six seven " +
                    "eight nine ten eleven twelve thirteen fourteen fifteen sixteen.\n",
                    _ => { } }
            };

            using var help = new StringWriter();
            parser.WriteOptionDescriptions(help);

            Assert.Contains("  --very-long-option-name=VALUE", help.ToString());
            Assert.Contains(Environment.NewLine + "                               eleven",
                help.ToString());
        }
    }
}
