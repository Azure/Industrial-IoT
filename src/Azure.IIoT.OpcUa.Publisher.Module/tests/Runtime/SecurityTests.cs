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

        [Theory]
        [InlineData("secre")]        // a correct prefix, one character short
        [InlineData("secrets")]      // the correct key with a character appended
        [InlineData("Secret")]       // right length, differs only in case
        [InlineData("xecret")]       // right length, differs in the first byte
        [InlineData("secreT")]       // right length, differs in the last byte
        public async Task AuthenticateFailsForKeysThatDifferOnlySlightlyAsync(string offered)
        {
            //
            // The comparison is fixed-time, which is easy to get subtly wrong -
            // a length mismatch, a case difference, or a difference in the very
            // first or very last byte are the cases where a hand-rolled
            // constant-time compare tends to fall back to accepting. Each of
            // these must be rejected.
            //
            var (service, accessor, provider) = CreateAuthenticationService("secret");
            var context = CreateContext(provider);
            context.Request.Headers.Authorization = $"{Security.ApiKeyScheme} {offered}";
            accessor.HttpContext = context;

            var result = await service.AuthenticateAsync(context, Security.ApiKeyScheme);

            Assert.False(result.Succeeded);
            Assert.IsType<UnauthorizedAccessException>(result.Failure);
        }

        [Fact]
        public async Task AuthenticateSucceedsForANonAsciiKeyAsync()
        {
            //
            // The comparison runs over UTF-8 bytes rather than chars, so a key
            // whose characters encode to more than one byte has to keep working.
            //
            const string key = "schl\u00fcssel-\u4e2d\u6587";
            var (service, accessor, provider) = CreateAuthenticationService(key);
            var context = CreateContext(provider);
            context.Request.Headers.Authorization = $"{Security.ApiKeyScheme} {key}";
            accessor.HttpContext = context;

            var result = await service.AuthenticateAsync(context, Security.ApiKeyScheme);

            Assert.Null(result.Failure);
            Assert.True(result.Succeeded);
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
