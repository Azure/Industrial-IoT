// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Microsoft.AspNetCore.Http.Json;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Xunit;

    /// <summary>
    /// Verifies the web host startup wiring, in particular that the minimal API
    /// JSON options are configured with the shared serializer settings so the
    /// REST surface keeps the exact same wire format as the rest of the pipeline
    /// (and the direct method / SDK path).
    /// </summary>
    public sealed class StartupTests
    {
        [Fact]
        public void ConfigureServicesAppliesSharedJsonOptionsToHttp()
        {
            using var provider = BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<JsonOptions>>().Value;

            Assert.Same(JsonNamingPolicy.CamelCase,
                options.SerializerOptions.PropertyNamingPolicy);
            Assert.True(options.SerializerOptions.PropertyNameCaseInsensitive);
            Assert.Equal(
                JsonNumberHandling.AllowReadingFromString |
                JsonNumberHandling.AllowNamedFloatingPointLiterals,
                options.SerializerOptions.NumberHandling);
        }

        [Fact]
        public void ConfigureServicesRegistersHttpJsonConverters()
        {
            using var provider = BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<JsonOptions>>().Value;

            // The shared converter set (matrix, byte array, enum, ...) is applied
            // so the minimal API endpoints (de)serialize identically to the SDK.
            Assert.NotEmpty(options.SerializerOptions.Converters);
        }

        private static ServiceProvider BuildServiceProvider()
        {
            var configuration = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();
            new Startup(configuration).ConfigureServices(services);
            return services.BuildServiceProvider();
        }
    }
}
