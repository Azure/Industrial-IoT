// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using System;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class SecurityTests
    {
        [Fact]
        public async Task AuthenticateFailsWhenHttpContextAccessorHasNoRequestAsync()
        {
            var (service, _, provider) = CreateAuthenticationService("secret");

            var result = await service.AuthenticateAsync(CreateContext(provider),
                Security.ApiKeyScheme);

            Assert.False(result.Succeeded);
            Assert.Equal("No request.", result.Failure?.Message);
        }

        [Fact]
        public async Task AuthenticateFailsWhenAuthorizationHeaderIsMissingAsync()
        {
            var (service, accessor, provider) = CreateAuthenticationService("secret");
            accessor.HttpContext = CreateContext(provider);

            var result = await service.AuthenticateAsync(accessor.HttpContext,
                Security.ApiKeyScheme);

            Assert.False(result.Succeeded);
            Assert.Equal("Missing Authorization header.", result.Failure?.Message);
        }

        [Fact]
        public async Task AuthenticateReturnsNoResultForDifferentSchemeAsync()
        {
            var (service, accessor, provider) = CreateAuthenticationService("secret");
            accessor.HttpContext = CreateContext(provider);
            accessor.HttpContext.Request.Headers.Authorization = "Bearer secret";

            var result = await service.AuthenticateAsync(accessor.HttpContext,
                Security.ApiKeyScheme);

            Assert.False(result.Succeeded);
            Assert.Null(result.Failure);
            Assert.Null(result.Principal);
        }

        [Fact]
        public async Task AuthenticateFailsWhenHeaderCannotBeParsedAsync()
        {
            var (service, accessor, provider) = CreateAuthenticationService("secret");
            accessor.HttpContext = CreateContext(provider);
            accessor.HttpContext.Request.Headers.Authorization = "ApiKey \"unterminated";

            var result = await service.AuthenticateAsync(accessor.HttpContext,
                Security.ApiKeyScheme);

            Assert.False(result.Succeeded);
            Assert.NotNull(result.Failure);
        }

        [Fact]
        public async Task AuthenticateFailsWhenApiKeyDoesNotMatchAsync()
        {
            var (service, accessor, provider) = CreateAuthenticationService("secret");
            accessor.HttpContext = CreateContext(provider);
            accessor.HttpContext.Request.Headers.Authorization = "ApiKey wrong";

            var result = await service.AuthenticateAsync(accessor.HttpContext,
                Security.ApiKeyScheme);

            Assert.False(result.Succeeded);
            Assert.IsType<UnauthorizedAccessException>(result.Failure);
        }

        [Fact]
        public async Task AuthenticateSucceedsWithTrimmedApiKeyParameterAsync()
        {
            var (service, accessor, provider) = CreateAuthenticationService("secret");
            accessor.HttpContext = CreateContext(provider);
            accessor.HttpContext.Request.Headers.Authorization = "ApiKey  secret  ";

            var result = await service.AuthenticateAsync(accessor.HttpContext,
                Security.ApiKeyScheme);

            Assert.Null(result.Failure);
            var principal = Assert.IsAssignableFrom<ClaimsPrincipal>(result.Principal);
            Assert.Equal(Security.ApiKeyScheme, result.Ticket?.AuthenticationScheme);
            Assert.Contains(principal.Claims,
                claim => claim.Value == Security.ApiKeyScheme);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task AuthenticateFailsWhenNoApiKeyIsConfiguredAsync(string? configured)
        {
            //
            // With no key configured, a bare "ApiKey" header carries no
            // parameter, and comparing that against a null configured key made
            // null equal null - so an unauthenticated request authenticated.
            // Nothing can present a valid key when none exists.
            //
            var (service, accessor, provider) = CreateAuthenticationService(configured);
            var context = CreateContext(provider);
            context.Request.Headers.Authorization = Security.ApiKeyScheme;
            accessor.HttpContext = context;

            var result = await service.AuthenticateAsync(context, Security.ApiKeyScheme);

            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task AuthenticateFailsWhenNoApiKeyIsConfiguredAndOneIsOfferedAsync()
        {
            var (service, accessor, provider) = CreateAuthenticationService(null);
            var context = CreateContext(provider);
            context.Request.Headers.Authorization = $"{Security.ApiKeyScheme} guessed";
            accessor.HttpContext = context;

            var result = await service.AuthenticateAsync(context, Security.ApiKeyScheme);

            Assert.False(result.Succeeded);
        }

        private static (IAuthenticationService Service, IHttpContextAccessor Accessor,
            IServiceProvider Provider)
            CreateAuthenticationService(string? apiKey)
        {
            var accessor = new HttpContextAccessor();
            var keyProvider = new Mock<IApiKeyProvider>(MockBehavior.Strict);
            keyProvider.SetupGet(k => k.ApiKey).Returns(apiKey);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions();
            services.AddSingleton<IHttpContextAccessor>(accessor);
            services.AddSingleton(keyProvider.Object);
            services.AddAuthentication().UsingConfiguredApiKey();
            var provider = services.BuildServiceProvider();
            return (provider.GetRequiredService<IAuthenticationService>(), accessor, provider);
        }

        private static DefaultHttpContext CreateContext(IServiceProvider provider)
        {
            return new DefaultHttpContext
            {
                RequestServices = provider
            };
        }
    }
}
