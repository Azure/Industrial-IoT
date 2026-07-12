// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http.Json;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Verifies the web host startup wiring, in particular that the minimal API
    /// JSON options are configured with the shared serializer settings so the
    /// REST surface keeps the exact same wire format as the rest of the pipeline
    /// (and the direct method / SDK path).
    /// </summary>
    [Trait("Compatibility", "Authoritative")]
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

        [Fact]
        public async Task MinimalApiUsesDataContractWireFormatAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.ConfigureHttpJsonOptions(options =>
                Json.ApplyTo(options.SerializerOptions));
            await using var app = builder.Build();
            app.MapPost("/published-nodes", (PublishedNodesEntryModel model) => model);
            await app.StartAsync();
            using var client = app.GetTestClient();

            var publishedNodesUri = new Uri("/published-nodes", UriKind.Relative);
            var dataSetClassId = Guid.NewGuid();
            using var content = new StringContent(
                $"{{\"DataSetClassId\":\"{dataSetClassId}\"}}",
                Encoding.UTF8, Json.MimeType);
            using var response = await client.PostAsync(publishedNodesUri, content);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(body);
            Assert.Equal(dataSetClassId,
                document.RootElement.GetProperty("DataSetClassId").GetGuid());
            Assert.False(document.RootElement.TryGetProperty("dataSetClassId", out _));

            using var emptyContent = new StringContent("{}", Encoding.UTF8, Json.MimeType);
            using var emptyResponse = await client.PostAsync(publishedNodesUri, emptyContent);
            emptyResponse.EnsureSuccessStatusCode();
            using var emptyDocument = JsonDocument.Parse(
                await emptyResponse.Content.ReadAsStringAsync());
            Assert.False(emptyDocument.RootElement.TryGetProperty("DataSetClassId", out _));
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
