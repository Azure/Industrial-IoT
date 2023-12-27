﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class PublisherControllerTests : PublisherIntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public PublisherControllerTests(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        [Fact]
        public async Task GetApiKeyAndCertificateTest()
        {
            const string name = nameof(GetApiKeyAndCertificateTest);
            StartPublisher(name, "./Resources/empty_pn.json", arguments: new string[] { "--mm=PubSub" });
            try
            {
                var apiKey = await PublisherApi.GetApiKeyAsync();
                Assert.NotNull(apiKey);
                Assert.NotNull(Convert.FromBase64String(apiKey));

                var certificate = await PublisherApi.GetServerCertificateAsync();
                Assert.NotNull(certificate);
                using var x509 = X509Certificate2.CreateFromPem(certificate);
                Assert.StartsWith("DC=", x509.Subject, StringComparison.Ordinal);
            }
            finally
            {
                StopPublisher();
            }
        }

        [Fact]
        public async Task ShutdownTest()
        {
            const string name = nameof(ShutdownTest);
            StartPublisher(name, "./Resources/empty_pn.json");
            try
            {
                // We mocked this call
                await PublisherApi.ShutdownAsync();
                await PublisherApi.ShutdownAsync(true);
            }
            finally
            {
                StopPublisher();
            }
        }
    }
}
