// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Verifies the web host startup wiring, in particular that recursive model
    /// validation is suppressed for <see cref="VariantValue"/>. Validating the
    /// large recursive value graph of a VariantValue (e.g. a big ByteString
    /// argument on a method call request) is extremely expensive and adds
    /// seconds of latency to otherwise trivial API calls.
    /// </summary>
    public sealed class StartupTests
    {
        [Fact]
        public void ConfigureServicesSuppressesVariantValueChildValidation()
        {
            using var provider = BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<MvcOptions>>().Value;

            Assert.Contains(options.ModelMetadataDetailsProviders
                .OfType<SuppressChildValidationMetadataProvider>(),
                p => p.Type == typeof(VariantValue));
        }

        [Fact]
        public void VariantValueChildrenAreNotValidated()
        {
            using var provider = BuildServiceProvider();
            var metadata = provider.GetRequiredService<IModelMetadataProvider>();

            // The VariantValue graph must not be walked during validation ...
            var variant = metadata.GetMetadataForType(typeof(VariantValue));
            Assert.False(variant.ValidateChildren);

            // ... while regular request models keep their default behaviour.
            var request = metadata.GetMetadataForType(
                typeof(RequestEnvelope<MethodCallRequestModel>));
            Assert.True(request.ValidateChildren);
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
