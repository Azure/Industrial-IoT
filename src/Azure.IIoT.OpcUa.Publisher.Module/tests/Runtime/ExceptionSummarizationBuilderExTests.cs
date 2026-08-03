// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class ExceptionSummarizationBuilderExTests
    {
        [Fact]
        public void AddDefaultProvidersRegistersExceptionSummarizer()
        {
            var services = new ServiceCollection();
            services.AddExceptionSummarizer(builder => builder.AddDefaultProviders());

            using var provider = services.BuildServiceProvider();
            var summarizer = provider.GetRequiredService<IExceptionSummarizer>();

            var summary = summarizer.Summarize(
                new ResourceNotFoundException("publisher entry was not found"));

            Assert.Equal(nameof(ResourceNotFoundException), summary.ExceptionType);
            Assert.Equal("The requested resource could not be found.",
                summary.Description);
            Assert.Equal("publisher entry was not found", summary.AdditionalDetails);
        }

        [Theory]
        [InlineData(typeof(NotSupportedException),
            "The operation is not supported.")]
        [InlineData(typeof(NotImplementedException),
            "The operation has not yet been implemented.")]
        [InlineData(typeof(TimeoutException),
            "The operation timed out.")]
        [InlineData(typeof(TaskCanceledException),
            "The operation was cancelled.")]
        [InlineData(typeof(ArgumentNullException),
            "A parameter was unexpectedly null.")]
        [InlineData(typeof(ArgumentOutOfRangeException),
            "A parameter was outside of the allowed range.")]
        public void BuiltInProviderSummarizesExactExceptionTypes(
            Type exceptionType, string expectedDescription)
        {
            var provider = CreateProvider("BuiltInExceptionProvider");
            var exception = (Exception)Activator.CreateInstance(
                exceptionType, "details")!;

            var index = provider.Describe(exception, out var additionalDetails);

            Assert.Equal(expectedDescription, provider.Descriptions[index]);
            Assert.Contains("details", additionalDetails, StringComparison.Ordinal);
        }

        [Fact]
        public void BuiltInProviderUsesReasonUnknownForOperationCancellation()
        {
            var provider = CreateProvider("BuiltInExceptionProvider");

            var index = provider.Describe(new OperationCanceledException(),
                out var additionalDetails);

            Assert.Equal("The operation was cancelled.", provider.Descriptions[index]);
            Assert.Equal("Reason unknown", additionalDetails);
        }

        [Fact]
        public void HttpProviderSummarizesSocketAndWebExceptions()
        {
            var provider = CreateProvider("HttpExceptionProvider");

            var socketIndex = provider.Describe(
                new SocketException((int)SocketError.ConnectionRefused), out var socketDetails);
            var webIndex = provider.Describe(
                new WebException("timed out", WebExceptionStatus.Timeout), out var webDetails);

            Assert.Equal("ConnectionRefused", provider.Descriptions[socketIndex]);
            Assert.Equal("Timeout", provider.Descriptions[webIndex]);
            Assert.Null(socketDetails);
            Assert.Null(webDetails);
        }

        [Fact]
        public void HttpProviderDistinguishesRequestedAndUnrequestedCancellation()
        {
            var provider = CreateProvider("HttpExceptionProvider");
            using var source = new CancellationTokenSource();
            source.Cancel();

            var requestedIndex = provider.Describe(
                new OperationCanceledException(source.Token), out var requestedDetails);
            var unrequestedIndex = provider.Describe(
                new OperationCanceledException(), out var unrequestedDetails);

            Assert.Equal(0, requestedIndex);
            Assert.Equal(1, unrequestedIndex);
            Assert.Null(requestedDetails);
            Assert.Null(unrequestedDetails);
        }

        [Fact]
        public void HttpProviderReturnsMinusOneForUnsupportedException()
        {
            var provider = CreateProvider("HttpExceptionProvider");

            var index = provider.Describe(new InvalidOperationException("not http"),
                out var additionalDetails);

            Assert.Equal(-1, index);
            Assert.Null(additionalDetails);
        }

        [Theory]
        [InlineData("HttpExceptionProvider")]
        [InlineData("BuiltInExceptionProvider")]
        public void ProvidersRejectNullExceptions(string providerName)
        {
            var provider = CreateProvider(providerName);

            Assert.Throws<ArgumentNullException>(() =>
                provider.Describe(null!, out _));
        }

        private static IExceptionSummaryProvider CreateProvider(string providerName)
        {
            var providerType = typeof(ExceptionSummarizationBuilderEx).GetNestedType(
                providerName, BindingFlags.NonPublic);
            Assert.NotNull(providerType);
            return Assert.IsAssignableFrom<IExceptionSummaryProvider>(
                Activator.CreateInstance(providerType));
        }
    }
}
