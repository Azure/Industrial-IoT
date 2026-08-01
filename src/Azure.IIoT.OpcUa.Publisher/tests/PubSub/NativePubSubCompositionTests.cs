// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.PubSub
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.PubSub;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class NativePubSubCompositionTests
    {
        [Fact]
        public async Task AbsentOptionUsesTheNativePubSubSinkAsync()
        {
            //
            // Absent means the product default applies, and in 3.0 that is the
            // native path. This asserts the registration and the bound option
            // agree on the default: if they diverge the publisher composes one
            // telemetry path while configured for the other, and the symptom is
            // missing telemetry rather than a configuration error.
            //
            await using var provider = CreateProvider(new Dictionary<string, string?>());

            Assert.NotNull(provider.GetService<IPubSubShadowHost>());
            await using var scope = provider.CreateAsyncScope();
            InitializeWriterGroupScope(scope.ServiceProvider, provider);

            var sink = scope.ServiceProvider.GetRequiredService<IMessageSink>();

            Assert.IsType<PubSubNotificationSink>(sink);
            Assert.True(PublisherConfig.UseNativePubSubDefault);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task BlankOptionUsesTheProductDefaultAsync(string value)
        {
            //
            // An empty value is not a configured "false". Treating it as one
            // would let an unset environment variable that still materialises
            // as an empty string silently select the path the product no
            // longer defaults to.
            //
            await using var provider = CreateProvider(new Dictionary<string, string?>
            {
                [PublisherConfig.UseNativePubSubKey] = value
            });

            Assert.NotNull(provider.GetService<IPubSubShadowHost>());
            await using var scope = provider.CreateAsyncScope();
            InitializeWriterGroupScope(scope.ServiceProvider, provider);

            Assert.IsType<PubSubNotificationSink>(
                scope.ServiceProvider.GetRequiredService<IMessageSink>());
        }

        [Theory]
        [InlineData("false")]
        [InlineData("NO")]
        [InlineData("n")]
        [InlineData("0")]
        [InlineData("nonsense")]
        public async Task DisablingTheOptionNoLongerSelectsAnythingElseAsync(string value)
        {
            //
            // The custom encoder this option used to select is gone in 3.0, so
            // a configuration that still disables it keeps working and
            // publishes natively rather than failing to start. This is
            // asserted rather than left implicit because the option is still
            // read and bound, which makes it look like it still branches.
            //
            await using var provider = CreateProvider(new Dictionary<string, string?>
            {
                [PublisherConfig.UseNativePubSubKey] = value
            });

            Assert.NotNull(provider.GetService<IPubSubShadowHost>());
            await using var scope = provider.CreateAsyncScope();
            InitializeWriterGroupScope(scope.ServiceProvider, provider);

            Assert.IsType<PubSubNotificationSink>(
                scope.ServiceProvider.GetRequiredService<IMessageSink>());
        }

        [Fact]
        public async Task EnabledOptionUsesTheNativePubSubSinkAsync()
        {
            await using var provider = CreateProvider(new Dictionary<string, string?>
            {
                [PublisherConfig.UseNativePubSubKey] = "true"
            });

            Assert.NotNull(provider.GetService<IPubSubShadowHost>());
            await using var scope = provider.CreateAsyncScope();
            InitializeWriterGroupScope(scope.ServiceProvider, provider);

            var sink = scope.ServiceProvider.GetRequiredService<IMessageSink>();

            Assert.IsType<PubSubNotificationSink>(sink);
        }

        [Theory]
        [InlineData("true")]
        [InlineData("TRUE")]
        [InlineData("yes")]
        [InlineData("Y")]
        [InlineData("1")]
        public async Task EnabledOptionAliasesAreHonouredAsync(string value)
        {
            //
            // The registration-time check must accept the same aliases the
            // options binder accepts, otherwise the flag is silently ignored.
            //
            await using var provider = CreateProvider(new Dictionary<string, string?>
            {
                [PublisherConfig.UseNativePubSubKey] = value
            });

            Assert.NotNull(provider.GetService<IPubSubShadowHost>());
            await using var scope = provider.CreateAsyncScope();
            InitializeWriterGroupScope(scope.ServiceProvider, provider);

            Assert.IsType<PubSubNotificationSink>(
                scope.ServiceProvider.GetRequiredService<IMessageSink>());
        }

        private static ServiceProvider CreateProvider(
            Dictionary<string, string?> configurationValues)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationValues)
                .Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IConfigurationRoot>(configuration);
            services.AddTransient<NullEventClient>();
            services.AddTransient<IEventClient>(
                static provider => provider.GetRequiredService<NullEventClient>());
            services.AddPublisherCore();
            return services.BuildServiceProvider();
        }

        private static void InitializeWriterGroupScope(IServiceProvider scopedProvider,
            IServiceProvider rootProvider)
        {
            var context = scopedProvider.GetRequiredService<WriterGroupScopeContext>();
            context.Initialize(new WriterGroupModel { Id = "group" },
                rootProvider.GetRequiredService<IOptions<PublisherOptions>>(), null);
        }
    }
}
