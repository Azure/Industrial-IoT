// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Configuration
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using Xunit;

    public sealed class ConfigureOptionBaseTests
    {
        [Fact]
        public void ConstructorUsesEmptyConfigurationWhenNull()
        {
            var options = new TestConfigure(null!);

            Assert.NotNull(options.Configuration);
        }

        [Fact]
        public void GetStringTrimsAndDefaultsEmptyValues()
        {
            var options = Create(new Dictionary<string, string?>
            {
                ["value"] = "  trimmed  ",
                ["empty"] = ""
            });

            Assert.Equal("trimmed", options.StringOrDefault("value"));
            Assert.Null(options.StringOrDefault("empty"));
            Assert.Equal("fallback", options.StringOrDefault("empty", "fallback"));
        }

        [Theory]
        [InlineData("YES", true)]
        [InlineData("n", false)]
        [InlineData("unknown", null)]
        public void GetBoolOrNullParsesLegacyAliases(string value, bool? expected)
        {
            var options = Create(new Dictionary<string, string?> { ["flag"] = value });

            Assert.Equal(expected, options.BoolOrNull("flag"));
        }

        [Fact]
        public void GetBoolOrDefaultReturnsProvidedDefaultForUnknownValue()
        {
            var options = Create(new Dictionary<string, string?>
            {
                ["flag"] = "unknown"
            });

            Assert.Equal(true, options.BoolOrDefault("flag", true));
        }

        [Fact]
        public void GetDurationAndIntHelpersParseOrDefault()
        {
            var options = Create(new Dictionary<string, string?>
            {
                ["duration"] = "00:00:05",
                ["badDuration"] = "not-a-duration",
                ["number"] = " 42 ",
                ["badNumber"] = "not-a-number"
            });

            Assert.Equal(TimeSpan.FromSeconds(5), options.DurationOrNull("duration"));
            Assert.Equal(TimeSpan.FromMinutes(1),
                options.DurationOrDefault("badDuration", TimeSpan.FromMinutes(1)));
            Assert.Equal(42, options.IntOrNull("number"));
            Assert.Equal(7, options.IntOrDefault("badNumber", 7));
        }

        [Fact]
        public void NormalizeLegacyBooleanAliasesKeepsProviderPrecedence()
        {
            var options = Create(new Dictionary<string, string?>
            {
                ["Section:Flag"] = "YES",
                ["Other"] = "NO"
            });

            var normalized = options.Normalize("Flag");

            Assert.Equal(bool.TrueString, normalized["Section:Flag"]);
            Assert.Equal("NO", normalized["Other"]);
        }

        [Fact]
        public void ConfigureOptionBaseTDelegatesUnnamedConfigure()
        {
            var config = new TestNamedConfigure(new ConfigurationBuilder().Build());
            var options = new SampleOptions();

            config.Configure(options);

            Assert.Null(config.LastName);
            Assert.Same(options, config.LastOptions);
        }

        [Fact]
        public void PostConfigureToOptionsBindsAndPostConfigures()
        {
            var config = new TestPostConfigure(new ConfigurationBuilder().Build());

            var options = config.ToOptions();

            Assert.Equal(Options.DefaultName, config.LastName);
            Assert.Equal(true, options.Value.PostConfigured);
        }

        private static TestConfigure Create(IDictionary<string, string?> values)
        {
            return new TestConfigure(new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build());
        }

        private sealed class TestConfigure : ConfigureOptionBase
        {
            public TestConfigure(IConfiguration configuration) : base(configuration)
            {
            }

            public string? StringOrDefault(string key)
            {
                return GetStringOrDefault(key);
            }

            public string StringOrDefault(string key, string defaultValue)
            {
                return GetStringOrDefault(key, defaultValue);
            }

            public bool? BoolOrNull(string key)
            {
                return GetBoolOrNull(key);
            }

            public bool BoolOrDefault(string key, bool defaultValue)
            {
                return GetBoolOrDefault(key, defaultValue);
            }

            public TimeSpan? DurationOrNull(string key)
            {
                return GetDurationOrNull(key);
            }

            public TimeSpan DurationOrDefault(string key, TimeSpan defaultValue)
            {
                return GetDurationOrDefault(key, defaultValue);
            }

            public int? IntOrNull(string key)
            {
                return GetIntOrNull(key);
            }

            public int IntOrDefault(string key, int defaultValue)
            {
                return GetIntOrDefault(key, defaultValue);
            }

            public IConfiguration Normalize(params string[] keys)
            {
                return NormalizeLegacyBooleanAliases(keys);
            }
        }

        private sealed class TestNamedConfigure :
            ConfigureOptionBase<SampleOptions>
        {
            public string? LastName { get; private set; }
            public SampleOptions? LastOptions { get; private set; }

            public TestNamedConfigure(IConfiguration configuration) :
                base(configuration)
            {
            }

            public override void Configure(string? name, SampleOptions options)
            {
                LastName = name;
                LastOptions = options;
            }
        }

        private sealed class TestPostConfigure :
            PostConfigureOptionBase<SampleOptions>
        {
            public string? LastName { get; private set; }

            public TestPostConfigure(IConfiguration configuration) :
                base(configuration)
            {
            }

            public override void PostConfigure(string? name, SampleOptions options)
            {
                LastName = name;
                options.PostConfigured = true;
            }
        }

        private sealed class SampleOptions
        {
            public SampleOptions()
            {
            }

            public bool PostConfigured { get; set; }
        }
    }
}
