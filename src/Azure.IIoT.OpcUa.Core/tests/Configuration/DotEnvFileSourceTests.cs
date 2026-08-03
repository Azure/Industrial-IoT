// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Configuration
{
    using Microsoft.Extensions.Configuration;
    using System;
    using System.IO;
    using Xunit;

    public sealed class DotEnvFileSourceTests
    {
        [Fact]
        public void AddFromDotEnvFileLoadsKeysValuesAndEscapes()
        {
            var path = Path.Combine(AppContext.BaseDirectory,
                $"dotenv-{Guid.NewGuid():N}.env");
            try
            {
                File.WriteAllLines(path,
                [
                    "#COMMENT=ignored",
                    " Plain = value ",
                    "Nested__Key=line\\nnext\\r",
                    "WITHOUT_EQUALS",
                    "Plain=last"
                ]);

                var configuration = new ConfigurationBuilder()
                    .AddFromDotEnvFile(path)
                    .Build();

                Assert.Equal("last", configuration["Plain"]);
                Assert.Equal("line\nnext\r", configuration["Nested:Key"]);
                Assert.Null(configuration["COMMENT"]);
                Assert.Null(configuration["WITHOUT_EQUALS"]);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void AddFromDotEnvFileIgnoresMissingFile()
        {
            var path = Path.Combine(AppContext.BaseDirectory,
                $"missing-{Guid.NewGuid():N}.env");

            var configuration = new ConfigurationBuilder()
                .AddFromDotEnvFile(path)
                .Build();

            Assert.Null(configuration["Any"]);
        }

        [Fact]
        public void SourceBuildCanBeCalledDirectly()
        {
            var source = new DotEnvFileSource(string.Empty);

            var provider = source.Build(new ConfigurationBuilder());

            Assert.NotNull(provider);
        }
    }
}
